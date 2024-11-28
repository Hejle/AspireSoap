using AspireSoap.ServiceDefaults.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AspireSoap.ServiceDefaults.Middleware;

public class AuditLogMiddleware : IMiddleware
{
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(ILogger<AuditLogMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var requestHeaders = ReadRequestHeaders(context);
        var requestBody = await ReadBodyFromRequest(context.Request);

        _logger.LogInformation("Recieved request: {requestmethod} | {requestpath} | {@requestheaders} | {@requestbody}",
            context.Request.Method, context.Request.Path, requestHeaders, requestBody);

        Stream originalBody = context.Response.Body;
        var newBody = new MemoryStream();
        context.Response.Body = newBody;
        string? responseBody = null;
        try
        {
            await next(context);
        }
        catch (Exception)
        {
            responseBody = await ReadResponseBodyAndAssignResponseToResponseStream(originalBody, newBody);
            throw;
        }
        finally
        {
            responseBody ??= await ReadResponseBodyAndAssignResponseToResponseStream(originalBody, newBody);
            var responseHeaders = ReadResponseHeaders(context);
            _logger.LogInformation("Responded with: {responsecode} | {@responseheader} | {@responsebody}",
                context.Response.StatusCode, responseHeaders.ToJson(), responseBody);
        }
    }

    private static async Task<string> ReadResponseBodyAndAssignResponseToResponseStream(Stream originalBody, MemoryStream newBody)
    {
        var buff = newBody.ToArray();
        var responseBody = Encoding.UTF8.GetString(buff, 0, buff.Length);
        //Write repsonse to the Original Stream
        await originalBody.WriteAsync(buff);
        return responseBody;
    }

    private static HttpResponseHeadersLog ReadResponseHeaders(HttpContext context)
    {
        var uniqueResponseHeaders = context.Response.Headers
                    .ToList()
                    .ConvertAll(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        return new HttpResponseHeadersLog(uniqueResponseHeaders);
    }

    private static HttpRequestHeadersLog ReadRequestHeaders(HttpContext context)
    {
        var uniqueRequestHeaders = context.Request.Headers
                    .ToList()
                    .ConvertAll(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        return new HttpRequestHeadersLog(uniqueRequestHeaders);
    }

    private static async Task<string> ReadBodyFromRequest(HttpRequest request)
    {
        // Ensure the request's body can be read multiple times (for the next middlewares in the pipeline).
        request.EnableBuffering();

        using var streamReader = new StreamReader(request.Body, leaveOpen: true);
        var requestBody = await streamReader.ReadToEndAsync();

        // Reset the request's body stream position for next middleware in the pipeline.
        request.Body.Position = 0;
        return requestBody;
    }
}
