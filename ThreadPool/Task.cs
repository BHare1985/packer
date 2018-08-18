using System.Threading;

namespace ThreadPool
{
    internal class Task
    {
        public Task(Thread thread)
        {
            Thread = thread;
        }
        public State State { get; set; }
        public Thread Thread { get; }
    }
}
