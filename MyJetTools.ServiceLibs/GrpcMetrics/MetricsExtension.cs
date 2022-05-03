using Prometheus;

namespace MyJetTools.ServiceLibs.GrpcMetrics;

public static class MetricsExtension
{
    public static Gauge CreateGauge(this string metricName, string description, params string[] labels)
    {
        return Metrics.CreateGauge(metricName, description, new GaugeConfiguration {LabelNames = labels});
    }

    public static Counter CreateCounter(this string metricName, string description, params string[] labels)
    {
        return Metrics.CreateCounter(metricName, description, new CounterConfiguration {LabelNames = labels});
    }

    public static Histogram CreateHistogram(this string metricName, string description, double[] buckets,
        params string[] labels)
    {
        return Metrics.CreateHistogram(metricName, description, new HistogramConfiguration
        {
            LabelNames = labels,
            Buckets = new[] {double.PositiveInfinity}
        });
    }
}