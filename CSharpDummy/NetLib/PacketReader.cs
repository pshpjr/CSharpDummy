using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CSharpDummy.NetLib;

public ref struct PacketReader
{
    private Span<byte> _buffer = default;
    private int _idx = 0;

    public int Count => Remain;
    private int Remain => _buffer.Length - _idx;

    public PacketReader(Span<byte> buffer)
    {
        _buffer = buffer;
    }

    public T Read<T>() where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (!EnsureRemaining(size))
            throw new ArgumentOutOfRangeException();
        
        T value = MemoryMarshal.Read<T>(_buffer[_idx..]);
        _idx += size;
        return value;
    }
    
    public void ReadBytes(Span<byte> value)
    {
        if (!EnsureRemaining(value.Length))
            throw new ArgumentOutOfRangeException();
        
        _buffer.Slice(_idx, value.Length).CopyTo(value);
        _idx += value.Length;
    }
    
    public string ReadFixedString(int size)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size));
        int byteSize = checked(size * sizeof(char));
        if (!EnsureRemaining(byteSize))
            throw new ArgumentOutOfRangeException(nameof(size));

        string value = Encoding.Unicode.GetString(_buffer.Slice(_idx, byteSize));
        _idx += byteSize;
        int terminator = value.IndexOf('\0');
        return terminator < 0 ? value : value[..terminator];
    }

    public string ReadString()
    {
        int size = Read<ushort>();
        int byteSize = size * sizeof(char);
        if (!EnsureRemaining(byteSize))
            throw new ArgumentOutOfRangeException(nameof(size));

        string value = Encoding.Unicode.GetString(_buffer.Slice(_idx, byteSize));
        _idx += byteSize;
        return value;
    }

    private bool EnsureRemaining(int size)
    {
        if (size < 0 || size > Remain)
            return false;

        return true;
    }
}
