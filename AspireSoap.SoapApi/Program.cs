using CoreWCF.Configuration;
using CoreWCF.Description;
using CoreWCF.Channels;
using CoreWCF;
using AspireSoap.ServiceDefaults;
using AspireSoap.SoapApi.ServiceContracts;
using AspireSoap.SoapApi.Logging;
using AspireSoap.SoapApi.Behavior;
using AspireSoap.SoapApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.AllowSynchronousIO = true;
});

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});

builder.Services.AddScoped<EchoService>();
builder.Services.AddSingleton<LogMessageBehavior>();
builder.Services.AddSingleton<LogMessageInspector>();

// Add WSDL support
builder.Services.AddServiceModelServices().AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();

var app = builder.Build();

app.UseMiddleware<LogHeadersMiddleware>();

app.UseServiceModel(builder =>
{
    builder
    .AddService<EchoService>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = true;
        serviceOptions.BaseAddresses.Add(new Uri("https://soapservice/EchoService.svc"));
    })
    // Add a BasicHttpBinding at a specific endpoint with Behavior
    .AddServiceEndpoint<EchoService, IEchoService>(new BasicHttpBinding(BasicHttpSecurityMode.Transport), "/EchoService", endpointOptions =>
    {
        endpointOptions.EndpointBehaviors.Add(app.Services.GetRequiredService<LogMessageBehavior>());
    });
});

var serviceMetadataBehavior = app.Services.GetRequiredService<ServiceMetadataBehavior>();
serviceMetadataBehavior.HttpsGetEnabled = true;

app.Run();
