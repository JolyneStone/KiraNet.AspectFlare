using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using KiraNet.AspectFlare.DynamicProxy;

namespace KiraNet.AspectFlare.Test
{
    [Calling]
    public class T
    {
        //[Calling]
        public T()
        {

        }

        public T(int x, Exception ex1, Exception ex2) { }
        public T(ref int x, out Exception ex1, Exception ex2) { ex1 = null; }

        [Calling]

        public virtual void T0()
        {
        }

        public virtual ref int Tr0(ref int x)
        {
            ref int j = ref x;
            return ref j;
        }

        public virtual ref Exception Tr0(ref Exception x)
        {
            ref Exception j = ref x;
            return ref j;
        }

        [Calling]
        public virtual int T1(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2, InterceptResult xx, Exception yy)
        {
            y = default(InterceptResult);
            ex2 = new Exception();
            return 1;
        }

        public virtual int T2(InterceptResult x, InterceptResult y, Exception ex1, Exception ex2)
        {
            return 1;
        }

        [Calling]
        public virtual async Task Test0()
        {
            await Task.Delay(1000);
        }

        [Calling]
        public virtual async Task<int> Test1(int x, int y, Exception x1, Exception y1)
        {
            await Task.Delay(1000);
            return 1;
        }

        [Calling]
        public virtual async ValueTask<int> Test2()
        {
            await Task.Delay(1000);
            return 1;
        }
    }



    public class TFuck : T
    {
        private InterceptorWrapperCollection _wrappers;
        private VoidCaller _callerInit;
        private VoidCaller _caller0;
        private ReturnCaller<int> _caller1;
        private ReturnCaller<int> _caller2;
        private TaskCaller _caller3;
        private TaskCaller<int> _caller4;
        private ValueTaskCaller<int> _caller5;
        private ReturnCaller<int> _callerz;

        public TFuck(int x, Exception ex1, Exception ex2) : base(x, ex1, ex2) { }
        public TFuck(ref int x, out Exception ex1, Exception ex2, IList<RuntimeMethodHandle> methodHandles) : base(ref x, out ex1, ex2)
        {
            Init();
        }

        public TFuck() : base()
        {
            if (_callerInit == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _callerInit = new VoidCaller(wrapper);
            }

            //Action call = () => base();
            //_callerInit.Call(this, call, null);
            return;
        }

        private void Init()
        {
            _wrappers = new InterceptorWrapperCollection(typeof(T), typeof(TFuck));
        }

        public override void T0()
        {
            if (_caller0 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller0 = new VoidCaller(wrapper);
            }

            Action call = () => base.T0();
            _caller0.Call(this, call, null);
            return;
        }

        public override int T1(ref InterceptResult x, out InterceptResult y, ref Exception ex1, out Exception ex2, InterceptResult xx, Exception yy)
        {
            if (_caller1 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller1 = new ReturnCaller<int>(wrapper);
            }

            object[] parameters = new object[6];
            InterceptResult x1 = x;
            parameters[0] = x;
            InterceptResult y1 = default(InterceptResult);
            parameters[1] = y1;
            Exception ex_1 = ex1;
            parameters[2] = ex1;
            Exception ex_2 = default(Exception);
            parameters[3] = ex_2;
            parameters[4] = xx;
            parameters[5] = yy;
            Func<int> call = () => { return base.T1(ref x1, out y1, ref ex_1, out ex_2, xx, yy); };
            int returnValue = _caller1.Call(this, call, parameters);
            y = y1;
            ex2 = ex_2;
            return returnValue;
        }

        public override int T2(InterceptResult x, InterceptResult y, Exception ex1, Exception ex2)
        {
            if (_caller2 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller2 = new ReturnCaller<int>(wrapper);
            }

            object[] parameters = new object[4];
            parameters[0] = x;
            parameters[1] = y;
            parameters[2] = ex1;
            parameters[3] = ex2;
            Func<int> call = () => base.T2(x, y, ex1, ex2);
            return _caller2.Call(this, call, parameters);
        }

        public override Task Test0()
        {
            if (_caller3 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller3 = new TaskCaller(wrapper);
            }

            Func<Task> call = () => base.Test0();
            return _caller3.Call(this, call, null);
        }

        public override Task<int> Test1(int x, int y, Exception x1, Exception y1)
        {
            if (_caller4 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller4 = new TaskCaller<int>(wrapper);
            }

            object[] parameters = new object[4];
            parameters[0] = x;
            parameters[1] = y;
            parameters[2] = x1;
            parameters[3] = y1;
            Func<Task<int>> call = () => base.Test1(x, y, x1, y1);
            return _caller4.Call(this, call, parameters);
        }

        public override ValueTask<int> Test2()
        {
            if (_caller5 == null)
            {
                InterceptorWrapper wrapper = _wrappers.GetWrapper(10000);
                _caller5 = new ValueTaskCaller<int>(wrapper);
            }

            Func<ValueTask<int>> call = () => base.Test2();
            return _caller5.Call(this, call, null);
        }

        //public override async Task Test1()
        //{
        //    var wrapper = _wrappers.GetWrapper(10000);
        //    if (wrapper == null)
        //    {
        //        // 调用基类方法
        //        await base.Test1();
        //        return;
        //    }

        //    Object[] parameters = null;
        //    InterceptResult result;

        //    result = wrapper.CallingIntercepts(this, parameters);
        //    if (result.HasResult)
        //    {
        //        return;
        //    }

        //    // 调用基类方法
        //    await base.Test1();
        //}

        //[Calling]
        //[Calling]
        //public override async Task Test2()
        //{
        //    var wrapper = _wrappers.GetWrapper(10000);
        //    if (wrapper == null)
        //    {
        //        // 调用基类方法
        //        await base.Test2();
        //        return;
        //    }

        //    Object[] parameters = null;
        //    InterceptResult result;
        //    try
        //    {
        //        result = wrapper.CallingIntercepts(this, parameters);
        //        if (result.HasResult)
        //        {
        //            return;
        //        }

        //        // 调用基类方法
        //        await base.Test2();
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}
    }
}
