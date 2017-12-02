using System;

namespace KiraNet.AspectFlare.Validator
{
    public interface IProxyValidator
    {
        bool Validate(Type serviceType, Type proxy);
    }
}
