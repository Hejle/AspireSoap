using System.Text;

namespace SoapServicesCore.Logging;

public class LogHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LogHeadersMiddleware> _logger;

    public LogHeadersMiddleware(RequestDelegate next, ILogger<LogHeadersMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        //Ensures that the incomming request is a SOAP request.
        if (string.IsNullOrEmpty(context.Request.Headers["SOAPAction"]))
        {
            await _next.Invoke(context);
            return;
        }

        //Switch Response Stream out so we can read from it, but keep a reference of the original stream
        var originalBody = context.Response.Body;
        var newBody = new MemoryStream();
        context.Response.Body = newBody;

        var requestHeaders = ReadRequestHeaders(context);
        var requestBody = await ReadBodyFromRequest(context.Request);

        await _next.Invoke(context);
        string responseBody = await ReadResponseBodyAndAssignResponseToResponseStream(originalBody, newBody);
        var responseHeaders = ReadResponseHeaders(context);

        _logger.LogInformation(requestHeaders.ToString());

        _logger.LogInformation(requestBody);

        _logger.LogInformation(responseHeaders.ToString());

        _logger.LogInformation(responseBody);
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
