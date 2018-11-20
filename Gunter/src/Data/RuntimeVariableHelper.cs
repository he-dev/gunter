using System.Collections.Generic;
using System.IO;

namespace Gunter.Data
{
    internal partial class RuntimeVariable
    {
        public static class Program
        {
            public static readonly IRuntimeVariable FullName = RuntimeVariableFactory.Create<Gunter.ProgramInfo>(_ => ProgramInfo.FullName);
            public static readonly IRuntimeVariable Environment = RuntimeVariableFactory.Create<Gunter.ProgramInfo>(x => x.Environment);
        }

        public static class TestBundle
        {
            //public static readonly IRuntimeVariable Name = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => Path.GetFileNameWithoutExtension(x.FullName));
            public static readonly IRuntimeVariable FullName = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => x.FullName);
            public static readonly IRuntimeVariable FileName = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => x.FileName);
        }

        public static class TestCase
        {
            public static readonly IRuntimeVariable Level = RuntimeVariableFactory.Create<Gunter.Data.TestCase>(x => x.Level);
            public static readonly IRuntimeVariable Message = RuntimeVariableFactory.Create<Gunter.Data.TestCase>(x => x.Message);
        }

        public static class TestCounter
        {
            public static readonly IRuntimeVariable GetDataElapsed = RuntimeVariableFactory.Create<Gunter.Data.TestCounter>(x => x.GetDataElapsed);
            public static readonly IRuntimeVariable AssertElapsed = RuntimeVariableFactory.Create<Gunter.Data.TestCounter>(x => x.RunTestElapsed);
        }

        public static IEnumerable<IRuntimeVariable> Enumerate()
        {
            yield return Program.FullName;
            yield return Program.Environment;
            yield return TestBundle.FullName;
            yield return TestBundle.FileName;
            yield return TestCase.Level;
            yield return TestCase.Message;
            yield return TestCounter.GetDataElapsed;
            yield return TestCounter.AssertElapsed;
        }
    }
}