using System.Data;

namespace Gunter.Data
{
    public interface IAttachment
    {
        string Name { get; set; }

        object Compute(DataRow source);
    }
}