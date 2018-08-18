namespace ThreadPool
{
    public interface IWork<T>
    {
        T Result { get; }
    }
}
