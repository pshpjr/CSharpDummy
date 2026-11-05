using System.Runtime.InteropServices;
using CSharpDummy.NetLib;

namespace CSharpDummy.Tests;

public class SendPacketTests
{
    [Fact]
    public void Seal_EncodeRange_SameAsEncoded()
    {
        using var packet = CreatePacket();
        byte[] payload = packet.Buffer[Header.PayloadOffset..].ToArray();
        byte[] expected = [PacketCodec.CalculateChecksum(payload), .. payload];

        packet.Seal();

        var header = MemoryMarshal.Read<Header>(packet.Buffer.AsSpan());
        Assert.Equal(Header.PacketCode, header.Code);
        Assert.Equal(payload.Length, header.PayloadLen);

        PacketCodec.Encode(expected, Header.StaticKey, header.RandomKey);
        Assert.Equal(expected, packet.Buffer[Header.EncodeStartOffset..].AsSpan());
    }

    [Fact]
    public void SealDecode_Payload_SameAsOriginal()
    {
        using var packet = CreatePacket();
        byte[] payload = packet.Buffer[Header.PayloadOffset..].ToArray();

        SealAndDecode(packet);

        Assert.Equal(payload, packet.Buffer[Header.PayloadOffset..].AsSpan());
    }

    [Fact]
    public void SealDecode_Checksum_SameAsCalculated()
    {
        using var packet = CreatePacket();

        var header = SealAndDecode(packet);

        Assert.Equal(PacketCodec.CalculateChecksum(packet.Buffer[Header.PayloadOffset..]), header.Checksum);
    }

    [Fact]
    public void SealDecode_PayloadChanged_ChecksumDiffers()
    {
        using var packet = CreatePacket();

        var header = SealAndDecode(packet);
        Span<byte> payload = packet.Buffer[Header.PayloadOffset..];
        payload[^1]++;

        Assert.NotEqual(header.Checksum, PacketCodec.CalculateChecksum(payload));
    }

    private static SendPacket CreatePacket()
    {
        var packet = SendPacket.Create();
        Protocol.MakeLogin_ReqLogin(packet, "ID_1", "PASS_1");
        return packet;
    }

    // Session.RecvLoop와 같은 범위를 복호화한다.
    private static Header SealAndDecode(SendPacket packet)
    {
        packet.Seal();
        Span<byte> frame = packet.Buffer;
        PacketCodec.Decode(frame[Header.EncodeStartOffset..], [], Header.StaticKey, MemoryMarshal.Read<Header>(frame).RandomKey);
        return MemoryMarshal.Read<Header>(frame);
    }
}
