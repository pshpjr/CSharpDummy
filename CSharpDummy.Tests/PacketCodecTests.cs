using CSharpDummy.NetLib;

namespace CSharpDummy.Tests;

public class PacketCodecTests
{
    private const byte StaticKey = Header.StaticKey;
    private const byte RandomKey = 0x31;
    
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(100)]
    [InlineData(1000)]
    public void RoundTrip(int length)
    {
        var payload = new byte[length];
        new Random(length).NextBytes(payload);
        var original = (byte[])payload.Clone();

        PacketCodec.Encode(payload, StaticKey, RandomKey);
        Assert.NotEqual(original, payload);

        PacketCodec.Decode(payload, Span<byte>.Empty, StaticKey, RandomKey);
        Assert.Equal(original, payload);
    }
}

//
// public class PacketCodecTests
// {
//     private const byte StaticKey = 50;
//     private const byte RandomKey = 0x31;
//
//     // web-client/src/codec.ts와 같은 규칙으로 계산한 값
//     // Game_ReqChangeComplete(accountNo = 1)
//     private static readonly byte[] Payload1 = Hex("08 00 01 00 00 00 00 00 00 00");
//     private static readonly byte[] Frame1 = Hex("77 0a 00 31 08 5a 15 85 b8 cb 77 1d be db 45");
//
//     // Login_ReqLogin("ID_1", "PASS_1")
//     private static readonly byte[] Payload2 = Hex(
//         "00 00 49 00 44 00 5f 00 31 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
//         "50 00 41 00 53 00 53 00 5f 00 31 00 00 00 00 00 00 00 00 00 00 00 00 00");
//     private static readonly byte[] Frame2 = Hex(
//         "77 32 00 31 e5 10 78 95 bd 19 76 b2 d1 4b 0a f7 cb 37 04 fb c1 47 0a 9b " +
//         "f3 63 0c bf 39 fb 82 97 73 66 42 81 b3 ee 7e 19 53 e0 9a 08 35 20 70 a4 " +
//         "cb 0c ea a4 4d 1c 90");
//
//     public static TheoryData<byte[], byte[]> Vectors => new()
//     {
//         { Payload1, Frame1 },
//         { Payload2, Frame2 },
//     };
//
//     [Fact]
//     public void HeaderLayout()
//     {
//         Assert.Equal(5, Header.Size);
//         Assert.Equal(4, Header.EncodeStartOffset);
//     }
//
//     [Fact]
//     public void Checksum()
//     {
//         Assert.Equal(0x09, PacketCodec.CalculateChecksum(Payload1));
//     }
//
//     [Theory]
//     [MemberData(nameof(Vectors))]
//     public void EncodeMatchesVector(byte[] payload, byte[] expected)
//     {
//         var frame = MakePlainFrame(payload);
//
//         PacketCodec.Encode(frame.AsSpan(Header.EncodeStartOffset), StaticKey, RandomKey);
//
//         Assert.Equal(expected, frame);
//     }
//
//     [Theory]
//     [MemberData(nameof(Vectors))]
//     public void DecodeMatchesVector(byte[] payload, byte[] encoded)
//     {
//         var frame = (byte[])encoded.Clone();
//
//         PacketCodec.DeSerialize(frame.AsSpan(Header.EncodeStartOffset), Span<byte>.Empty, StaticKey, RandomKey);
//
//         Assert.Equal(MakePlainFrame(payload), frame);
//     }
//
//     // 링버퍼 끝에서 잘린 경우: 어디서 나눠도 한 번에 디코딩한 결과와 같아야 한다.
//     [Theory]
//     [MemberData(nameof(Vectors))]
//     public void DecodeSplitMatchesWhole(byte[] payload, byte[] encoded)
//     {
//         var expected = MakePlainFrame(payload);
//         var body = encoded.AsSpan(Header.EncodeStartOffset).ToArray();
//
//         for (int split = 0; split <= body.Length; split++)
//         {
//             var work = (byte[])body.Clone();
//             PacketCodec.DeSerialize(work.AsSpan(0, split), work.AsSpan(split), StaticKey, RandomKey);
//             Assert.Equal(expected.AsSpan(Header.EncodeStartOffset).ToArray(), work);
//         }
//     }
//
//
//
//     [Fact]
//     public void WrongStaticKeyBreaksChecksum()
//     {
//         var frame = (byte[])Frame2.Clone();
//
//         PacketCodec.DeSerialize(frame.AsSpan(Header.EncodeStartOffset), Span<byte>.Empty, StaticKey + 1, RandomKey);
//
//         var checksum = frame[Header.EncodeStartOffset];
//         var payload = frame.AsSpan(Header.Size);
//         Assert.NotEqual(checksum, PacketCodec.CalculateChecksum(payload));
//     }
//
//     private static byte[] MakePlainFrame(byte[] payload)
//     {
//         var frame = new byte[Header.Size + payload.Length];
//         frame[0] = Header.StaticKey;
//         BitConverter.TryWriteBytes(frame.AsSpan(1, 2), (ushort)payload.Length);
//         frame[3] = RandomKey;
//         frame[4] = PacketCodec.CalculateChecksum(payload);
//         payload.CopyTo(frame, Header.Size);
//         return frame;
//     }
//
//     private static byte[] Hex(string text) => Convert.FromHexString(text.Replace(" ", ""));
// }
