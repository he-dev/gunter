using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Gunter.Data.Configuration;
using Gunter.Services;
using Gunter.Services.Validators;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable.Logging;

namespace Gunter.Data
{
    internal class GlobalFile
    {
        [NotNull]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Globals { get; set; } = new Dictionary<string, object>();
    }
}