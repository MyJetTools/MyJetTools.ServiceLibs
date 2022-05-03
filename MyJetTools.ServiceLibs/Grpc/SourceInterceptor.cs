using Grpc.Core;
using Grpc.Core.Interceptors;

namespace MyJetTools.ServiceLibs.Grpc;

public class SourceInterceptor : Interceptor
{
    public static string AppName { get; set; }
    public static string AppVersion { get; set; }
    public static string AppHost { get; set; }

    public const string GrpcSourceAppNameHeader = "source-app-name";
    public const string GrpcSourceAppVersionHeader = "source-app-name";
    public const string GrpcSourceAppHostHeader = "source-app-name";

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        context.Options.Headers?.Add(GrpcSourceAppNameHeader, AppName);
        context.Options.Headers?.Add(GrpcSourceAppVersionHeader, AppVersion);
        context.Options.Headers?.Add(GrpcSourceAppHostHeader, AppHost);
            
        return base.BlockingUnaryCall(request, context, continuation);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        context.Options.Headers?.Add(GrpcSourceAppNameHeader, AppName);
        context.Options.Headers?.Add(GrpcSourceAppVersionHeader, AppVersion);
        context.Options.Headers?.Add(GrpcSourceAppHostHeader, AppHost);
            
        return base.AsyncUnaryCall(request, context, continuation);
    }
}