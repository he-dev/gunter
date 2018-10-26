using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Custom;
using System.Linq.Expressions;
using System.Reflection;
using Autofac;
using Autofac.Features.Indexed;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Extensions;
using Reusable;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;

namespace Gunter.Services
{
    internal class TestComposer
    {
        private readonly ILogger _logger;
        private readonly IComponentContext _componentContext;

        public TestComposer(ILogger<TestComposer> logger, IComponentContext componentContext)
        {
            _logger = logger;
            _componentContext = componentContext;
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

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private bool TryCompose(TestBundle testBundle, IEnumerable<TestBundle> partials, out TestBundle composition)
        {
            var scope = _logger.BeginScope().AttachElapsed();
            _logger.Log(Abstraction.Layer.Infrastructure().Argument(new { testBundle.FileName }));

            try
            {
                composition = _componentContext.Resolve<TestBundle>();
                composition.FullName = testBundle.FullName;
                composition.Variables = Merge(testBundle, partials, bundle => bundle.Variables).ToList();
                composition.DataSources = Merge(testBundle, partials, bundle => bundle.DataSources).ToList();
                composition.Tests = Merge(testBundle, partials, bundle => bundle.Tests).ToList();
                composition.Messages = Merge(testBundle, partials, bundle => bundle.Messages).ToList();
                composition.Reports = Merge(testBundle, partials, bundle => bundle.Reports).ToList();

                _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Completed());

                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Faulted(), ex);
                composition = default;
                return false;
            }
            finally
            {
                scope.Dispose();
            }
        }

        private IEnumerable<T> Merge<T>(TestBundle testBundle, IEnumerable<TestBundle> partials, Func<TestBundle, IEnumerable<T>> selectMergables) where T : class, IMergable
        {
            var mergables = selectMergables(testBundle);
            foreach (var mergable in mergables.Where(x => x.Merge.IsNotNull()))
            {
                var merge = mergable.Merge;
                var otherTestBundle = partials.SingleOrDefault(p => p.Name == merge.OtherFileName) ?? throw DynamicException.Create("OtherTestBundleNotFound", $"Could not find test bundle '{merge.OtherFileName}'.");
                var otherMergables = selectMergables(otherTestBundle).SingleOrDefault(x => x.Id == merge.OtherId) ?? throw DynamicException.Create("OtherMergableNotFound", $"Could not find mergable '{merge.OtherId}'.");

                var (first, second) = (mergable, otherMergables);

                var merged = (IMergable)_componentContext.Resolve(mergable.GetType());
                merged.Id = mergable.Id;
                merged.Merge = mergable.Merge;

                var mergableProperties =
                    merged
                        .GetType()
                        .GetProperties()
                        .Where(p => p.IsDefined(typeof(MergableAttribute)))
                        .ToList();

                foreach (var property in mergableProperties)
                {
                    var firstValue = property.GetValue(first);
                    var newValue = property.GetValue(second);
                    switch (firstValue)
                    {
                        case ISet<int> set when newValue is ISet<int> other:
                            set.UnionWith(other);
                            break;

                        case IDictionary<SoftString, object>  dict when newValue is IDictionary<SoftString, object> other:
                            dict.UnionWith(other);
                            break;
                    }

                    property.SetValue(merged, newValue);
                }

                yield return (T)merged;
            }
        }

        // private Dictionary<SoftString, object> Merge(Dictionary<SoftString, object> variables, IEnumerable<TestBundle> partialBundles)
        // {
        //     if (variables.TryGetValue("Merge", out var x) && x is string merge)
        //     {
        //         _logger.Log(Abstraction.Layer.Infrastructure().Variable(new { merge }));
        //
        //         var merges = merge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(n => n.FormatPartialName().ToSoftString()).ToList();
        //         var otherVariables = partialBundles.Where(p => p.Name.In(merges)).SelectMany(p => p.Variables);
        //
        //         return
        //             variables
        //                 .Concat(otherVariables)
        //                 .GroupBy(v => v.Key)
        //                 .Select(g => g.Last())
        //                 .ToDictionary(g => g.Key, g => g.Value);
        //     }
        //     else
        //     {
        //         return variables;
        //     }
        // }

    #region Helpers        

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

    internal static class DictionaryExtensions
    {
        public static void UnionWith<TKey, TValue>(this IDictionary<TKey, TValue> target, IEnumerable<KeyValuePair<TKey, TValue>> other)
        {
            foreach (var pair in other)
            {
                if (!target.ContainsKey(pair.Key))
                {
                    target.Add(pair);
                }
            }
        }
    }
}