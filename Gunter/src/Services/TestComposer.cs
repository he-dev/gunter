using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Custom;
using System.Reflection;
using Autofac;
using Gunter.Annotations;
using Gunter.Data;
using Gunter.Extensions;
using Reusable;
using Reusable.Exceptionize;
using Reusable.Extensions;
using Reusable.OmniLog;
using Reusable.OmniLog.Abstractions;
using Reusable.OmniLog.Nodes;
using Reusable.OmniLog.SemanticExtensions;

namespace Gunter.Services
{
    internal interface ITestComposer
    {
        IEnumerable<Specification> ComposeTests(IEnumerable<Specification> bundles, TestFilter testFilter);
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

        public IEnumerable<Specification> ComposeTests(IEnumerable<Specification> bundles, TestFilter testFilter)
        {
            var bundleGroups = bundles.ToLookup(b => b.TestFile.Type);

            foreach (var bundle in bundleGroups[TestBundleType.Regular])
            {
                var executableTests =
                    from test in bundle.Tests
                    where
                        test.Enabled &&
                        testFilter.Tests.IsNullOr(ids => ids.Select(SoftString.Create).Contains(test.Id)) &&
                        testFilter.Tags.IsNullOr(tags => tags.Select(SoftString.Create).Intersect(test.Tags).Any())
                    select test;

                bundle.Tests = executableTests.ToList();

                if (TryCompose(bundle, bundleGroups[TestBundleType.Template], out var composition))
                {
                    yield return composition;
                }
            }
        }

        private bool TryCompose(Specification specification, IEnumerable<Specification> partials, out Specification composition)
        {
            composition = default;
            using (_logger.BeginScope().WithCorrelationHandle("MergeTests").UseStopwatch())
            {
                _logger.Log(Abstraction.Layer.Service().Meta(new { TestBundleName = specification.Name.ToString() }));
                try
                {
                    composition = Merge(specification, partials);
                    _logger.Log(Abstraction.Layer.Service().Routine(nameof(TryCompose)).Completed());
                    return true;
                }
                catch (Exception inner)
                {
                    _logger.Log(Abstraction.Layer.Service().Routine(nameof(TryCompose)).Faulted(), inner);
                    return false;
                }
            }
        }

        private Specification Merge(Specification regular, IEnumerable<Specification> partials)
        {
            var mergeableTestBundleProperties =
                typeof(Specification)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite);

            var newTestBundle = _componentContext.Resolve<Specification>();

            foreach (var testBundleProperty in mergeableTestBundleProperties)
            {
                // Set original value by default.
                testBundleProperty.SetValue(newTestBundle, testBundleProperty.GetValue(regular));

                // Ignore non-mergeable properties.
                if (!typeof(IEnumerable<IModel>).IsAssignableFrom(testBundleProperty.PropertyType))
                {
                    continue;
                }

                var testBundlePropertyValue = (IEnumerable<IModel>)testBundleProperty.GetValue(regular);
                var testBundlePropertyConstructor = testBundleProperty.PropertyType.GetConstructor(Type.EmptyTypes) ?? throw new NotSupportedException();
                var newTestBundlePropertyValue = testBundlePropertyConstructor.Invoke(null);

                foreach (var mergeable in testBundlePropertyValue)
                {
                    var other = default(IModel);
                    if (mergeable.Merge is {})
                    {
                        var partialTestBundle = partials.Where(p => p.Name == mergeable.Merge.Name).SingleOrThrow
                        (
                            onEmpty: () => DynamicException.Create("OtherTestBundleNotFound", $"Could not find test bundle '{mergeable.Merge.Name}'.")
                        );

                        var partialTestBundleValue = (IEnumerable<IModel>)testBundleProperty.GetValue(partialTestBundle);
                        var mergeId = mergeable.Merge.Id ?? mergeable.Id;
                        other = partialTestBundleValue.Where(x => x.Id == mergeId).SingleOrThrow
                        (
                            onEmpty: () => DynamicException.Create("OtherMergeableNotFound", $"Could not find mergeable '{mergeId}' of '{mergeable.GetType().ToPrettyString()}' with the id '{mergeable.Id}'.")
                        );
                    }

                    var newMergeable = (IModel)_componentContext.Resolve(mergeable.GetType());

                    foreach (var mergeableProperty in mergeable.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
                    {
                        var currentValue = mergeableProperty.GetValue(mergeable);

                        if (mergeableProperty.IsDefined(typeof(MergeableAttribute)) && !(other is null))
                        {
                            var otherValue = mergeableProperty.GetValue(other);
                            if (otherValue is null && mergeableProperty.GetCustomAttribute<MergeableAttribute>().Required)
                            {
                                throw DynamicException.Create($"PropertyNull", $"Mergeable property '{mergeableProperty.Name}' must not be null.");
                            }

                            var newValue = currentValue switch
                            {
                                IEnumerable<SoftString> x when otherValue is IEnumerable<SoftString> y => x.Union(y).ToList(),
                                IEnumerable<KeyValuePair<SoftString, object>> x when otherValue is IEnumerable<KeyValuePair<SoftString, object>> y => x.Union(y).ToDictionary(p => p.Key, p => p.Value),
                                null => otherValue,
                                _ => currentValue
                            };

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