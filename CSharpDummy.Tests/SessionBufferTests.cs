using CSharpDummy.NetLib;

namespace CSharpDummy.Tests;

public class SessionBufferTests
{
    const int BufferSize = 20;
    private SessionBuffer _buffer = new SessionBuffer(BufferSize);

    [Fact]
    public void WriteSpan_Init_FullCapacity()
    {
        var span = _buffer.WriteMemory;
        Assert.Equal(span.Length, _buffer.Capacity);
    }
    
    
    [Fact]
    public void Advance_IncreasesCount()
    {
        _buffer.Advance(10);
        
        Assert.Equal(10, _buffer.Count);
    }

    [Fact]
    public void Consume_DecreasesCount()
    {
        var span = _buffer.WriteMemory;
        
        _buffer.Advance(span.Length);
        var sub = span.Length / 2;
        _buffer.Consume(sub);
        
        Assert.Equal(span.Length - sub, _buffer.Count);
    }

    [Fact]
    public void Consume_AllAndCompact_WriteSpanEmpty()
    {
        _buffer.Advance(BufferSize);
        _buffer.Consume(BufferSize);
        _buffer.Compact();

        Assert.Equal(0, _buffer.Count);
        Assert.Equal(BufferSize, _buffer.WriteMemory.Length);
    }
    
    [Fact]
    public void Advance_AfterPartialConsume_RemainCount()
    {
        _buffer.Advance(BufferSize - 1);
        _buffer.Consume(BufferSize - 2);
        _buffer.Compact();
        _buffer.Advance(3);
        
        Assert.Equal(4, _buffer.Count);
    }
    
    [Fact]
    public void Advance_AfterFullAndConsumeAll_CanAdvance()
    {
        _buffer.Advance(BufferSize);
        _buffer.Consume(BufferSize);
        _buffer.Compact();
        _buffer.Advance(3);
        
        Assert.Equal(3, _buffer.Count);
    }
}
