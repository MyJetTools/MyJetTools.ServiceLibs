using System.Text.Json;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace MyJetTools.ServiceLibs.Grpc;

public class ExceptionInterceptor : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger;

    public ExceptionInterceptor(ILogger<ExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sourceAppName = "-none-";
        var sourceAppVersion = "-none-";
        var sourceAppHost = "-none-";
        try
        {
            sourceAppName = context.RequestHeaders?.Get(SourceInterceptor.GrpcSourceAppHostHeader)?.Value;
            sourceAppVersion = context.RequestHeaders?.Get(SourceInterceptor.GrpcSourceAppVersionHeader)?.Value;
            sourceAppHost = context.RequestHeaders?.Get(SourceInterceptor.GrpcSourceAppHostHeader)?.Value;
                
            var resp = await base.UnaryServerHandler(request, context, continuation);

            return resp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GRPC service exception. Path: {path}; Request: {requestJson}; Source: {sourceAppName}.{sourceAppVersion} [{sourceHost}]",
                context.Method, JsonSerializer.Serialize(request), sourceAppName, sourceAppVersion, sourceAppHost);
                
            throw;
        }
    }
}