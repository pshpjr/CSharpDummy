using System.Runtime.InteropServices;

namespace CSharpDummy.NetLib;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Header
{
    public const byte PacketCode = 0x77;
    public const byte StaticKey = 50;

    public static readonly int Size = Marshal.SizeOf<Header>();
    public static readonly int EncodeStartOffset = (int)Marshal.OffsetOf<Header>(nameof(Checksum));
    public static readonly int PayloadOffset = EncodeStartOffset + 1;

    public byte Code;
    public ushort PayloadLen;
    public byte RandomKey;
    public byte Checksum;
}




