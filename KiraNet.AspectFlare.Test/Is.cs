using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using KiraNet.AspectFlare.DynamicProxy;

namespace KiraNet.AspectFlare.Test
{
    [Calling]
    public interface ITs
    {
        int T0();
        int T1(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2);
    }

    public class Tss : ITs
    {
        public Tss(int x) { }
        public int T0()
        {
            var its = typeof(ITs).FullName;
            var ms = typeof(ITs).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(x => its + "." + x.Name);
            var t = typeof(Tss1);
            foreach (var x in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (ms.Contains(x.Name))
                {
                    Console.WriteLine(x.Name);
                }
            }

            var ms1 = typeof(ITs).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(x => x.Name);
            var t1 = typeof(Tss);
            foreach (var x in t1.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ms1.Contains(x.Name))
                {
                    Console.WriteLine(x.Name);
                }
            }

            return 1;
        }

        public int T1(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2)
        {
            y = default(InterceptResult);
            ex2 = null;
            return 1;
        }
    }

    public class Tss1 : ITs
    {
        private ITs _ts;
        private InterceptorWrapperCollection _wrappers;
        private ReturnCaller<int> _caller0;
        private ReturnCaller<int> _caller1;

        public Tss1(int x)
        {
            Init();
            _ts = new Tss(x);
        }

        private void Init()
        {
            _wrappers = new InterceptorWrapperCollection(typeof(ITs), typeof(T), typeof(Tss1));
        }

        int ITs.T0()
        {
            if (_caller0 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller0 = new ReturnCaller<int>(wrapper);
            }

            var parameters = new object[1];
            Func<int> call = () => _ts.T0();
            return _caller0.Call(this, call, null);
        }

        //int ITs.T1(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2)
        //{
        //    if (_caller1 == null)
        //    {
        //        InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
        //        _caller1 = new ReturnCaller<int>(wrapper);
        //    }

        //    object[] parameters = new object[4];
        //    InterceptResult x1 = x;
        //    parameters[0] = x;
        //    InterceptResult y1 = default(InterceptResult);
        //    parameters[1] = y1;
        //    Exception ex11 = ex1;
        //    parameters[2] = ex1;
        //    Exception ex22 = null;
        //    parameters[3] = ex22;
        //    Func<int> call = () => _ts.T1(ref x1, out y1, ref ex11, out ex22);
        //    y = y1;
        //    ex2 = ex22;
        //    return _caller1.Call(this, call, parameters);
        //}

        private InterceptorWrapper _wrapper1;

        ref InterceptResult T2(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2)
        {
            y = default(InterceptResult);
            ex2 = null;
            return ref x;
        }

        int ITs.T1(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2)
        {
            if (_wrapper1 == null)
            {
                _wrapper1 = _wrappers.GetWrapper(10000);
            }

            object[] parameters = new object[4];
            parameters[0] = x;
            InterceptResult y1 = default(InterceptResult);
            parameters[1] = y1;
            parameters[2] = ex1;
            Exception ex22 = null;
            parameters[3] = ex22;
            InterceptResult result;
            int returnVaule = default(int);

            try
            {
                result = _wrapper1.CallingIntercepts(this, parameters);
                if (result.HasResult)
                {
                    y = y1;
                    ex2 = ex22;
                    return (int)result.Result;
                }
                // 调用基类方法
                returnVaule = _ts.T1(ref x, out y, ref ex1, out ex22);

                result = _wrapper1.CalledIntercepts(this, parameters, returnVaule);
                if (result.HasResult)
                {
                    y = y1;
                    ex2 = ex22;
                    return (int)result.Result;
                }

                y = y1;
                ex2 = ex22;
                return returnVaule;
            }
            catch (Exception ex)
            {
                result = _wrapper1.ExceptionIntercept(this, parameters, returnVaule, ex);
                if (result.HasResult)
                {
                    y = y1;
                    ex2 = ex22;
                    return (int)result.Result;
                }

                throw ex;
            }
        }
    }
}
