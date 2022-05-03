using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Grpc.Core;
using Grpc.Core.Interceptors;
using OpenTelemetry.Trace;
using Prometheus;

namespace MyJetTools.ServiceLibs.GrpcMetrics;

public class PrometheusMetricsInterceptor : Interceptor
{
    public static string AppName { get; set; }
    public static string AppVersion { get; set; }
    public static string AppHost { get; set; }


    private static readonly string HostName;

    public const string GrpcSourceAppNameHeader = "source-app-name";
    public const string GrpcSourceAppVersionHeader = "source-app-name";
    public const string GrpcSourceAppHostHeader = "source-app-name";

    #region ServerMetrics
    
    
    static PrometheusMetricsInterceptor()
    {
        var name = Assembly.GetEntryAssembly()?.GetName();

        AppName = name?.Name ?? "--none--";
        AppVersion = name?.Version?.ToString() ?? "Not Found";
            
        HostName = Environment.GetEnvironmentVariable("HOST") 
                   ?? Environment.GetEnvironmentVariable("HOSTNAME") 
                   ?? AppName;

        AppHost = HostName;
    }

    private static readonly Gauge ServerGrpcCallProcessCount = "grpc_server_active_call_count"
        .CreateGauge("Counter of active calls of grpc methods.", "host", "controller", "method");

    private static readonly Counter ServerGrpcCallOutCount = "grpc_server_call_out_count"
        .CreateCounter("Counter of calls grpc methods. Counter applied after execute logic", "host", "controller",
            "method", "status");

    private static readonly Counter ServerGrpcCallInCount = "grpc_server_call_in_count"
        .CreateCounter("Counter of calls grpc methods. Counter applied before execute logic", "host", "controller",
            "method");

    private static readonly Histogram ServerGrpcCallDelaySec = "grpc_server_call_delay_sec"
        .CreateHistogram("Histogram of grpc call delay in second.", new[] {double.PositiveInfinity}, "host",
            "controller", "method");

    #endregion

    #region ClientMetrics

    private static readonly Counter ClientGrpcCallInCount = "grpc_client_call_in_count"
        .CreateCounter("Counter of calls grpc methods. Counter applied before execute logic", "host", "controller",
            "method");

    private static readonly Counter ClientGrpcCallOutCount = "grpc_client_call_out_count"
        .CreateCounter("Counter of calls grpc methods. Counter applied after execute logic", "host", "controller",
            "method", "status");

    private static readonly Gauge ClientGrpcCallProcessCount = "grpc_client_call_process_count"
        .CreateGauge("Counter of active calls of grpc methods.", "host", "controller",
            "method");

    private static readonly Histogram ClientGrpcCallDelaySec = "grpc_client_call_delay_sec"
        .CreateHistogram("Histogram of grpc call delay in second.", new[] {double.PositiveInfinity}, "host",
            "controller", "method");

    #endregion

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var method = context.Method ?? "MethodNotFound";
        var prm = context.Method.Split('/');
        var controller = prm.Length >= 2 ? prm[1] : "unknown";

        var sourceAppName = context.RequestHeaders?.Get(GrpcSourceAppHostHeader)?.Value ?? "-none-";
        var sourceAppVersion = context.RequestHeaders?.Get(GrpcSourceAppVersionHeader)?.Value ?? "-none-";
        var sourceAppHost = context.RequestHeaders?.Get(GrpcSourceAppHostHeader)?.Value ?? "-none-";

        Activity.Current?.AddTag("call-source-AppName", sourceAppName);
        Activity.Current?.AddTag("call-source-AppVersion", sourceAppVersion);
        Activity.Current?.AddTag("call-source-AppAppHost", sourceAppHost);

        using (ServerGrpcCallProcessCount.WithLabels(HostName, controller, method).TrackInProgress())
        {
            using (ServerGrpcCallDelaySec.Labels(HostName, controller, method).NewTimer())
            {
                ServerGrpcCallInCount.WithLabels(HostName, controller, method).Inc();

                try
                {
                    var resp = await continuation(request, context);

                    ServerGrpcCallOutCount
                        .WithLabels(HostName, controller, method, context.Status.StatusCode.ToString()).Inc();

                    return resp;
                }
                catch (Exception ex)
                {
                    ServerGrpcCallOutCount.WithLabels(HostName, controller, method, "exception").Inc();
                    Activity.Current?.RecordException(ex);
                    if (request != null)
                        Activity.Current?.AddTag("grpc-request", JsonSerializer.Serialize(request));
                    throw;
                }
            }
        }
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var method = context.Method.Name ?? "MethodNotFound";
        var controller = context.Method.ServiceName ?? "MethodNotFound";

        context.Options.Headers?.Add(GrpcSourceAppNameHeader, AppName);
        context.Options.Headers?.Add(GrpcSourceAppVersionHeader, AppVersion);
        context.Options.Headers?.Add(GrpcSourceAppHostHeader, AppHost);

        using (ClientGrpcCallProcessCount.WithLabels(HostName, controller, method).TrackInProgress())
        {
            using (ClientGrpcCallDelaySec.Labels(HostName, controller, method).NewTimer())
            {
                ClientGrpcCallInCount.WithLabels(HostName, controller, method).Inc();

                try
                {
                    var resp = continuation(request, context);

                    ClientGrpcCallOutCount.WithLabels(HostName, controller, method, "success").Inc();

                    return resp;
                }
                catch (Exception ex)
                {
                    ClientGrpcCallOutCount.WithLabels(HostName, controller, method, "exception").Inc();
                    Activity.Current?.RecordException(ex);
                    if (request != null)
                        Activity.Current?.AddTag("grpc-request", JsonSerializer.Serialize(request));
                    throw;
                }
            }
        }
    }
}