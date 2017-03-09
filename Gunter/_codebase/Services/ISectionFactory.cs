using Gunter.Services;
using Gunter.Testing;
using System;
using System.Collections.Generic;
using System.Data;

namespace Gunter.Data.Sections
{
    public interface ISectionFactory
    {
        ISection Create(TestContext context, IConstantResolver constants);
    }
}
