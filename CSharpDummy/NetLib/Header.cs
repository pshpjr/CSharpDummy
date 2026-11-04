using System.Runtime.InteropServices;

namespace CSharpDummy.NetLib;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Header
{
    public const byte PacketCode = 0x77;

    public static readonly int HeaderSize = Marshal.SizeOf<Header>();
    public static readonly int EncodeStartOffset = (int)Marshal.OffsetOf<Header>(nameof(Checksum));

    public byte Code;
    public ushort Len;
    public byte RandomKey;
    public byte Checksum;
}




