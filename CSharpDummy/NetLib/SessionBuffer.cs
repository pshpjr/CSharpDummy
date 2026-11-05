using System.Drawing;

namespace CSharpDummy.NetLib;

public class SessionBuffer
{
    private readonly byte[] _buffer;
    private int _front = 0;
    private int _rear = 0;

    public int Count => (int)(_rear - _front);
    public int Capacity => _buffer.Length;
    public Memory<byte> WriteMemory => new Memory<byte>(_buffer, _rear, Capacity - _rear);
    public Span<byte> ReadSpan => new Span<byte>(_buffer, _front, Count);

    public SessionBuffer(int bufferSize)
    {
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        
        _buffer = new byte[bufferSize];
    }

    public void Clear()
    {
        _front = 0;
        _rear = 0;
    }

    public bool Advance(int count)
    {
        if (count <= 0 || count > Capacity - _rear)
            return false;
        
        _rear += count;
        return true;
    }
    
    public bool Consume(int count)
    {
        if (count <= 0 || count > Count)
            return false;

        _front += count;
        return true;
    }

    //  남으면 복사하는 방식. 
    //  구현 편의성을 위해 수신 데이터가 연속되어 있게 만듬
    //  decode 시 편의성을 고려했고, 실제로 memcpy 성능 이슈가 측정되면 수정
    public void Compact()
    {
        if (_front == _rear)
        {
            Clear();
        }
        else
        {
            int remaining = Count;

            _buffer.AsSpan(_front, remaining).CopyTo(_buffer);

            _front = 0;
            _rear = remaining;
        }
    }
    

    
}
