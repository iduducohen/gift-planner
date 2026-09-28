using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GiftPlanner.Application.Common;

/// <summary>
/// Traces and metrics using only the BCL. The API wires these into OpenTelemetry;
/// nothing here depends on a vendor.
/// </summary>
public static class Telemetry
{
    public const string Name = "GiftPlanner";

    public static readonly ActivitySource Source = new(Name);
    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> LlmInputTokens = Meter.CreateCounter<long>("llm.tokens.input");
    public static readonly Counter<long> LlmOutputTokens = Meter.CreateCounter<long>("llm.tokens.output");
    public static readonly Counter<long> LlmFailures = Meter.CreateCounter<long>("llm.failures");
    public static readonly Counter<long> ComposerFallbacks = Meter.CreateCounter<long>("recommendations.composer.fallbacks");
    public static readonly Counter<long> EmptyResults = Meter.CreateCounter<long>("recommendations.empty");
    public static readonly Histogram<double> LlmLatencyMs = Meter.CreateHistogram<double>("llm.latency", "ms");
    public static readonly Histogram<double> GenerateLatencyMs = Meter.CreateHistogram<double>("recommendations.generate.latency", "ms");
    public static readonly Counter<long> PurchaseIntents = Meter.CreateCounter<long>("purchases.intents");
}
