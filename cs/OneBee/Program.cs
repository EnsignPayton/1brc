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
        var temp = float.Parse(line.AsSpan(iSemi + 1));

        map.Update(station, temp);
    }

    return map;
}

struct WeatherData
{
    private float _min;
    private float _max;
    private float _mean;
    private long _count;

    public void Init(float initialValue)
    {
        _min = _max = _mean = initialValue;
        _count = 1;
    }

    public void Update(float value)
    {
        if (value < _min) _min = value;
        if (value > _max) _max = value;
        _count++;

        var lastMean = _mean;
        _mean = lastMean + ((value - lastMean) / _count);
    }

    public override string ToString() => $"{_min:F1}/{_mean:F1}/{_max:F1}";
}

class WeatherMap
{
    private readonly Dictionary<string, WeatherData> _data = [];

    public void Update(string station, float temp)
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