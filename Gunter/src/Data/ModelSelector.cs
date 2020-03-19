using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Utilities.JsonNet.Annotations;

namespace Gunter.Data
{
    [JsonString]
    [UsedImplicitly]
    public class ModelSelector
    {
        public ModelSelector(string templateName, string modelName)
        {
            TemplateName = templateName;
            ModelName = modelName;
        }

        public string TemplateName { get; }

        public string ModelName { get; }

        public static ModelSelector Parse(string merge)
        {
            // file-name#model-name

            //var joinTypes = Enum.GetNames(typeof(JoinType)).Join("|");
            //var mergeMatch = Regex.Match(merge, $"(?<otherFileName>[_a-z]+)\\/(?<otherId>\\d+)\\?(?<type>{joinTypes})", RegexOptions.IgnoreCase);
            var mergeMatch = Regex.Match(merge, $"(?<fileName>[_a-z0-9-]+)(#(?<modelName>[_a-z0-9-]+))?", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Create($"InvalidMergeExpression", $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'file-name[#model-name]'.");
            }

            return new ModelSelector
            (
                mergeMatch.Group("fileName"),
                mergeMatch.Group("modelName")
            );
        }

        public override string ToString() => $"{TemplateName}#{ModelName}";

        public static readonly IEqualityComparer<ModelSelector> Comparer = EqualityComparerFactory<ModelSelector>.Create
        (
            getHashCode: (obj) => 0,
            equals: (left, right) =>
            {
                return
                    SoftString.Comparer.Equals(left.TemplateName, right.TemplateName) &&
                    SoftString.Comparer.Equals(left.ModelName, right.ModelName);
            }
        );
    }
}