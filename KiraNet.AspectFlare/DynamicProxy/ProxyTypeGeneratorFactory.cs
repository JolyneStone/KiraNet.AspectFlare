namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyTypeGeneratorFactory : IProxyTypeGeneratorFactory
    {
        public IProxyTypeGenerator BuilderTypeGenerator(IProxyConfiguration configuration)
        {
            return new ProxyTypeGenerator(configuration);
        }
    }
}
