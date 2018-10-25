using System;
using System.Text.RegularExpressions;
using Gunter.Services;
using Reusable;
using Reusable.Extensions;
using Reusable.Reflection;

namespace Gunter.Data
{
    public enum MergeMode
    {
        Base,
        Join
    }

    public class Merge
    {
        public Merge(string otherFileName, int otherId, MergeMode mode)
        {
            OtherFileName = otherFileName;
            OtherId = otherId;
            Mode = mode;
        }

        public SoftString OtherFileName { get; }

        public int OtherId { get; }

        public MergeMode Mode { get; }

        public static Merge Parse(string merge)
        {
            // _Global/301?base

            var mergeMatch = Regex.Match(merge, @"(?<otherFileName>[_a-z]+)\/(?<otherId>\d+)\?(?<mode>base|join)", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Create($"InvalidMergeExpression", $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'Name/Id?Mode'.");
            }

            return new Merge
            (
                mergeMatch.Groups["otherFileName"].Value,
                int.Parse(mergeMatch.Groups["otherId"].Value),
                (MergeMode)Enum.Parse(typeof(MergeMode), mergeMatch.Groups["mode"].Value, ignoreCase: true)
            );
        }

        public override string ToString() => $"{OtherFileName}/{OtherId}?{Mode.ToString()}";
    }
}