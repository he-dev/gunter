using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Gunter.Data.Configuration
{
    internal class Global
    {
        [Required]
        public string Environment { get; set; }

        [Required]
        public string TestsDirectoryName { get; set; }
    }
}
