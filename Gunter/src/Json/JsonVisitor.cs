using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Gunter.Json
{
    public class JsonVisitor
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();

        public void Visit(JObject source)
        {
            VisitJObject(source);
        }

        public static IDictionary<string, object> GetProperties(JObject source)
        {
            var visitor = new JsonVisitor();
            visitor.Visit(source);
            return visitor._properties;
        }

        protected void VisitJObject(JObject source)
        {
            foreach (var item in source)
            {
                switch (item.Value)
                {
                    case JObject jObject:
                        VisitJObject(jObject);
                        break;
                    case JValue jValue:
                        VisitJValue(jValue);
                        break;
                    case JArray jArray:
                        VisitJArray(jArray);
                        break;
                }
            }
        }

        private void VisitJArray(JArray jArray)
        {
            // not used
        }

        private void VisitJValue(JValue jValue)
        {
            _properties[jValue.Path] = jValue.Value;
        }     
    }
}
