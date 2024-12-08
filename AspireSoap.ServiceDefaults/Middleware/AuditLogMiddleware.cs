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
        using (var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            { "RequestHeaders", requestHeaders.ToJson() },
            { "RequestBody", requestBody },
        }))
        {
            _logger.LogInformation("Received incoming {RequestMethod} request on {RequestPath}.", context.Request.Method, context.Request.Path);
        }

        Stream originalBody = context.Response.Body;
        var newBody = new MemoryStream();
        context.Response.Body = newBody;
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while processing the request.");
            throw;
        }
        finally
        {
            var responseBody = await ReadResponseBodyAndAssignResponseToResponseStream(originalBody, newBody);
            var responseHeaders = ReadResponseHeaders(context);
            using (var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                { "ResponseHeaders", responseHeaders.ToJson() },
                { "ResponseBody", responseBody },
            }))
            {
                _logger.LogInformation("Responded with: {ResponseCode} {RequestPath}.", context.Response.StatusCode, context.Request.Path);
            }
        }
    }

    private static async Task<string> ReadResponseBodyAndAssignResponseToResponseStream(Stream originalBody, MemoryStream newBody)
    {
        if(newBody.Length == 0)
        {
            return string.Empty;
        }
        var buff = newBody.ToArray();
        var responseBody = Encoding.UTF8.GetString(buff, 0, buff.Length);
        //Write repsonse to the Original Stream
        await originalBody.WriteAsync(buff);
        return responseBody;
    }

    private static HttpHeadersLog ReadResponseHeaders(HttpContext context)
    {
        var uniqueResponseHeaders = context.Response.Headers
                    .ToList()
                    .ConvertAll(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        return new HttpHeadersLog(uniqueResponseHeaders);
    }

    private static HttpHeadersLog ReadRequestHeaders(HttpContext context)
    {
        var uniqueRequestHeaders = context.Request.Headers
                    .ToList()
                    .ConvertAll(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        return new HttpHeadersLog(uniqueRequestHeaders);
    }

    private static async Task<string?> ReadBodyFromRequest(HttpRequest request)
    {
        if(request.ContentLength == 0 || request.ContentLength is null)
        {
            return string.Empty;
        }
        // Ensure the request's body can be read multiple times (for the next middlewares in the pipeline).
        request.EnableBuffering();

        using var streamReader = new StreamReader(request.Body, leaveOpen: true);
        var requestBody = await streamReader.ReadToEndAsync();

        // Reset the request's body stream position for next middleware in the pipeline.
        request.Body.Position = 0;
        return requestBody;
    }
}
