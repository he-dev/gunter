using Newtonsoft.Json.Linq;

namespace Gunter.Json
{
    public abstract class JsonVisitor
    {
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

        protected virtual void VisitJArray(JArray jArray)
        {
            // not used
        }

        protected virtual void VisitJValue(JValue jValue)
        {
            // not used
        }
    }
}
