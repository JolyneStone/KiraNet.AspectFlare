using System;
using System.Collections.Generic;
using System.Text;

namespace KiraNet.AspectFlare.Test
{
    internal interface IClass
    {
        void Test();
    }

    internal class ClassBase : IClass
    {
        public ClassBase(int x) { }
        public virtual void Test()
        {

        }
    }

    internal class Class : ClassBase, IClass
    {
        public Class(int x) : base(x)
        {
        }

        public override void Test()
        {
            base.Test();
        }

        void IClass.Test()
        {
            base.Test();
        }
    }
}
