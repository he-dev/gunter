using System.Collections.Generic;
using System.Linq;
using Gunter.Data.Configuration;

namespace Gunter.Workflow.Data
{
    internal class TheoryContext
    {
        public TheoryContext(Theory theory, IEnumerable<Theory> templates)
        {
            Theory = theory;
            Templates = templates.ToList();
        }

        public Theory Theory { get; }

        public List<Theory> Templates { get; }
    }
}