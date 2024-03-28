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

    using var reader = new StreamReader(inputPath);

    while (!reader.EndOfStream)
    {
        var line = reader.ReadLine();
        if (line is null) break;

        var iSemi = line.IndexOf(';');
        var station = line[..iSemi];
        var temp = ParseTemperature(line.AsSpan(iSemi + 1));

        map.Update(station, temp);
    }

    return map;
}

// Input is guaranteed to be -99.9..99.9 map to -999..999
static short ParseTemperature(ReadOnlySpan<char> input)
{
    int index = 0;
    bool isNegative = false;
    if (input[0] == '-')
    {
        isNegative = true;
        index++;
    }

    var sign = isNegative ? -1 : 1;

    var msd = input[index++] - '0';

    // Single digit without decimal
    if (index == input.Length) return (short)(sign * 10 * msd);

    // Single digit with decimal
    if (input[index] == '.')
    {
        index++;
        var ones = input[index] - '0';
        return (short)(sign * (10 * msd + ones));
    }

    var nsd = input[index++] - '0';

    // Two digits without decimal
    if (index == input.Length) return (short)(sign * (100 * msd + 10 * nsd));

    // Two digits with decimal
    {
        index++;
        var ones = input[index] - '0';
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

    public void Update(string station, short temp)
    {
        ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(_data, station, out var exists);
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