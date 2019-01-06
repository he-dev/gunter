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
using Reusable.Collections;
using Reusable.Exceptionizer;
using Reusable.Extensions;
using Reusable.Flawless;
using Reusable.OmniLog;
using Reusable.OmniLog.SemanticExtensions;
using Reusable.Reflection;

namespace Gunter.Services
{
    internal interface ITestComposer
    {
        IEnumerable<TestBundle> ComposeTests(IEnumerable<TestBundle> tests);
    }

    internal class TestComposer : ITestComposer
    {
        private static readonly IExpressValidator<TestBundle> TestBundleBouncer = ExpressValidator.For<TestBundle>(builder =>
        {
            var comparer = EqualityComparerFactory<IMergeable>.Create((x, y) => x.Id == y.Id, obj => obj.Id.GetHashCode());
            builder.True(x => ContainsUniqueIds(x.Variables, comparer));
            builder.True(x => ContainsUniqueIds(x.DataSources, comparer));
            builder.True(x => ContainsUniqueIds(x.Tests, comparer));
            builder.True(x => ContainsUniqueIds(x.Messages, comparer));
            builder.True(x => ContainsUniqueIds(x.Reports, comparer));
        });

        private readonly ILogger _logger;
        private readonly IComponentContext _componentContext;

        public TestComposer(ILogger<TestComposer> logger, IComponentContext componentContext)
        {
            _logger = logger;
            _componentContext = componentContext;
        }

        public IEnumerable<TestBundle> ComposeTests(IEnumerable<TestBundle> tests)
        {
            var partials = tests.ToLookup(TestBundleExtensions.IsPartial);

            foreach (var testBundle in partials[false])
            {
                if (TryCompose(testBundle, partials[true], out var composition))
                {
                    yield return composition;
                }
            }
        }

        private static bool ContainsUniqueIds(IEnumerable<IMergeable> mergeables, IEqualityComparer<IMergeable> comparer)
        {
            return mergeables.GroupBy(y => y, comparer).All(g => g.Count() == 1);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private bool TryCompose(TestBundle testBundle, IEnumerable<TestBundle> partials, out TestBundle composition)
        {
            var scope = _logger.BeginScope().AttachElapsed();
            try
            {
                composition = _componentContext.Resolve<TestBundle>();
                composition.FullName = testBundle.FullName;
                composition.Variables = Merge(testBundle, partials, bundle => bundle.Variables).ToList();
                composition.DataSources = Merge(testBundle, partials, bundle => bundle.DataSources).ToList();
                composition.Tests = Merge(testBundle, partials, bundle => bundle.Tests).ToList();
                composition.Messages = Merge(testBundle, partials, bundle => bundle.Messages).ToList();
                composition.Reports = Merge(testBundle, partials, bundle => bundle.Reports).ToList();

                composition.ValidateWith(TestBundleBouncer).Assert();

                _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Completed(), testBundle.FileName.ToString());

                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Faulted(), testBundle.FileName.ToString(), ex);
                composition = default;
                return false;
            }
            finally
            {
                scope.Dispose();
            }
        }

        private IEnumerable<T> Merge<T>(TestBundle testBundle, IEnumerable<TestBundle> partials, Func<TestBundle, IEnumerable<T>> selectMergeables) where T : class, IMergeable
        {
            var mergeables = selectMergeables(testBundle);
            foreach (var mergeable in mergeables)
            {
                if (mergeable.Merge is null)
                {
                    yield return mergeable;
                    continue;
                }

                var merge = mergeable.Merge;
                var otherTestBundle = partials.SingleOrDefault(p => p.Name == merge.OtherFileName) ?? throw DynamicException.Create("OtherTestBundleNotFound", $"Could not find test bundle '{merge.OtherFileName}'.");
                var otherMergeables = selectMergeables(otherTestBundle).SingleOrDefault(x => x.Id == merge.OtherId) ?? throw DynamicException.Create("OtherMergeableNotFound", $"Could not find mergeable '{merge.OtherId}'.");

                var (first, second) = (mergeable, otherMergeables);

                var merged = (IMergeable)_componentContext.Resolve(mergeable.GetType());
                merged.Id = mergeable.Id;
                merged.Merge = mergeable.Merge;

                var mergeableProperties = merged.GetType().GetProperties().Where(p => p.IsDefined(typeof(MergableAttribute)));

                foreach (var property in mergeableProperties)
                {
                    var firstValue = property.GetValue(first);
                    var newValue = property.GetValue(second);
                    switch (firstValue)
                    {
                        case IEnumerable<SoftString> x when newValue is IEnumerable<SoftString> y:
                            newValue = x.Union(y).ToList();
                            break;

                        case IEnumerable<KeyValuePair<SoftString, object>> x when newValue is IEnumerable<KeyValuePair<SoftString, object>> y:
                            newValue = x.Union(y).ToDictionary(p => p.Key, p => p.Value);
                            break;
                    }

                    if (property.GetCustomAttribute<MergableAttribute>().Required && newValue is null)
                    {
                        throw DynamicException.Create(
                            "MissingValue",
                            $"You need to specify a value for '{property.Name}' in '{testBundle.FileName}'. Either directly or via a merge."
                        );
                    }

                    property.SetValue(merged, newValue);
                }

                yield return (T)merged;
            }
        }
    }
}