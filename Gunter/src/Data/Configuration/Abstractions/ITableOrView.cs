namespace Gunter.Data.Configuration.Abstractions {
    public interface ITableOrView : IQuery, IMergeable
    {
        string ConnectionString { get; }

        string Command { get; }

        int Timeout { get; }
    }
}