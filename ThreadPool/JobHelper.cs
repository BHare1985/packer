using SimpleLogger;
using System;

namespace ThreadPool
{
    public static class JobHelper
    {
        public static Job Then(this Job current, QueueType priority, Action payload)
        {
            var job = new Job(payload);
            job.SetPriority(priority);
            current.AddNext(job);
            Logger.Log(Level.Verbose, $"job {job.Id} was connected next to {current.Id}");
            return job;
        }

        public static Job Then<TInput>(this Job<TInput> current, QueueType priority, Action<Job<TInput>> payload)
        {
            var job = new Job(payload);
            job.SetPriority(priority);
            current.AddNext(job);
            Logger.Log(Level.Verbose, $"job {job.Id} was connected next to {current.Id}");
            return job;
        }

        public static Job<TResult> Then<TInput, TResult>(this Job<TInput> current, QueueType priority, Func<Job<TInput>, TResult> payload)
        {
            var job = new Job<TResult>(payload);
            job.SetPriority(priority);
            current.AddNext(job);
            Logger.Log(Level.Verbose, $"job {job.Id} was connected next to {current.Id}");
            return job;
        }
    }

}
