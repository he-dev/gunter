using System;
using System.Data;

namespace Gunter.Data.Sections
{
    public class TableSection : ISection, IDisposable
    {
        public string Heading { get; set; }

        public string Text { get; set; }

        public DataTable Body { get; set; }

        public DataTable Footer { get; set; }

        public Orientation Orientation { get; set; }

        public void Dispose()
        {
            Body?.Dispose();
            Footer?.Dispose();
        }
    }
}
