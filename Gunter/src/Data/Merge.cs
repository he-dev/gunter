using System;
using System.Linq.Custom;
using System.Text.RegularExpressions;
using Gunter.Services;
using Reusable;
using Reusable.Extensions;
using Reusable.Reflection;

namespace Gunter.Data
{
    public class Merge
    {
        public Merge(string otherFileName, SoftString otherId)
        {
            OtherFileName = otherFileName;
            OtherId = otherId;
        }

        public SoftString OtherFileName { get; }

        public SoftString OtherId { get; }

        public static Merge Parse(string merge)
        {
            // _Global/301?base

            //var joinTypes = Enum.GetNames(typeof(JoinType)).Join("|");
            //var mergeMatch = Regex.Match(merge, $"(?<otherFileName>[_a-z]+)\\/(?<otherId>\\d+)\\?(?<type>{joinTypes})", RegexOptions.IgnoreCase);
            var mergeMatch = Regex.Match(merge, $"(?<otherFileName>[_a-z0-9-]+)\\/(?<otherId>[_a-z0-9-]+)", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Create($"InvalidMergeExpression", $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'Name/Id'.");
            }

            return new Merge
            (
                mergeMatch.Groups["otherFileName"].Value,
                mergeMatch.Groups["otherId"].Value
                //(JoinType)Enum.Parse(typeof(JoinType), mergeMatch.Groups["type"].Value, ignoreCase: true)
            );
        }

        public override string ToString() => $"{OtherFileName}/{OtherId}";
    }
}