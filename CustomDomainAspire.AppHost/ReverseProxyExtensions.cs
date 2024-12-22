using System.Collections.Immutable;
using System.Net.Sockets;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

/// <summary>
/// https://anthonysimmon.com/dotnet-aspire-non-localhost-endpoints/
/// </summary>

namespace CustomDomainAspire.AppHost;

public sealed class ReverseProxyEndpointAnnotation : IResourceAnnotation
{
    public ReverseProxyEndpointAnnotation(string name, string url)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Instantiating an EndpointAnnotation triggers the built-in, internal validation of the endpoint name.
        _ = new EndpointAnnotation(ProtocolType.Tcp, name: name);

        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"'{url}' is not an absolute URL.", nameof(url));
        }

        this.Name = name;
        this.Url = url;
    }

    public string Name { get; }

    public string Url { get; }
}

internal static class ReverseProxyExtensions
{
    public static IResourceBuilder<T> WithReverseProxyEndpoint<T>(this IResourceBuilder<T> builder, string name, string url)
        where T : IResource
    {
        // Best effort to prevent duplicate endpoint names
        if (builder.Resource.Annotations.OfType<EndpointAnnotation>().Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new DistributedApplicationException($"Endpoint with name '{name}' already exists.");

        if (builder.Resource.Annotations.OfType<ReverseProxyEndpointAnnotation>().Any(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new DistributedApplicationException($"Endpoint with name '{name}' already exists.");

        builder.ApplicationBuilder.Services.TryAddLifecycleHook<ReverseProxyLifecycleHook>();
        return builder.WithAnnotation(new ReverseProxyEndpointAnnotation(name, url));
    }

    private sealed class ReverseProxyLifecycleHook(ResourceNotificationService notificationService, ILogger<ReverseProxyLifecycleHook> logger) : IDistributedApplicationLifecycleHook, IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private Task? _task;

        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            this._task = this.EnsureUrlsAsync(this._cts.Token);
            return Task.CompletedTask;
        }

        private async Task EnsureUrlsAsync(CancellationToken cancellationToken)
        {
            await foreach (var evt in notificationService.WatchAsync(cancellationToken))
            {
                if (evt.Snapshot.State != KnownResourceStates.Running)
                {
                    // By default, .NET Aspire only displays endpoints for running resources.
                    continue;
                }

                var urlsToAdd = ImmutableArray.CreateBuilder<UrlSnapshot>();

                foreach (var endpoint in evt.Resource.Annotations.OfType<ReverseProxyEndpointAnnotation>())
                {
                    var urlAlreadyAdded = evt.Snapshot.Urls.Any(x => string.Equals(x.Name, endpoint.Name, StringComparison.OrdinalIgnoreCase));
                    if (!urlAlreadyAdded)
                    {
                        urlsToAdd.Add(new UrlSnapshot(endpoint.Name, endpoint.Url, IsInternal: false));
                    }
                }

                if (urlsToAdd.Count > 0)
                {
                    await notificationService.PublishUpdateAsync(evt.Resource, snapshot => snapshot with
                    {
                        Urls = snapshot.Urls.AddRange(urlsToAdd)
                    });
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await this._cts.CancelAsync();

            if (this._task != null)
            {
                try
                {
                    await this._task;
                }
                catch (OperationCanceledException)
                {
                    // Application is shutting down
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while adding reverse proxy endpoints.");
                }
            }

            this._cts.Dispose();
        }
    }
}