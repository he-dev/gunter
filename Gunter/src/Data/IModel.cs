using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Reusable;

namespace Gunter.Data
{
    public interface IModel
    {
        [JsonRequired]
        SoftString Name { get; }
    }

    public interface IMergeable
    {
        [JsonProperty("Merge")]
        List<TemplateSelector>? TemplateSelectors { get; }
    }
}