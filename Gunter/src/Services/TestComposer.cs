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
            try
            {
                composition = _createTestBundle(testBundle);                

                var merge = composition.Variables.TryGetValue("Merge", out var x) && x is string m ? m : default;
                if (merge.IsNotNullOrEmpty())
                {
                    var merges = merge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.FormatPartialName().ToSoftString()).ToList();
                    var variables = partialBundles.Where(p => p.Name.In(merges)).SelectMany(p => p.Variables).ToList();
                    composition.Variables = MergeVariables(variables.Concat(testBundle.Variables));
                }

                composition.DataSources = Merge(testBundle, tb => tb.DataSources, partialBundles).ToList();
                composition.Tests = Merge(testBundle, tb => tb.Tests, partialBundles).ToList();
                composition.Messages = Merge(testBundle, tb => tb.Messages, partialBundles).ToList();
                composition.Reports = Merge(testBundle, tb => tb.Reports, partialBundles).ToList();

                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Action().Failed(nameof(TryCompose)), ex);
                return false;
            }
        }

        private IEnumerable<T> Merge<T>(
            TestBundle testBundle,
            Func<TestBundle, IEnumerable<T>> getMergables,
            IEnumerable<TestBundle> partialBundles
        ) where T : class, IMergable
        {
            var mergables = getMergables(testBundle);

            foreach (var mergable in mergables)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Data().Object(new { mergable = new { mergable.Merge } }));

                if (mergable.Merge.IsNullOrEmpty())
                {
                    yield return mergable;
                    continue;
                }

                var (otherName, otherId, mode) = ParseMerge(mergable.Merge);

                _logger.Log(Abstraction.Layer.Infrastructure().Data().Variable(new { otherName, otherId, mode }));

                if (!TryFindPartialBundle(partialBundles, otherName, out var otherTestBundle))
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Action().Cancelled(nameof(Merge)), "Other test-bundle not found.");
                    continue;
                }

                if (!TryFindOtherMergable(getMergables(otherTestBundle), otherId, out var otherMergable))
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Action().Cancelled(nameof(Merge)), "Other mergable not found.");
                    continue;
                }

                var (first, second) = mode == MergeMode.Base ? (otherMergable, mergable) : (mergable, otherMergable);

                var result = mergable.New();

                var mergableProperties =
                    result
                        .GetType()
                        .GetProperties()
                        .Where(p => p.IsDefined(typeof(MergableAttribute)))
                        .ToList();

                foreach (var property in mergableProperties)
                {
                    var firstValue = property.GetValue(first);
                    var newValue = firstValue ?? property.GetValue(second);
                    property.SetValue(result, newValue);
                }

                yield return (T)result;
            }
        }

        #region Helpers

        private static Dictionary<SoftString, object> MergeVariables(IEnumerable<KeyValuePair<SoftString, object>> variables)
        {
            return
                variables
                    .GroupBy(x => x.Key)
                    .Select(g => g.Last())
                    .ToDictionary(x => x.Key, x => x.Value);
        }

        private static (SoftString Name, int Id, MergeMode Mode) ParseMerge(string merge)
        {
            var mergeMatch = Regex.Match(merge, @"(?<partial>[_a-z]+):(?<id>\d+):(?<mode>base|join)", RegexOptions.IgnoreCase);
            if (!mergeMatch.Success)
            {
                throw DynamicException.Factory.CreateDynamicException(
                    $"InvalidMergeExpression{nameof(Exception)}",
                    $"{merge.QuoteWith("'")} is not a valid merge expression. Expected: 'Name:Id:Mode'.",
                    null
                );
            }

            return
            (
                mergeMatch.Groups["partial"].Value.FormatPartialName().ToSoftString(),
                int.Parse(mergeMatch.Groups["id"].Value),
                (MergeMode)Enum.Parse(typeof(MergeMode), mergeMatch.Groups["mode"].Value, ignoreCase: true)
            );
        }

        private static bool TryFindPartialBundle(IEnumerable<TestBundle> partialBundles, SoftString otherName, out TestBundle partialBundle)
        {
            partialBundle = partialBundles.SingleOrDefault(p => p.Name == otherName);
            return !(partialBundle is null);
        }

        private static bool TryFindOtherMergable<T>(IEnumerable<T> mergables, int otherId, out T otherMergable) where T : class, IMergable
        {
            otherMergable = mergables.SingleOrDefault(x => x.Id == otherId);
            return !(otherMergable is null);
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