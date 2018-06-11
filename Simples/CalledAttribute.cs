using System;
using KiraNet.AspectFlare;

namespace Simples
{
    public class CalledAttribute : CalledInterceptAttribute
    {
        public override void Called(CalledInterceptContext calledInterceptorContext)
        {
            Console.WriteLine("Called in " + calledInterceptorContext.Owner.ToString());
        }
    }
}