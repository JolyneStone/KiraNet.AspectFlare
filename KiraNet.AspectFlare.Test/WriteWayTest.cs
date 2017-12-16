using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using KiraNet.AspectFlare.DynamicProxy;

namespace KiraNet.AspectFlare.Test
{
    public interface IClass
    {
        void Test();
    }

    public class ClassBase : IClass
    {
        public ClassBase(int x) { }

        public ClassBase(int x, params char[] chs)
        {

        }

        public ClassBase(ref int x, out char y, ref InterceptResult r, out InterceptResult u, int z, int t, out Exception ex1, ref Exception ex2, Exception ex3, Exception ex4, Exception ex5, Exception ex6, Exception ex7)
        {
            y = '1';
            u = default(InterceptResult);
            ex1 = null;
        }

        public virtual void Test()
        {

        }

        public virtual void NoReturnAndParameter()
        {

        }

        protected internal void T1()
        {

        }


        internal void T2()
        {

        }

        protected virtual void NoReturn(ref int x, out int y, ref InterceptResult result, Exception exception)
        {
            y = 1;
        }

        public virtual Exception HasReturnAndNoParameter()
        {
            return new Exception();
        }

        public virtual int HasReturn(ref int x, out int y, ref InterceptResult result, Exception exception)
        {
            y = 1;
            return y;
        }

        public virtual async void AsyncT1(int x, int y, int z, int t)
        {
            await Task.Delay(2000);
        }

        public virtual async Task AsyncT2()
        {
            await Task.Delay(1000);
            await Task.Delay(2000);
        }

        public virtual async Task<int> AsyncT3()
        {
            await Task.Delay(1000);
            return 1;
        }

        public virtual async ValueTask<int> AsyncT4()
        {
            await Task.Delay(1000);
            return 1;
        }

        public virtual async Task<Exception> AsyncT5()
        {
            await Task.Delay(1000);
            return new Exception();
        }

        public virtual async ValueTask<Exception> AsyncT6(int x, int y)
        {
            await Task.Delay(1000);
            return new Exception((x + y).ToString());
        }

        public virtual async Task<Exception> AsyncT7(string x, string y, Exception ex1, Exception ex2, params object[] objs)
        {
            await Task.Delay(1000);
            return new Exception((x + y).ToString() + ex1.Message + objs?.Length, ex2);
        }

        public virtual async Task<string> AsyncT8(InterceptResult result)
        {
            await Task.Delay(1000);
            return result.ToString();
        }

        public virtual async ValueTask<long> AsyncT9(long l)
        {
            await Task.Delay(1000);
            return ++l;
        }

        public virtual async Task<string> TaskAsyncT(int x, int y)
        {
            await Task.Delay(1000);
            return $"{x}+{y}";
        }
    }

    public class Class : ClassBase, IClass
    {
        private InterceptorWrapperCollection _wrappers;

        public Class(int x, params char[] chs) : base(x, chs)
        {
            Init();
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                x = 1;
            }

            var parameters = new object[2];
            parameters[0] = x;
            parameters[1] = chs;
            //object[] parameters = null;

            InterceptResult result;
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                // 调用基类方法

