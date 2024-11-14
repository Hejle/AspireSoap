using CoreWCF;
using SoapServicesCore.ServiceContracts;
using System.Security.Principal;

namespace AspireSoap.Web.Services
{
    public class EchoService : IEchoService
    {
        private readonly WeatherApiClient _weatherApiClient;

        public EchoService(WeatherApiClient weatherApiClient)
        {
            _weatherApiClient = weatherApiClient;
        }

        public async Task<string> Echo(string text)
        {
            var weather = (await _weatherApiClient.GetWeatherAsync(1)).First();
            return $"Weather is {weather.Summary} with {weather.TemperatureC} degrees in C.";
        }

        public string ComplexEcho(EchoMessage text)
        {
            if (text is null)
            {
                return string.Empty;
            }
            return text.Text;
        }

        public string FailEcho(string text)
            => throw new FaultException<EchoFault>(new EchoFault() { Text = "WCF Fault OK" }, new FaultReason("FailReason"));

        public PingOutput Ping()
        {
            return new PingOutput() { Result = true };
        }
    }
}
