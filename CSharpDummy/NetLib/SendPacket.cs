using System.Buffers;
using System.Runtime.InteropServices;

namespace CSharpDummy.NetLib;

public partial class SendPacket : IDisposable
{
    public const int MaxPacketSize = 1024;

    private int _atomicRef = 0;
    private byte[]? _buffer;
    private int _idx = 0;

    public PacketType Type
    {
        get => MemoryMarshal.Read<PacketType>(_buffer.AsSpan(Header.Size));
        set => MemoryMarshal.Write(_buffer.AsSpan(Header.Size), in value);
    }

    private SendPacket(byte[] buffer)
    {
        _buffer = buffer;
        //  헤더 + 패킷 타입
        _idx = Header.Size + sizeof(PacketType);
        _atomicRef = 1;
    }

    public static SendPacket Create()
    {
        return new SendPacket(ArrayPool<byte>.Shared.Rent(MaxPacketSize));
    }

    public void AddRef()
    {
        //  C#에는 acq rel 지정 x
        Interlocked.Increment(ref _atomicRef);
    }

    public void AddRef(int refCount)
    {
        if (refCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(refCount));

        Interlocked.Add(ref _atomicRef, refCount);
    }

    public void Dispose()
    {
        if (Interlocked.Decrement(ref _atomicRef) == 0)
        {
            ArrayPool<byte>.Shared.Return(_buffer!);
            _buffer = null;
        }
    }
    
}
