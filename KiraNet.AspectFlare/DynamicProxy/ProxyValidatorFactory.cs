﻿namespace KiraNet.AspectFlare.DynamicProxy
{
    public class ProxyValidatorFactory : IProxyValidatorFactory
    {
        public IProxyValidator BuilderValidator()
        {
            return new ProxyValidator();
        }
    }
}
