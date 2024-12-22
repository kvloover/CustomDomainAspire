using CustomDomainAspire.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.CustomDomainAspire_ApiService>("apiservice");

var webfrontend = builder.AddProject<Projects.CustomDomainAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

var gateway = builder.AddProject<Projects.CustomDomainAspire_Gateway>("gateway")
    .WithHttpsEndpoint(port: 443)
    .WithReverseProxyEndpoint("app-dev", "https://app-dev.myapp.com")
    .WithReference(webfrontend)
    .WithReference(apiService);

builder.Build().Run();
