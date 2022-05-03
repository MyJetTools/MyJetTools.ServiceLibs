using System.Diagnostics;
using OpenTelemetry;

namespace MyJetTools.ServiceLibs.Telemetry.ActivityProcessors
{
    public class SpanTraceProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity data)
        {
            base.OnEnd(data);


            var spanId = data.GetSpanId();
            var traceId = data.GetTraceId();
            var parentId = data.GetParentId();
            var appLocation = Environment.GetEnvironmentVariable("APP_LOCATION") ?? "NotFound";

            data.AddTag("Span_Id", spanId);
            data.AddTag("Trace_Id", traceId);
            data.AddTag("Parent_Id", parentId);
            data.AddTag("AppLocation", appLocation);
        }
    }
}