using System.Data;

namespace Gunter.Services.Abstractions
{
    public interface IDataFilter
    {
        void Execute(DataTable data);
    }
}