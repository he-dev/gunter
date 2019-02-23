using System;
using System.Collections;
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
using JetBrains.Annotations;
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
        IEnumerable<TestBundle> ComposeTests
        (
            [NotNull, ItemNotNull] IEnumerable<TestBundle> bundles,
            [NotNull] ITestFilter testFilter
        );
    }

    internal class TestComposer : ITestComposer
    {
        private readonly ILogger _logger;
        private readonly IComponentContext _componentContext;

        public TestComposer(ILogger<TestComposer> logger, IComponentContext componentContext)
        {
            _logger = logger;
            _componentContext = componentContext;
        }

        public IEnumerable<TestBundle> ComposeTests(IEnumerable<TestBundle> bundles, ITestFilter testFilter)
        {
            var bundleGroups = bundles.ToLookup(b => b.Type);

            foreach (var bundle in bundleGroups[TestBundleType.Regular])
            {
                if (!testFilter.Files.IsNullOr(names => names.Select(SoftString.Create).Contains(bundle.Name)))
                {
                    continue;
                }

                var executableTests =
                    from test in bundle.Tests
                    where
                        test.Enabled &&
                        testFilter.Tests.IsNullOr(ids => ids.Select(SoftString.Create).Contains(test.Id)) &&
                        testFilter.Tags.IsNullOr(tags => tags.Select(SoftString.Create).Intersect(test.Tags).Any())
                    select test;

                bundle.Tests = executableTests.ToList();

                if (TryCompose(bundle, (IGrouping<TestBundleType, TestBundle>)bundleGroups[TestBundleType.Partial], out var composition))
                {
                    yield return composition;
                }
            }
        }

        private bool TryCompose(TestBundle testBundle, IGrouping<TestBundleType, TestBundle> partials, out TestBundle composition)
        {
            composition = default;
            using (_logger.BeginScope().WithCorrelationHandle("Merge").AttachElapsed())
            {
                _logger.Log(Abstraction.Layer.Infrastructure().Meta(new { TestBundleName = testBundle.Name.ToString() }));
                try
                {
                    composition = Merge(testBundle, partials);
                    _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Completed());
                    return true;
                }
                catch (Exception inner)
                {
                    _logger.Log(Abstraction.Layer.Infrastructure().Routine(nameof(TryCompose)).Faulted(), inner);
                    return false;
                }
            }
        }

        private TestBundle Merge(TestBundle regular, IGrouping<TestBundleType, TestBundle> partials)
        {
            var mergeableTestBundleProperties =
                typeof(TestBundle)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);

            var newTestBundle = _componentContext.Resolve<TestBundle>();

            foreach (var testBundleProperty in mergeableTestBundleProperties)
            {
                // Set original value by default.
                testBundleProperty.SetValue(newTestBundle, testBundleProperty.GetValue(regular));

                // Ignore non-mergeable properties.
                if (!typeof(IEnumerable<IMergeable>).IsAssignableFrom(testBundleProperty.PropertyType))
                {
                    continue;
                }

                var testBundlePropertyValue = (IEnumerable<IMergeable>)testBundleProperty.GetValue(regular);
                var testBundlePropertyConstructor = testBundleProperty.PropertyType.GetConstructor(Type.EmptyTypes) ?? throw new NotSupportedException();
                var newTestBundlePropertyValue = testBundlePropertyConstructor.Invoke(null);

                foreach (var mergeable in testBundlePropertyValue)
                {
                    var other = default(IMergeable);
                    if (!(mergeable.Merge is null))
                    {
                        var partialTestBundle = partials.SingleOrThrow
                        (
                            p => p.Name == mergeable.Merge.OtherName,
                            onEmpty: () => DynamicException.Create("OtherTestBundleNotFound", $"Could not find test bundle '{mergeable.Merge.OtherName}'.")
                        );

                        var partialTestBundleValue = (IEnumerable<IMergeable>)testBundleProperty.GetValue(partialTestBundle);
                        other = partialTestBundleValue.SingleOrThrow
                        (
                            x => x.Id == mergeable.Merge.OtherId,
                            onEmpty: () => DynamicException.Create("OtherMergeableNotFound", $"Could not find mergeable '{mergeable.Merge.OtherId}'.")
                        );
                    }

                    var newMergeable = (IMergeable)_componentContext.Resolve(mergeable.GetType());

                    foreach (var mergeableProperty in mergeable.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
                    {
                        var currentValue = mergeableProperty.GetValue(mergeable);

                        if (mergeableProperty.IsDefined(typeof(MergeableAttribute)) && !(other is null))
                        {
                            var otherValue = mergeableProperty.GetValue(other);
                            if (otherValue is null && mergeableProperty.GetCustomAttribute<MergeableAttribute>().Required)
                            {
                                throw DynamicException.Create($"{mergeableProperty.Name}Null", $"Mergeable property value must not be null.");
                            }

                            var newValue = default(object);
                            switch (currentValue)
                            {
                                case IEnumerable<SoftString> x when otherValue is IEnumerable<SoftString> y:
                                    newValue = x.Union(y).ToList();
                                    break;

                                case IEnumerable<KeyValuePair<SoftString, object>> x when otherValue is IEnumerable<KeyValuePair<SoftString, object>> y:
                                    newValue = x.Union(y).ToDictionary(p => p.Key, p => p.Value);
                                    break;
                                
                                case null:
                                    newValue = otherValue;
                                    break;
                                
                                default:
                                    newValue = currentValue;
                                    break;
                            }

                            mergeableProperty.SetValue(newMergeable, newValue);
                        }
                        else
                        {
                            mergeableProperty.SetValue(newMergeable, currentValue);
                        }
                    }

                    ((IList)newTestBundlePropertyValue).Add(newMergeable);
                }

                testBundleProperty.SetValue(newTestBundle, newTestBundlePropertyValue);
            }

            return newTestBundle;
        }
    }
}