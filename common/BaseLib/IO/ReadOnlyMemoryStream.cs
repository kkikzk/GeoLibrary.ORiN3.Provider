namespace GeoLibrary.ORiN3.Provider.BaseLib.IO;

public class ReadOnlyMemoryStream : Stream
{
    private readonly ReadOnlyMemory<byte> _memory;
    private int _position;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _memory.Length;
    public override long Position { get => _position; set => _position = (int)value; }

    public ReadOnlyMemoryStream(ReadOnlyMemory<byte> memory)
    {
        _memory = memory;
        _position = 0;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = _memory.Span[_position..];
        var toCopy = Math.Min(remaining.Length, count);
        remaining[..toCopy].CopyTo(buffer.AsSpan(offset, toCopy));
        _position += toCopy;
        return toCopy;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return Position = origin switch
        {
            SeekOrigin.Begin => (int)offset,
            SeekOrigin.Current => _position + (int)offset,
            SeekOrigin.End => _memory.Length + (int)offset,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
