using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Reflection;
using System.Text.RegularExpressions;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Extensions;
using Gunter.Messaging;
using Gunter.Reporting;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter
{
    internal class TestComposer
    {
        private readonly ILogger _logger;

        private readonly TestBundle.Factory _createTestBundle;

        public TestComposer(ILogger<TestComposer> logger, TestBundle.Factory createTestBundle)
        {
            _logger = logger;
            _createTestBundle = createTestBundle;
        }

        public IEnumerable<TestBundle> ComposeTests(IEnumerable<TestBundle> tests)
        {
            var partials = tests.ToLookup(IsPartial);

            foreach (var testBundle in partials[false])
            {
                if (TryCompose(testBundle, partials[true], out var composition))
                {
                    yield return composition;
                }
            }
        }

        private bool TryCompose(TestBundle testBundle, IEnumerable<TestBundle> partialBundles, out TestBundle composition)
        {
            composition = default;
            using (_logger.BeginScope(nameof(TryCompose)).AttachElapsed())
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Argument(new { testBundle.FileName }));
                try
                {
                    composition = _createTestBundle(testBundle);
                    composition.FullName = testBundle.FullName;
                    composition.Variables = testBundle.Variables;                    

                    composition.Variables = Merge(testBundle.Variables, partialBundles);
                    composition.DataSources = Merge(testBundle, tb => tb.DataSources, partialBundles).ToList();
                    composition.Tests = Merge(testBundle, tb => tb.Tests, partialBundles).ToList();
                    composition.Messages = Merge(testBundle, tb => tb.Messages, partialBundles).ToList();
                    composition.Reports = Merge(testBundle, tb => tb.Reports, partialBundles).ToList();

                    _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Completed());

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Faulted(), ex);
                    return false;
                }
            }
        }

        private IEnumerable<T> Merge<T>(TestBundle testBundle, Func<TestBundle, IEnumerable<T>> getMergables, IEnumerable<TestBundle> partialBundles) where T : class, IMergable
        {
            var mergables = getMergables(testBundle);

            foreach (var mergable in mergables.Where(m => m.Merge.IsNotNullOrEmpty()))
            {
                var (otherName, otherId, mode) = ParseMerge(mergable.Merge);

                var otherTestBundle = partialBundles.SingleOrDefault(p => p.Name == otherName) ?? throw DynamicException.Factory.CreateDynamicException("OtherTestBundleNotFound", $"Could not find test bundle '{otherName}'.");
                var otherMergable = getMergables(otherTestBundle).SingleOrDefault(x => x.Id == otherId) ?? throw DynamicException.Factory.CreateDynamicException("OtherMergableNotFound", $"Could not find mergable '{otherId}'.");

                var (first, second) = mode == MergeMode.Base ? (otherMergable, mergable) : (mergable, otherMergable);

                var merged = mergable.New();

                var mergableProperties =
                    merged
                        .GetType()
                        .GetProperties()
                        .Where(p => p.IsDefined(typeof(MergableAttribute)))
                        .ToList();

                foreach (var property in mergableProperties)
                {
                    var firstValue = property.GetValue(first);
                    var newValue = firstValue ?? property.GetValue(second);
                    property.SetValue(merged, newValue);
                }

                yield return (T)merged;
            }
        }

        private Dictionary<SoftString, object> Merge(Dictionary<SoftString, object> variables, IEnumerable<TestBundle> partialBundles)
        {
            if (variables.TryGetValue("Merge", out var x) && x is string merge)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Variable(new { merge }));

                var merges = merge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.FormatPartialName().ToSoftString()).ToList();
                var otherVariables = partialBundles.Where(p => p.Name.In(merges)).SelectMany(p => p.Variables);

                return
                    variables
                        .Concat(otherVariables)
                        .GroupBy(v => v.Key)
                        .Select(g => g.Last())
                        .ToDictionary(g => g.Key, g => g.Value);
            }
            else
            {
                return variables;
            }
        }

        #region Helpers

        private (SoftString Name, int Id, MergeMode Mode) ParseMerge(string merge)
        {
            _logger.Log(Abstraction.Layer.Infrastructure().Argument(new { merge }));

            var mergeMatch = Regex.Match(merge, @"(?<partial>[_a-z]+):(?<id>\d+):(?<mode>base|join)", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Factory.CreateDynamicException(
                    $"InvalidMergeExpression",
                    $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'Name:Id:Mode'."
                );
            }

            return
            (
                mergeMatch.Groups["partial"].Value.FormatPartialName().ToSoftString(),
                int.Parse(mergeMatch.Groups["id"].Value),
                (MergeMode)Enum.Parse(typeof(MergeMode), mergeMatch.Groups["mode"].Value, ignoreCase: true)
            );
        }

        private static bool IsPartial(TestBundle testBundle)
        {
            Debug.Assert(testBundle.FileName.IsNotNullOrEmpty());

            return
                Path
                    .GetFileName(testBundle.FullName)
                    .StartsWith("_", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }

    public enum MergeMode
    {
        Base,
        Join
    }
}