                result = wrapper.CalledIntercepts(this, ".ctor", this);
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, this, ex);
            }
        }


        public Class(ref int x, out char y, ref InterceptResult r, out InterceptResult u, int z, int t, out Exception ex1, ref Exception ex2, Exception ex3, Exception ex4, Exception ex5, Exception ex6, Exception ex7) : base(ref x, out y, ref r, out u, z, t, out ex1, ref ex2, ex3, ex4, ex5, ex6, ex7)
        {
            Init();
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                ex1 = new Exception();
            }

            //var parameters = new object[] { x, y, r, u, z, t, ex1, ex2, ex3, ex4, ex5, ex6, ex7 };
            var parameters = new object[13];
            parameters[0] = x;
            y = default(Char);
            parameters[1] = y;
            parameters[2] = r;
            u = default(InterceptResult);
            parameters[3] = u;
            parameters[4] = z;
            parameters[5] = t;
            ex1 = default(Exception);
            parameters[6] = ex1;
            parameters[7] = ex2;
            parameters[8] = ex3;
            parameters[9] = ex4;
            parameters[10] = ex5;
            parameters[11] = ex6;
            parameters[12] = ex7;

            InterceptResult result;
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);

                result = wrapper.CallingIntercepts(this, ".ctor", null);
                // 调用基类方法

                result = wrapper.CalledIntercepts(this, ".ctor", this);
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, this, ex);
            }
        }


        public Class(int x) : base(x)
        {
            Init();

            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
            }

            InterceptResult result;
            Object[] parameters = new object[] { x };
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);

                result = wrapper.CallingIntercepts(this, ".ctor", null);
                // 调用基类方法

                result = wrapper.CalledIntercepts(this, ".ctor", null);
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, null, ex);
            }
        }


        private void Init()
        {
            var type = typeof(ClassBase);
            _wrappers = new InterceptorWrapperCollection(type);
        }

        public override void Test()
        {
            base.Test();
        }

        void IClass.Test()
        {
            base.Test();
        }

        public override void NoReturnAndParameter()
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                base.NoReturnAndParameter();
                return;
            }

            Object[] parameters = null;
            try
            {

                if (wrapper.CallingIntercepts(this, ".ctor", parameters).HasResult)
                {
                    return;
                }

                // 调用基类方法
                base.NoReturnAndParameter();

                wrapper.CalledIntercepts(this, ".ctor", null);
            }
            catch (Exception ex)
            {
                if (wrapper.ExceptionIntercept(this, ".ctor", parameters, null, ex).HasResult)
                {
                    return;
                }

                throw ex;
            }
        }

        public override Exception HasReturnAndNoParameter()
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                return base.HasReturnAndNoParameter();
            }

            Object[] parameters = null;
            InterceptResult result;
            Exception returnVaule = null;
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return (Exception)result.Result;
                }
                // 调用基类方法
                returnVaule = base.HasReturnAndNoParameter();

                result = wrapper.CalledIntercepts(this, ".ctor", returnVaule);
                if (result.HasResult)
                {
                    return (Exception)result.Result;
                }

                return returnVaule;
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, returnVaule, ex);
                if (result.HasResult)
                {
                    return (Exception)result.Result;
                }

                throw ex;
            }
        }

        public override int HasReturn(ref int x, out int y, ref InterceptResult z, Exception exception)
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                return base.HasReturn(ref x, out y, ref z, exception);
            }

            Object[] parameters = new object[4];
            parameters[0] = x;
            y = default(int);
            parameters[1] = y;
            parameters[2] = z;
            parameters[3] = exception;
            InterceptResult result;
            int returnVaule = default(int);

            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return (int)result.Result;
                }
                // 调用基类方法
                returnVaule = base.HasReturn(ref x, out y, ref z, exception);

                result = wrapper.CalledIntercepts(this, ".ctor", returnVaule);
                if (result.HasResult)
                {
                    return (int)result.Result;
                }

                return returnVaule;
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, returnVaule, ex);
                if (result.HasResult)
                {
                    return (int)result.Result;
                }

                throw ex;
            }
        }

        public override void AsyncT1(int x, int y, int z, int t)
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                base.AsyncT1(x, y, z, t);
                return;
            }

            Object[] parameters = new object[4];
            parameters[0] = x;
            parameters[1] = y;
            parameters[2] = z;
            parameters[3] = t;
            InterceptResult result;

            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return;
                }

                // 调用基类方法
                base.AsyncT1(x, y, z, t);

                wrapper.CalledIntercepts(this, ".ctor", null);
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, null, ex);
                if (result.HasResult)
                {
                    return;
                }

                throw ex;
            }
        }

        public override async Task AsyncT2()
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                await base.AsyncT2();
                return;
            }

            Object[] parameters = null;
            InterceptResult result;

            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    await (Task)result.Result;
                    return;
                }

                // 调用基类方法
                await base.AsyncT2();

                result = wrapper.CalledIntercepts(this, ".ctor", null);
                if (result.HasResult)
                {
                    await (Task)result.Result;
                    return;
                }
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, null, ex);
                if (result.HasResult)
                {
                    await (Task)result.Result;

                }

                throw ex;
            }
        }

        public override async Task<int> AsyncT3()
        {
            await Task.Delay(1000);
            return await base.AsyncT3();
        }

        public override async ValueTask<int> AsyncT4()
        {
            await Task.Delay(1000);
            return await base.AsyncT4();
        }

        public override async Task<Exception> AsyncT5()
        {
            await Task.Delay(1000);
            return await base.AsyncT5();
        }

        public override async ValueTask<Exception> AsyncT6(int x, int y)
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                return await base.AsyncT6(x, y);
            }

            Object[] parameters = new object[2];
            parameters[0] = x;
            parameters[1] = y;
            InterceptResult result;
            Exception returnValue = null;
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return await (ValueTask<Exception>)result.Result;
                }

                // 调用基类方法
                returnValue = await base.AsyncT6(x, y);

                result = wrapper.CalledIntercepts(this, ".ctor", returnValue);

                if (result.HasResult)
                {
                    return await (ValueTask<Exception>)result.Result;
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, returnValue, ex);
                if (result.HasResult)
                {
                    return await (ValueTask<Exception>)result.Result;
                }

                throw ex;
            }
        }

        public override async Task<Exception> AsyncT7(string x, string y, Exception ex1, Exception ex2, params object[] objs)
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                return await base.AsyncT7(x, y, ex1, ex2, objs);
            }

            Object[] parameters = new object[4];
            parameters[0] = x;
            parameters[1] = y;
            parameters[2] = ex1;
            parameters[3] = ex2;
            parameters[4] = objs;
            InterceptResult result;
            Exception returnValue = null;
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return await (Task<Exception>)result.Result;
                }

                // 调用基类方法
                returnValue = await base.AsyncT7(x, y, ex1, ex2, objs);

                result = wrapper.CalledIntercepts(this, ".ctor", returnValue);

                if (result.HasResult)
                {
                    return await (Task<Exception>)result.Result;
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, returnValue, ex);
                if (result.HasResult)
                {
                    return await (Task<Exception>)result.Result;
                }

                throw ex;
            }
        }

        public override async Task<string> AsyncT8(InterceptResult resultArg)
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                return await base.AsyncT8(resultArg);
            }

            Object[] parameters = new object[1];
            parameters[0] = resultArg;
            InterceptResult result;
            string returnValue = null;
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return await (Task<string>)result.Result;
                }

                // 调用基类方法
                returnValue = await base.AsyncT8(resultArg);

                result = wrapper.CalledIntercepts(this, ".ctor", returnValue);

                if (result.HasResult)
                {
                    return await (Task<string>)result.Result;
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, returnValue, ex);
                if (result.HasResult)
                {
                    return await (Task<string>)result.Result;
                }

                throw ex;
            }
        }

        public override async ValueTask<long> AsyncT9(long l)
        {
            var wrapper = _wrappers.GetWrapper(10000);
            if (wrapper == null)
            {
                // 调用基类方法
                return await base.AsyncT9(l);
            }

            Object[] parameters = new object[1];
            InterceptResult result;
            long returnValue = default(long);
            try
            {
                result = wrapper.CallingIntercepts(this, ".ctor", parameters);
                if (result.HasResult)
                {
                    return await (ValueTask<long>)result.Result;
                }

                // 调用基类方法
                returnValue = await base.AsyncT9(l);

                result = wrapper.CalledIntercepts(this, ".ctor", returnValue);

                if (result.HasResult)
                {
                    return await (ValueTask<long>)result.Result;
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                result = wrapper.ExceptionIntercept(this, ".ctor", parameters, returnValue, ex);
                if (result.HasResult)
                {
                    return await (ValueTask<long>)result.Result;
                }

                throw ex;
            }
        }

        //public override async Task<string> AsyncT10(int x, int y)
        //{
        //    Func<int, int, Task<string>> fun = async (int a, int b) => await base.AsyncT10(a, b);
        //    return await fun(x, y);
        //}
    }

    public class GenericTs<T1, T2>
        where T1 : List<T2>
        where T2 : class, IList
    {
        public GenericTs(T1 t1, T2 t2)
        {
            t1 = null;
            t2 = null;
        }
    }

    public class GenericTss<T1, T2> : GenericTs<T1, T2>
        where T1 : List<T2>
        where T2 : class, IList
    {
        public GenericTss(T1 t1, T2 t2) : base(t1, t2)
        {
        }
    }


    public class BarBase
    {
        public virtual async Task Async1(int x, Exception ex, string s, long l)
        {
            await Task.CompletedTask;
        }

        public virtual async Task<Exception> Async2(int x, Exception ex)
        {
            await Task.CompletedTask;
            return new Exception(x.ToString(), ex);
        }

        public virtual async ValueTask<Exception> Async3(int x, Exception ex)
        {
            await Task.CompletedTask;
            return new Exception(x.ToString(), ex);
        }
    }


    public class Bar : BarBase
    {
        private InterceptorWrapperCollection _wrappers;
        private Func<object> async1;
        private Caller caller1;

        private Func<object> async2;
        private Caller caller2;

        private Func<object> async3;
        private Caller caller3;

        private void T(ref int x)
        {

        }

        public override Task Async1(int x, Exception ex, string s, long l)
        {
            if (caller1 == null)
            {
                caller1 = new Caller(_wrappers.GetWrapper(1000), MethodType.Type);
            }

            if (async1 == null)
            {
                async1 = () => base.Async1(x, ex, s, l);
            }

            object[] p = new object[4];
            p[0] = x;
            p[1] = ex;
            p[2] = s;
            p[3] = l;

            caller1.Call<object>(this, String.Empty, async1, p);
            _wrappers
        }

        public override Task<Exception> Async2(int x, Exception ex)
        {
            if (caller2 == null)
            {
                caller2 = new Caller(_wrappers.GetWrapper(1000));
            }

            if (async2 == null)
            {
                async2 = () => base.Async2(x, ex);
            }

            return caller2.CallTaskOfT(this, String.Empty, async2, x, ex);
        }

        public override ValueTask<Exception> Async3(int x, Exception ex)
        {
            if (caller3 == null)
            {
                caller3 = new Caller(_wrappers.GetWrapper(1000));
            }

            if (async3 == null)
            {
                async3 = () => base.Async3(x, ex);
            }

            return caller3.CallValueTaskOfT(this, String.Empty, async3, x, ex);
        }
    }
}
