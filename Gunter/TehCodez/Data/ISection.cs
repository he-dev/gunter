using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Data
{
    public interface ISection
    {
        string Heading { get; }

        string Text { get; }
    }

    public enum Orientation
    {
        Horizontal,
        Vertical
    }
}
