//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Text;
//using Xunit;

//namespace KiraNet.AspectFlare.Test
//{
//    public class ReflectionTest
//    {
//        [Fact]
//        public void InvokeBaseCtor()
//        {
//            var reflection = new Reflection();
//            Assert.NotNull(reflection);
//        }
//    }

//    public class ReflectionBase
//    {
//        public ReflectionBase()
//        {

//        }
//    }

//    public class Reflection : ReflectionBase
//    {
//        public Reflection()
//        {
//            var method = typeof(ReflectionBase).GetConstructor(BindingFlags.CreateInstance | BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);

//            var returnValue = method.Invoke(this, null);
//            Assert.NotNull(returnValue);
//        }
//    }
//}
