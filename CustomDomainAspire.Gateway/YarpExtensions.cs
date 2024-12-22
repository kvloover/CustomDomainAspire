namespace CustomDomainAspire.Gateway;

public static class YarpExtensions
{
    public static IReverseProxyBuilder LoadFromReverseProxyMemory(this IReverseProxyBuilder builder, List<ReverseProxyConfig> reverseProxyConfigs)
    {
        var routes = reverseProxyConfigs.Select(YarpConfig.Route).ToList();
        var clusters = reverseProxyConfigs.Select(YarpConfig.Cluster).ToList();
        
        builder.LoadFromMemory(routes, clusters);
        
        return builder;
    }
}