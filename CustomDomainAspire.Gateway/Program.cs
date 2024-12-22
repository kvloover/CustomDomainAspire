using CustomDomainAspire.Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddServiceDiscovery()
    .AddReverseProxy()
    .LoadFromReverseProxyMemory([
        ReverseProxyConfig.Create("apiService", "api"),
        ReverseProxyConfig.Create("webFrontend", "web"),
    ])
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();