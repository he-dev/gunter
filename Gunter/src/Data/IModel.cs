using System.Collections.Generic;
using Gunter.Annotations;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    [Gunter]
    public interface IModel
    {
        SoftString? Name { get; set; }
    }

    public interface IMergeable
    {
        [JsonProperty("Merge")]
        TemplateSelector TemplateSelector { get; set; }
    }
}