using System.Collections.Generic;

namespace Gunter.Data
{
    internal partial class RuntimeVariable
    {
        public static class Program
        {
            public static readonly IRuntimeVariable FullName = RuntimeVariableFactory.Create<Gunter.Program>(x => x.FullName);
            public static readonly IRuntimeVariable Environment = RuntimeVariableFactory.Create<Gunter.Program>(x => x.Environment);
        }

        public static class TestFile
        {
            public static readonly IRuntimeVariable FullName = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => x.FullName);
            public static readonly IRuntimeVariable FileName = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => x.FileName);
        }

        public static class TestCase
        {
            public static readonly IRuntimeVariable Level = RuntimeVariableFactory.Create<Gunter.Data.TestCase>(x => x.Level);
            public static readonly IRuntimeVariable Message = RuntimeVariableFactory.Create<Gunter.Data.TestCase>(x => x.Message);
        }

        public static class TestMetrics
        {
            public static readonly IRuntimeVariable GetDataElapsed = RuntimeVariableFactory.Create<Gunter.Data.TestMetrics>(x => x.GetDataElapsed);
            public static readonly IRuntimeVariable AssertElapsed = RuntimeVariableFactory.Create<Gunter.Data.TestMetrics>(x => x.AssertElapsed);
        }

        public static IEnumerable<IRuntimeVariable> Enumerate()
        {
            yield return Program.FullName;
            yield return Program.Environment;
            yield return TestFile.FullName;
            yield return TestFile.FileName;
            yield return TestCase.Level;
            yield return TestCase.Message;
            yield return TestMetrics.GetDataElapsed;
            yield return TestMetrics.AssertElapsed;
        }
    }
}