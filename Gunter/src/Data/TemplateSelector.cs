using System.Text.RegularExpressions;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Utilities.JsonNet.Annotations;

namespace Gunter.Data
{
    [JsonString]
    [UsedImplicitly]
    public class TemplateSelector
    {
        private TemplateSelector(string templateName, string modelId)
        {
            TemplateName = templateName;
            ModelName = modelId;
        }

        public SoftString TemplateName { get; }

        public SoftString ModelName { get; }

        public static TemplateSelector Parse(string merge)
        {
            // other-file-name#other-id

            //var joinTypes = Enum.GetNames(typeof(JoinType)).Join("|");
            //var mergeMatch = Regex.Match(merge, $"(?<otherFileName>[_a-z]+)\\/(?<otherId>\\d+)\\?(?<type>{joinTypes})", RegexOptions.IgnoreCase);
            var mergeMatch = Regex.Match(merge, $"(?<fileName>[_a-z0-9-]+)(#(?<id>[_a-z0-9-]+))?", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Create($"InvalidMergeExpression", $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'file-name[#module-id]'.");
            }

            return new TemplateSelector
            (
                mergeMatch.Group("fileName"),
                mergeMatch.Group("id")
            );
        }

        public override string ToString() => $"{TemplateName}#{ModelName}";
    }
}