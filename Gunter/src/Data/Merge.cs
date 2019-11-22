using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.Utilities.JsonNet.Annotations;

namespace Gunter.Data
{
    [JsonString]
    [UsedImplicitly]
    public class Merge
    {
        private Merge(string name, SoftString id)
        {
            Name = name;
            Id = id;
        }

        [NotNull]
        public SoftString Name { get; }

        [CanBeNull]
        public SoftString Id { get; }

        public static Merge Parse(string merge)
        {
            // other-file-name#other-id

            //var joinTypes = Enum.GetNames(typeof(JoinType)).Join("|");
            //var mergeMatch = Regex.Match(merge, $"(?<otherFileName>[_a-z]+)\\/(?<otherId>\\d+)\\?(?<type>{joinTypes})", RegexOptions.IgnoreCase);
            var mergeMatch = Regex.Match(merge, $"(?<fileName>[_a-z0-9-]+)(#(?<id>[_a-z0-9-]+))?", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Create($"InvalidMergeExpression", $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'file-name[#module-id]'.");
            }

            return new Merge
            (
                mergeMatch.Group("fileName"),
                mergeMatch.Group("id")
            );
        }

        public override string ToString() => $"{Name}#{Id}";
    }
}