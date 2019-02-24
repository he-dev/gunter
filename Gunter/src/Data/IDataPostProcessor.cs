using System.Data;

namespace Gunter.Data
{
    public interface IDataPostProcessor
    {
        void Execute(DataTable data);
    }
}