namespace ThreadPool
{
    public interface IJob<T>
    {
        T Result { get; }
    }
}
