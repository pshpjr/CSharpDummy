using CSharpDummy.NetLib;

namespace CSharpDummy.Tests;

public class ProtocolPacketTest
{
    [Fact]
    public void FixedBytes_WriteRead_SameBytes()
    {
        byte[] key = Enumerable.Range(192, 64).Select(value => (byte)value).ToArray();
        using var packet = SendPacket.Create();
        Protocol.MakeGame_ReqLogin(packet, 1, key);

        var reader = new PacketReader(Payload(packet));
        reader.Read<PacketType>();
        var received = Protocol.GetGame_ReqLogin(ref reader);
        Assert.Equal(key, received.Key);
    }

    [Fact]
    public void FixedString_ShorterThanSize_PaddedWithZero()
    {
        using var packet = SendPacket.Create();
        packet.Buffer.Array.AsSpan().Fill(0xAA);
        packet.WriteFixedString("ID", 12);
        byte[] buffer = Body(packet);
        Assert.All(buffer[4..], value => Assert.Equal((byte)0, value));

        var reader = new PacketReader(buffer);
        Assert.Equal("ID", reader.ReadFixedString(12));
    }

    [Fact]
    public void FixedString_ExactSize_SameValue()
    {
        using var packet = SendPacket.Create();
        packet.WriteFixedString("123456789012", 12);

        var reader = new PacketReader(Body(packet));
        Assert.Equal("123456789012", reader.ReadFixedString(12));
        Assert.Equal(0, reader.Count);
    }

    [Fact]
    public void String_SurrogateAndNull_SameValue()
    {
        using var packet = SendPacket.Create();
        packet.WriteString("가😀\0");
        byte[] buffer = Body(packet);
        // 길이 접두사는 바이트 수가 아니라 UTF-16 code unit 수
        Assert.Equal(Convert.FromHexString("0400"), buffer[..2]);

        var reader = new PacketReader(buffer);
        Assert.Equal("가😀\0", reader.ReadString());
        Assert.Equal(0, reader.Count);
    }

    [Fact]
    public void Read_BufferShort_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
        () => {
            var reader = new PacketReader(new byte[47]);
            Protocol.GetLogin_ReqLogin(ref reader);
        });
    }

    [Fact]
    public void String_LengthExceedsBuffer_Throws()
    {
        // 길이 5(10바이트)라고 했지만 남은 바이트는 4
        byte[] buffer = Convert.FromHexString("050000000000");
        Assert.Throws<ArgumentOutOfRangeException>(
        () => {
            var reader = new PacketReader(buffer);
            reader.ReadString();
        });
    }

    [Fact]
    public void Write_BufferShort_Throws()
    {
        // 타입 자리는 생성 시 예약되므로 Login_ReqLogin 본문은 48바이트가 필요하지만 남은 공간은 47
        using var packet = SendPacket.Create();
        
        int capacity = packet.Buffer.Array!.Length;
        packet.WriteBytes(new byte[capacity - packet.Length - 47]);
        
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Protocol.MakeLogin_ReqLogin(packet, "ID", "PASS"));
    }

    [Fact]
    public void Read_BufferLonger_RemainCount()
    {
        using var packet = SendPacket.Create();
        Protocol.MakeGame_ReqChangeComplete(packet, 1);
        byte[] buffer = new byte[16];
        Payload(packet).CopyTo(buffer, 0);

        var reader = new PacketReader(buffer);
        reader.Read<PacketType>();
        Protocol.GetGame_ReqChangeComplete(ref reader);
        Assert.Equal(6, reader.Count);
    }

    [Fact]
    public void FixedString_LongerThanSize_Throws()
    {
        using var packet = SendPacket.Create();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => packet.WriteFixedString("1234567890123", 12));
    }

    [Fact]
    public void Dispatch_RequestPacket_Throws()
    {
        var received = new ReceivedPacket(PacketType.Game_ReqMove, new Game_ReqMove(new FVector(0, 0)));
        Assert.Throws<InvalidDataException>(
            () => Protocol.Dispatch(received, null!));
    }

    [Fact]
    public void Deserialize_RequestPacket_Throws()
    {
        Assert.Throws<InvalidDataException>(() =>
        {
            var reader = new PacketReader(new byte[8]);
            Protocol.Deserialize(PacketType.Game_ReqMove, ref reader);
        });
    }

    // Seal 전의 페이로드(헤더 제외)
    private static byte[] Payload(SendPacket packet) => packet.Buffer.AsSpan(Header.Size).ToArray();

    // 생성 시 예약된 패킷 타입 뒤의 본문
    private static readonly int BodyOffset = Header.Size + sizeof(PacketType);
    private static byte[] Body(SendPacket packet) => packet.Buffer.AsSpan(BodyOffset).ToArray();
}
