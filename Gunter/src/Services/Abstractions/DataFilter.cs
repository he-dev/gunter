using System.Data;

namespace Gunter.Services.Abstractions
{
    public interface IFilterData
    {
        void Execute(DataTable data);
    }
}