using System.Buffers;

namespace OneBee;

// Adapted from https://github.com/Cysharp/Utf8StreamReader/blob/main/src/Utf8StreamReader/Utf8StreamReader.cs
internal sealed class Utf8StreamReader(string path) : IDisposable
{
    private readonly Stream _stream =
        new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);

    private byte[]? _inputBuffer = ArrayPool<byte>.Shared.Rent(4096);
    private bool _endOfStream;
    private int _lastNewLinePosition = -2;
    private int _lastExaminedPosition;
    private int _positionBegin;
    private int _positionEnd;

    public bool LoadIntoBuffer()
    {
        if (_endOfStream)
        {
            return _positionBegin != _positionEnd;
        }

        if (_lastNewLinePosition >= 0) return true;

        if (_lastNewLinePosition == -1)
        {
            var index = IndexOfNewLine(
                _inputBuffer.AsSpan(_positionBegin, _positionEnd - _positionBegin), out var examined);
            if (index != -1)
            {
                _lastNewLinePosition = _positionBegin + index;
                _lastExaminedPosition = _positionBegin + examined;
                return true;
            }
        }
        else
        {
            _lastNewLinePosition = -1;
        }

        if (_positionEnd != 0 && _positionBegin == _positionEnd)
            _positionBegin = _positionEnd = 0;

        var examined0 = _positionEnd;

        READ_STREAM:
        if (_positionEnd != _inputBuffer!.Length)
        {
            var read = _stream.Read(_inputBuffer.AsSpan(_positionEnd));
            _positionEnd += read;
            if (read == 0)
            {
                _endOfStream = true;
                return _positionBegin != _positionEnd;
            }

            var index = IndexOfNewLine(
                _inputBuffer.AsSpan(examined0, _positionEnd - examined0), out var examined1);
            if (index != -1)
            {
                _lastNewLinePosition = examined0 + index;
                _lastExaminedPosition = examined0 + examined1;
                return true;
            }

            examined0 = _positionEnd;
            goto READ_STREAM;
        }

        if (_positionBegin != 0)
        {
            _inputBuffer.AsSpan(_positionBegin, _positionEnd - _positionBegin).CopyTo(_inputBuffer);
            _positionEnd -= _positionBegin;
            _positionBegin = 0;
            examined0 = _positionEnd;
            goto READ_STREAM;
        }

        // Buffer full without finding newline - kill it
        return false;
    }

    public bool TryReadLine(out ReadOnlySpan<byte> line)
    {
        if (_lastNewLinePosition >= 0)
        {
            line = _inputBuffer.AsSpan(_positionBegin, _lastNewLinePosition - _positionBegin);
            if (line[^1] is (byte)'\r' or (byte)'\n') line = line[..^1];
            _positionBegin = _lastExaminedPosition + 1;
            _lastNewLinePosition = _lastExaminedPosition = -1;
            return true;
        }

        var index = IndexOfNewLine(
            _inputBuffer.AsSpan(_positionBegin, _positionEnd - _positionBegin), out var examined);
        if (index == -1)
        {
            if (_endOfStream && _positionBegin != _positionEnd)
            {
                line = _inputBuffer.AsSpan(_positionBegin, _positionEnd - _positionBegin);
                if (line[^1] is (byte)'\r' or (byte)'\n') line = line[..^1];
                _positionBegin = _positionEnd;
                return true;
            }

            _lastNewLinePosition = _lastExaminedPosition = -2;
            line = default;
            return false;
        }

        line = _inputBuffer.AsSpan(_positionBegin, index);
        if (line[^1] is (byte)'\r' or (byte)'\n') line = line[..^1];
        _positionBegin = _positionBegin + examined + 1;
        _lastNewLinePosition = _lastExaminedPosition = -1;
        return true;
    }

    private static int IndexOfNewLine(ReadOnlySpan<byte> span, out int examined)
    {
        var index = span.IndexOf((byte)'\n');

        if (index == -1)
        {
            examined = span.Length - 1;
            return -1;
        }

        examined = index;

        if (index >= 1 && span[index - 1] == (byte)'\r')
            index--;

        return index;
    }

    public void Dispose()
    {
        if (_inputBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_inputBuffer);
            _inputBuffer = null;
        }

        _stream.Dispose();
    }
}