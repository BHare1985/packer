using System;

namespace ThreadPool
{
    public class WorkT<T> : Work, IWork<T>
    {
        internal WorkT(Delegate payload) : base(payload)
        {

        }

        private T result;
        public T Result => this.result;

        internal override void Run()
        {
            result = (T)payload.DynamicInvoke();
        }
    }
}
