using System;

namespace ThreadPool
{
    public class Job<T> : Job, IJob<T>
    {
        public T Result { get; private set; }

        internal Job(Delegate payload) : base(payload) { }

        internal override object Run()
        {
            Result = (T)base.Run();
            foreach(var next in Next)
                next.SetPreviousResult(this);
            return Result;
        }

        protected override void Disposing()
        {
            Result = default(T);
        }
    }
}
