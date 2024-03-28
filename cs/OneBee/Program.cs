using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

var inputPath = args[0];

var start = Stopwatch.GetTimestamp();
var map = ParseInput(inputPath);
var elapsed = Stopwatch.GetElapsedTime(start);

Console.WriteLine(map);
Console.WriteLine($"Processing took {elapsed.TotalSeconds:F3}s");

return;

static WeatherMap ParseInput(string inputPath)
{
    var map = new WeatherMap();

    using var reader = new Utf8StreamReader(inputPath);

    while (reader.LoadIntoBuffer())
    {
        while (reader.TryReadLine(out var line))
        {
            // Patch out leftover CRLF at buffer boundary
            if (line[^1] is (byte)'\r' or (byte)'\n') line = line[..^1];

            var iSemi = line.IndexOf((byte)';');
            var station = line[..iSemi];
            var temp = ParseTemperature(line.Slice(iSemi + 1));

            map.Update(station, temp);
        }
    }

    return map;
}

// Input is guaranteed to be -99.9..99.9 map to -999..999
static short ParseTemperature(ReadOnlySpan<byte> input)
{
    int index = 0;
    bool isNegative = false;
    if (input[0] == (byte)'-')
    {
        isNegative = true;
        index++;
    }

    var sign = isNegative ? -1 : 1;

    var msd = input[index++] - (byte)'0';

    // Single digit without decimal
    if (index == input.Length) return (short)(sign * 10 * msd);

    // Single digit with decimal
    if (input[index] == (byte)'.')
    {
        index++;
        var ones = input[index] - (byte)'0';
        return (short)(sign * (10 * msd + ones));
    }

    var nsd = input[index++] - (byte)'0';

    // Two digits without decimal
    if (index == input.Length) return (short)(sign * (100 * msd + 10 * nsd));

    // Two digits with decimal
    {
        index++;
        var ones = input[index] - (byte)'0';
        return (short)(sign * (100 * msd + 10 * nsd + ones));
    }
}

struct WeatherData
{
    private short _min;
    private short _max;
    private long _total;
    private long _count;

    public void Init(short initialValue)
    {
        _min = _max = initialValue;
        _total = initialValue;
        _count = 1;
    }

    public void Update(short value)
    {
        if (value < _min) _min = value;
        if (value > _max) _max = value;
        _count++;
        _total += value;
    }

    public override string ToString()
    {
        var mean = _total / _count;
        return $"{_min / 10}.{Math.Abs(_min) % 10}/{mean / 10}.{Math.Abs(mean) % 10}/{_max / 10}.{Math.Abs(_max) % 10}";
    }
}

class WeatherMap
{
    private readonly Dictionary<string, WeatherData> _data = [];

    public void Update(ReadOnlySpan<byte> station, short temp)
    {
        // TODO: Don't...
        var station2 = Encoding.UTF8.GetString(station);

        ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(_data, station2, out var exists);
        if (exists)
            data.Update(temp);
        else
            data.Init(temp);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('{');

        bool isFirst = true;
        foreach (var kvp in _data.OrderBy(x => x.Key))
        {
            if (!isFirst) sb.Append(", ");
            sb.Append($"{kvp.Key}={kvp.Value}");
            isFirst = false;
        }

        sb.Append('}');
        return sb.ToString();
    }
}

// Adapted from https://github.com/Cysharp/Utf8StreamReader/blob/main/src/Utf8StreamReader/Utf8StreamReader.cs
sealed class Utf8StreamReader(string path) : IDisposable
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
                _positionBegin = _positionEnd;
                return true;
            }

            _lastNewLinePosition = _lastExaminedPosition = -2;
            line = default;
            return false;
        }

        line = _inputBuffer.AsSpan(_positionBegin, index);
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