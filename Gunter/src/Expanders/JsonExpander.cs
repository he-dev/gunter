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
        private readonly IDictionary<string, object> _properties;

        private JsonExpander(JObject source)
        {
            _properties = new Dictionary<string, object>();
            VisitJObject(source);
        }

        public static IDictionary<string, object> Expand(string json)
        {
            var jObject = JObject.Parse(json);
            return new JsonExpander(jObject)._properties;
        }

        protected override void VisitJValue(JValue jValue)
        {
            _properties[jValue.Path] = jValue.Value;
        }
    }
}