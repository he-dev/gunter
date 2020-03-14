namespace Gunter.Data.Configuration.Abstractions {
    public interface IMessage : IModel
    {
        string ReportName { get; }
    }
}