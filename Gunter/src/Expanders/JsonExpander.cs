using System.Collections.Generic;
using Gunter.Json;
using Newtonsoft.Json.Linq;

namespace Gunter.Expanders
{
    public interface IColumnExpander
    {
        IDictionary<string, object> Expand(object data);
    }

    public class JsonExpander : JsonVisitor
    {
        private readonly IDictionary<string, object> _flat;

        private JsonExpander(JObject source)
        {
            _flat = new Dictionary<string, object>();
            VisitJObject(source);
        }

        public static IDictionary<string, object> Expand(string json)
        {
            var jObject = JObject.Parse(json);
            return new JsonExpander(jObject)._flat;
        }

        protected override void VisitJValue(JValue jValue)
        {
            _flat[jValue.Path] = jValue.Value;
        }
    }
}