using System.Collections.Generic;
using System.Linq;
using Gunter.Json;
using Newtonsoft.Json.Linq;

namespace Gunter.Expanders
{
    public interface IExpander
    {
        string Column { get; set; }

        int? Index { get; set; }

        IDictionary<string, object> Expand(object data);
    }

    public class JsonExpander : IExpander
    {
        public string Column { get; set; }

        public int? Index { get; set; }

        public IDictionary<string, object> Expand(object data)
        {
            if (data is null || !(data is string json))
            {
                return new Dictionary<string, object>();
            }

            var isArray = json.StartsWith("[") && json.EndsWith("]");
            if (isArray && Index.HasValue)
            {
                var jToken = JArray.Parse(json).ElementAtOrDefault(Index.Value);
                if (jToken is null)
                {
                    return new Dictionary<string, object>();
                }
                else
                {
                    var jObject = JObject.Parse(jToken.ToString());
                    return JsonVisitor.GetProperties(jObject);
                }
            }
            else
            {
                var jObject = JObject.Parse(json);
                return JsonVisitor.GetProperties(jObject);
            }
        }
    }
}