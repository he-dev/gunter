using Gunter.Testing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gunter.Data
{    
    public class TestContext
    {
        public IDataSource DataSource { get; set; }
        public DataTable Data { get; set; }
        public TestProperties Test { get; set; }
    }
}
