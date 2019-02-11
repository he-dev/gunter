using System.Collections.Generic;
using Reusable.IOnymous.Models;

namespace Gunter.Data.Dtos
{
    public class ModuleDto
    {
        public int Ordinal { get; set; }

        public string Heading { get; set; }

        public string Text { get; set; }

        public HtmlTable Data { get; set; }
    }
}