﻿namespace KiraNet.AspectFlare
{
    public interface ICallingInterceptor: IInterceptor
    {
        void Calling(CallingInterceptContext callingInterceptorContext);
    }
}
