using System;
using System.Threading;

namespace NewLife.Threading
{
    /// <summary>线程任务</summary>
    class ThreadTask
    {
        /// <summary>唯一编号</summary>
        public Int32 ID { get; private set; }

        /// <summary>任务方法</summary>
        public WaitCallback Method { get; set; }

        /// <summary>任务参数</summary>
        public Object Argument { get; set; }

        /// <summary>取消任务时执行的方法</summary>
        public WaitCallback AbortMethod { get; set; }

        private static Object newID_Lock = new object();
        private static Int32 _newID;
        /// <summary>取一个新编号</summary>
        private static Int32 newID
        {
            get
            {
                lock (newID_Lock)
                {
                    _newID++;
                    return _newID;
                }
            }
        }

        /// <summary>构造一个线程任务</summary>
        /// <param name="method">任务方法</param>
        /// <param name="argument">任务参数</param>
        public ThreadTask(WaitCallback method, Object argument)
        {
            Method = method;
            Argument = argument;
            ID = newID;
        }

        /// <summary>构造一个线程任务</summary>
        /// <param name="method">任务方法</param>
        /// <param name="abortMethod">任务被取消时执行的方法</param>
        /// <param name="argument">任务参数</param>
        public ThreadTask(WaitCallback method, WaitCallback abortMethod, Object argument)
        {
            Method = method;
            Argument = argument;
            ID = newID;
            AbortMethod = abortMethod;
        }
    }
}
