using Yarp.ReverseProxy.Configuration;

namespace CustomDomainAspire.Gateway;

public static class YarpTransforms
{
    public static string PathRemovePrefix = "PathRemovePrefix";
}

public static class YarpConfig
{
    /// <summary>
    /// Create a reverse proxy route pointing to the reverse proxy cluster
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static RouteConfig Route(ReverseProxyConfig config)
    {
        return new RouteConfig()
        {
            RouteId = RouteName(config),
            ClusterId = ClusterName(config),
            Match = new RouteMatch()
            {
                Path = config.Path.TrimEnd('/') + "/{**catchall}"
            },
            Transforms = new List<Dictionary<string, string>>()
            {
                new() { { YarpTransforms.PathRemovePrefix, config.Path.TrimEnd('/') } }
            }
        };
    }

    public static ClusterConfig Cluster(ReverseProxyConfig config)
    {
        return new ClusterConfig()
        {
            ClusterId = ClusterName(config),
            Destinations = new Dictionary<string, DestinationConfig>()
            {
                {
                    DestinationName(config),
                    new DestinationConfig()
                    {
                        Address = config.BackendUrl,
                    }
                }
            }
        };
    }

    public static string RouteName(ReverseProxyConfig config)
    {
        return $"route-{config.ClientName}";
    }

    public static string ClusterName(ReverseProxyConfig config)
    {
        return $"cluster-{config.ClientName}";
    }

    public static string DestinationName(ReverseProxyConfig config)
    {
        return $"dest-{config.ClientName}";
    }
}

public class ReverseProxyConfig
{
    public required string ClientName { get; init; }
    public required string BackendUrl { get; init; }
    public required string Path { get; init; }

    public static ReverseProxyConfig Create(string clientName, string path)
    {
        return new ReverseProxyConfig()
        {
            BackendUrl = $"http://{clientName}",
            ClientName = clientName,
            Path = path
        };
    }
}