using System.Data;

namespace Gunter.Data.Attachements.Abstractions
{
    public interface IAttachment
    {
        string Name { get; set; }

        object Compute(DataRow source);
    }
}