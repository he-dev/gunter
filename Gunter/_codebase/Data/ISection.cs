using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Data
{
    public interface ISection : IDisposable
    {
        string Heading { get; }

        DataTable Data { get; }

        Orientation Orientation { get; }
    }

    public class Section : ISection
    {
        public string Heading { get; set; }

        public DataTable Data { get; set; }

        public Orientation Orientation { get; set; }

        public void Dispose() => Data?.Dispose();
    }

    public enum Orientation
    {
        Horizontal,
        Vertical
    }
}
