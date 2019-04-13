using System.Data;

namespace Gunter.Services
{
    public interface IDataPostProcessor
    {
        void Execute(DataTable data);
    }
}