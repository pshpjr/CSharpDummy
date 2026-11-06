using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace CSharpDummy.NetLib;

public class Session : IAsyncDisposable
{
    public delegate void OnRecvPacket(PacketType type, PacketReader packet);
    
    const int MaxPendingPacketSize = 512;
    const int MaxSendingPacketSize = 64;
    
    private SessionBuffer _buffer = new(SendPacket.MaxPacketSize);
    private IPEndPoint? _endpoint;
    private Socket? _socket;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private Channel<SendPacket> _channel;
    private readonly OnRecvPacket _onRecv;
    public Action? OnDisconnect;

    public Session(OnRecvPacket onRecv)
    {
        _onRecv = onRecv;
        _channel = Channel.CreateBounded<SendPacket>(new BoundedChannelOptions(MaxPendingPacketSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
    }
    
    public async Task ConnectAsync(IPEndPoint endpoint)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        _endpoint = endpoint;
        
        _socket =  new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _socket.LingerState = new LingerOption(true, 0);
        
        await _socket.ConnectAsync(_endpoint, cts.Token);
    }

    //  패킷 소유권 가지고 감
    public async Task Send(SendPacket packet)
    {
        try
        {
            await _channel.Writer.WriteAsync(packet, _cts.Token);
        }
        catch
        {
            packet.Dispose();
            throw;
        }
    }
    
    public async Task Disconnect()
    {
        _channel.Writer.TryComplete();       // 이후 Send는 실패하게
        await _cts.CancelAsync();
        _socket?.Close();
    }
    
    public async ValueTask DisposeAsync()
    {
        await Disconnect();                  
    }
    
    public async Task Run(CancellationToken ct)
    {
        await using var reg = ct.Register(() => _cts.Cancel());   // 바깥 취소 → 내부 취소로 연결
        try
        {
            await Task.WhenAll(RecvLoop(), SendLoop());
        }
        finally
        {
            OnDisconnect?.Invoke(); 
        }
    }
    
    private async Task RecvLoop()
    {
        try
        {
            //  recv 반복해서 거는 루프
            while (_cts.IsCancellationRequested == false)
            {
                //  TODO: SAEA? 사용해보기
                int received = await _socket!.ReceiveAsync(_buffer.WriteMemory, _cts.Token);
                if (received == 0)
                {
                    await Disconnect();
                    break;
                }

                _buffer.Advance(received);
                while (true)
                {
                    var buffer = _buffer.ReadSpan;
                    if (buffer.Length < Header.Size)
                        break;
                    
                    Header header = MemoryMarshal.Read<Header>(buffer);
                    var packetLen = Header.Size + header.PayloadLen;
                    //  오류: 패킷 길이 이상 / 코드 이상
                    if (packetLen > _buffer.Capacity || header.Code != Header.PacketCode)
                    {
                        await Disconnect();
                        return;
                    }
                    
                    //  패킷 충분히 못 받은 경우
                    if (packetLen > buffer.Length)
                        break;
                    
                    var encoded = buffer.Slice(Header.EncodeStartOffset, packetLen - Header.EncodeStartOffset);
                    PacketCodec.Decode(encoded, Span<byte>.Empty, Header.StaticKey, header.RandomKey);
                    
                    var payload = buffer.Slice(Header.PayloadOffset, header.PayloadLen);
                    // 복호화된 체크섬
                    byte checksum = buffer[Header.EncodeStartOffset];   
                    //  오류: 체크섬 검증 실패
                    if (checksum != PacketCodec.CalculateChecksum(payload))
                    {
                        await Disconnect();
                        return;
                    }
                    
                    
                    var reader = new PacketReader(payload);
                    var type = reader.Read<PacketType>();
                    _onRecv(type, reader);
                    _buffer.Consume(packetLen);
                }
                _buffer.Compact();
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // disconnect 내가 호출한 경우.
        }
        catch
        {
            await Disconnect();
        }
    }
    
    private async Task SendLoop()
    {
        //  sendPacket 모아서 한 번에 보내기 위한 버퍼
        List<ArraySegment<byte>> lstBuffer = new(MaxSendingPacketSize);
        List<SendPacket> lstInflightPacket = new (MaxSendingPacketSize);
        try
        {
            while (await _channel.Reader.WaitToReadAsync(_cts.Token))
            {
                lstInflightPacket.Clear();
                lstBuffer.Clear();

                while (lstInflightPacket.Count < MaxSendingPacketSize && _channel.Reader.TryRead(out var packet))
                {
                    if (packet.Length == 0)
                    {
                        packet.Dispose();
                        continue;
                    }

                    lstInflightPacket.Add(packet);
                    lstBuffer.Add(packet.Buffer);
                    
                    //  클라이언트는 패킷을 broadcast 하지 않는 것으로 한다.
                    //  한 세션만 접근하므로 TrySeal같은 동기화 함수를 쓰지 않아도 된다. 
                    packet.Seal();
                }

                //  전송할 패킷이 없음
                if (lstInflightPacket.Count == 0)
                    continue;

                int sent = await _socket!.SendAsync(lstBuffer, SocketFlags.None);
                if (sent == 0)
                    throw new IOException("Socket send returned 0.");

                //  완료된 패킷 정리
                foreach (var packet in lstInflightPacket)
                {
                    packet.Dispose();
                }
                lstInflightPacket.Clear();
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // disconnect 내가 호출한 경우.
        }
        catch
        {
            await Disconnect();
        }
        finally
        {
            //  전송 중이던 것, 대기 중이던 것 정리
            foreach (var packet in lstInflightPacket)
            {
                packet.Dispose();
            }
            while (_channel.Reader.TryRead(out var p))
            {
                p.Dispose();
            }
            
            lstInflightPacket.Clear();
            lstBuffer.Clear();
        }
    }
}
