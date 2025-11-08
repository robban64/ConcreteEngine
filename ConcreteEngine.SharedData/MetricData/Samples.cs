namespace ConcreteEngine.Shared.MetricData;

public readonly record struct RenderInfoSample(float Fps, float Alpha, int Passes, int Draws, int Tris);
