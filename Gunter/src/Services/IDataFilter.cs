using System.Data;

namespace Gunter.Services
{
    public interface IDataFilter
    {
        void Execute(DataTable data);
    }
}