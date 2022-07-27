namespace BasicReverseProxy.Core.RouteForwarding.Client
{
    public interface IWebClientBuilderFactory
    {
        IWebClientBuilder CreateWebClientBuilder();
    }
}