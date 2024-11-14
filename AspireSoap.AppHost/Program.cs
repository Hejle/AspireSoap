var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireSoap_ApiService>("apiservice")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.AspireSoap_Web>("soapservice")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
