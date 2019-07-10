using Reusable.Utilities.JsonNet.Annotations;

namespace Gunter.Annotations
{
    public class GunterAttribute : NamespaceAttribute
    {
        public GunterAttribute() : base(ProgramInfo.Name) { }
    }
}