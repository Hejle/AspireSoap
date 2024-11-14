using System.Text;

namespace AspireSoap.Tests;

[TestClass]
public class WebTests
{
    [TestMethod]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireSoap_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("soapservice");
        await resourceNotificationService.WaitForResourceAsync("soapservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var response = await httpClient.GetAsync("/EchoService.svc");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task CallSoapReturnsOk()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AspireSoap_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        var httpClient = app.CreateHttpClient("soapservice");
        await resourceNotificationService.WaitForResourceAsync("soapservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var soapMessage = new StringContent(GetSoapBody(), Encoding.UTF8, "text/xml");
        soapMessage.Headers.Add("SOAPAction", "http://tempuri.org/IEchoService/Echo");
        var response = await httpClient.PostAsync("/EchoService.svc/EchoService", soapMessage);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadAsStringAsync();
        Assert.IsNotNull(result);
    }

    private string GetSoapBody()
    {
        return """
             <soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ns="http://tempuri.org/">
               <soapenv:Header/>
               <soapenv:Body>
                  <ns:Echo>
                     <ns:text />
                  </ns:Echo>
               </soapenv:Body>
            </soapenv:Envelope>
            """;
    }
}
