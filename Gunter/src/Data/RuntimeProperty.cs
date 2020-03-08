using System;
using System.Collections.Generic;
using Gunter.Services;
using JetBrains.Annotations;
using Reusable;
using Reusable.Collections;

namespace Gunter.Data
{
    [PublicAPI]
    [UsedImplicitly]
    public interface IProperty : IEquatable<IProperty>
    {
        //[AutoEqualityProperty]
        Type? SourceType { get; }
        
        [AutoEqualityProperty]
        SoftString Name { get; }

        object? GetValue(object? obj);
    }

    public abstract class RuntimeProperty : IProperty
    {
        protected RuntimeProperty(Type? sourceType, string name)
        {
            SourceType = sourceType;
            Name = name;
        }

        public Type? SourceType { get; }

        public SoftString Name { get; }

        public abstract object? GetValue(object? obj);

        #region IEquatable

        public bool Equals(IProperty other) => AutoEquality<IProperty>.Comparer.Equals(this, other);

        public override bool Equals(object other) => other is IProperty runtimeVariable && Equals(runtimeVariable);

        public override int GetHashCode() => AutoEquality<IProperty>.Comparer.GetHashCode(this);

        #endregion

        public static class BuiltIn
        {
            public static class Program
            {
                public static readonly IProperty FullName = RuntimePropertyFactory.Create<Gunter.ProgramInfo>(_ => ProgramInfo.FullName);
                public static readonly IProperty Environment = RuntimePropertyFactory.Create<Gunter.ProgramInfo>(x => x.Environment);
            }

            public static class TestBundle
            {
                //public static readonly IRuntimeVariable Name = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => Path.GetFileNameWithoutExtension(x.FullName));
                public static readonly IProperty FullName = RuntimePropertyFactory.Create<Gunter.Data.Theory>(x => x.FullName);
                public static readonly IProperty FileName = RuntimePropertyFactory.Create<Gunter.Data.Theory>(x => x.FileName);
            }

            public static class TestCase
            {
                public static readonly IProperty Level = RuntimePropertyFactory.Create<Gunter.Data.ITestCase>(x => x.Level);
                public static readonly IProperty Message = RuntimePropertyFactory.Create<Gunter.Data.ITestCase>(x => x.Message);
            }

            // public static class TestCounter
            // {
            //     public static readonly IProperty GetDataElapsed = RuntimePropertyFactory.Create<Gunter.Data.TestCounter>(x => x.GetDataElapsed);
            //     public static readonly IProperty AssertElapsed = RuntimePropertyFactory.Create<Gunter.Data.TestCounter>(x => x.RunTestElapsed);
            // }

            public static IEnumerable<IProperty> Enumerate()
            {
                yield return Program.FullName;
                yield return Program.Environment;
                yield return TestBundle.FullName;
                yield return TestBundle.FileName;
                yield return TestCase.Level;
                yield return TestCase.Message;
                //yield return TestCounter.GetDataElapsed;
                //yield return TestCounter.AssertElapsed;
            }
        }
    }

    internal class InstanceProperty : RuntimeProperty
    {
        private readonly Func<object, object?> _getValue;

        public InstanceProperty
        (
            Type sourceType,
            string name,
            Func<object, object?> getValue
        ) : base(sourceType, name)
        {
            _getValue = getValue;
        }
        
        public override object? GetValue(object? obj) => _getValue(obj);
    }

    [PublicAPI]
    [UsedImplicitly]
    public class StaticProperty : RuntimeProperty
    {
        private readonly object _value;

        public StaticProperty(string name, object value) : base(default, name)
        {
            _value = value;
        }

        public override object GetValue(object? obj) => _value;

        public static implicit operator StaticProperty(KeyValuePair<string, object> kvp) => new StaticProperty(kvp.Key, kvp.Value);

        //public static implicit operator KeyValuePair<SoftString, object>(StaticProperty tbv) => new KeyValuePair<SoftString, object>(tbv.Name, tbv.Value);
    }

    internal static class PropertyExtensions
    {
        public static string ToFormatString(this IProperty property, string format)
        {
            return $"{{{property.Name.ToString()}:{format}}}";
        }

        public static string ToPlaceholder(this IProperty property)
        {
            return "{" + property.Name.ToString() + "}";
        }
    }
}