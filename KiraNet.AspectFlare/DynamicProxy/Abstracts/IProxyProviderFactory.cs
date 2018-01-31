namespace KiraNet.AspectFlare.DynamicProxy
{
    public interface IProxyProviderFactory
    {
        IProxyProvider BuilderProvider(IProxyConfiguration configuration);
    }
}
