using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("CSharpDummy.Tests")]

namespace CSharpDummy.NetLib;

public partial class SendPacket
{
    internal int Length => _idx;
    internal ArraySegment<byte> Buffer => new(_buffer!, 0, Length);
    private int Remain => _buffer!.Length - _idx;
    private object _encodeLock = new();

    internal void Write<T>(T value) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        if (!EnsureCapacity(size))
            throw new ArgumentOutOfRangeException(value.ToString());

        MemoryMarshal.Write(_buffer.AsSpan(_idx), in value);
        _idx += size;
    }

    internal void WriteBytes(ReadOnlySpan<byte> value)
    {
        int size = value.Length;
        if (!EnsureCapacity(size))
            throw new ArgumentOutOfRangeException(value.ToString());

        value.CopyTo(_buffer.AsSpan(_idx));
        _idx += size;
    }

    internal void WriteFixedString(string value, int size)
    {
        if (size < 0 || value.Length > size)
            throw new ArgumentOutOfRangeException(nameof(size));

        //  c#에서 char 는 2바이트이며, checked는 오버플로 발생 시 에러 던진다.
        int byteSize = checked(size * sizeof(char));
        if (!EnsureCapacity(byteSize))
            throw new ArgumentOutOfRangeException(value);

        Span<byte> destination = _buffer.AsSpan(_idx, byteSize);
        //  텍스트 들어가고 남은 공간은 0으로
        destination.Clear();

        Encoding.Unicode.GetBytes(value.AsSpan(), destination);
        _idx += byteSize;
    }

    internal void WriteString(string value)
    {
        if (value.Length > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value));
        int byteSize = checked(value.Length * sizeof(char));
        if (!EnsureCapacity(sizeof(ushort) + byteSize))
            throw new ArgumentOutOfRangeException(nameof(value));

        Write((ushort)value.Length);
        Encoding.Unicode.GetBytes(value.AsSpan(), _buffer.AsSpan(_idx, byteSize));
        _idx += byteSize;
    }

    private bool EnsureCapacity(int size)
    {
        if (size < 0 || size > Remain)
            return false;

        return true;
    }
    
    internal void Seal()
    {
        var payload = _buffer.AsSpan(Header.Size, _idx - Header.Size);

        var header = new Header()
        {
            Code = Header.PacketCode,
            PayloadLen = (ushort)payload.Length,
            RandomKey = 0x31,
            Checksum = PacketCodec.CalculateChecksum(payload)
        };

        MemoryMarshal.Write(_buffer.AsSpan(), in header);

        PacketCodec.Encode(_buffer.AsSpan(Header.EncodeStartOffset, _idx - Header.EncodeStartOffset), Header.StaticKey, header.RandomKey);
    }
}
