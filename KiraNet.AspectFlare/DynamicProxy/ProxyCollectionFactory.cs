﻿namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyCollectionFactory : IProxyCollectionFactory
    {
        public IProxyCollection BuilderCollection()
        {
            return new ProxyCollection();
        }
    }
}
