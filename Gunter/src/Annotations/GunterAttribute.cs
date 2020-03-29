using Reusable.Utilities.JsonNet.Annotations;

namespace Gunter.Annotations
{
    public class GunterAttribute : JsonTypeSchemeAttribute
    {
        public GunterAttribute() : base(ProgramInfo.Name)
        {
            Alias = "G";
        }
    }
}