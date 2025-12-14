#region

using System.Diagnostics;

#endregion

namespace ConcreteEngine.Common.Time;

public sealed class FrameProfiler(int sampleFrames, int sampleFrameRate)
{
    private readonly Stopwatch _sw = new();

    private readonly double _targetFrameMs = 1.0 / sampleFrameRate * 1000.0;

    private readonly string[] _names = new string[8];
    private readonly FrameSamplerCounter[] _samplers = new FrameSamplerCounter[8];
    private int _idx;

    private int _boundIndex = -1;

    public bool Enabled = true;

    public void Register(string name)
    {
        _names[_idx] = name;
        _samplers[_idx] = new FrameSamplerCounter();
        _idx++;
    }

    public void Begin(int index)
    {
        if (!Enabled) return;
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(_boundIndex, 0);
        _sw.Restart();
        _boundIndex = index;
    }

    public bool End()
    {
        if (!Enabled) return false;

        ArgumentOutOfRangeException.ThrowIfNegative(_boundIndex);

        bool res = false;
        _sw.Stop();
        ref var sample = ref _samplers[_boundIndex];
        if (sample.End(_sw.ElapsedTicks, sampleFrames))
        {
            var name = _names[_boundIndex];
            Console.WriteLine(sample.GetResultString(name, _targetFrameMs));
            res = true;
        }

        _boundIndex = -1;
        return res;
    }

    public void PrintTotal()
    {
        double sum = 0;
        for (int i = 0; i < _idx; i++)
        {
            sum += _samplers[i].LastAvgMs;
        }

        var pct = sum / _targetFrameMs * 100.0;
        const string prefix = "Total: ";
        Console.WriteLine($"{prefix,-12} - {sum:F6} ms (~{pct:F2}% frame)");
    }


    private struct FrameSamplerCounter
    {
        public double LastAvgMs;
        private long _totalTicks;
        private int _samples;
        private int _frameCounter;

        public bool End(long elapsedTicks, long sampleFrames)
        {
            _totalTicks += elapsedTicks;
            _frameCounter++;
            _samples++;

            if (_frameCounter >= sampleFrames)
            {
                LastAvgMs = _totalTicks / (double)_samples * 1000.0 / Stopwatch.Frequency;

                _frameCounter = 0;
                _totalTicks = 0;
                _samples = 0;

                return true;
            }

            return false;
        }

        public readonly string GetResultString(string name, double targetFrameMs)
        {
            if (LastAvgMs <= 0) return "Waiting for data...";
            var pct = LastAvgMs / targetFrameMs * 100.0;
            return $"{name,-12} - {LastAvgMs:F6} ms (~{pct:F2}% frame)";
        }
    }
}