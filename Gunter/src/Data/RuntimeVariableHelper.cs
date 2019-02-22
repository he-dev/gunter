using System.Collections.Generic;
using System.IO;
using Gunter.Services;

namespace Gunter.Data
{
    internal partial class RuntimeValue
    {
        public static class Program
        {
            public static readonly IRuntimeValue FullName = RuntimeVariableFactory.Create<Gunter.ProgramInfo>(_ => ProgramInfo.FullName);
            public static readonly IRuntimeValue Environment = RuntimeVariableFactory.Create<Gunter.ProgramInfo>(x => x.Environment);
        }

        public static class TestBundle
        {
            //public static readonly IRuntimeVariable Name = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => Path.GetFileNameWithoutExtension(x.FullName));
            public static readonly IRuntimeValue FullName = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => x.FullName);
            public static readonly IRuntimeValue FileName = RuntimeVariableFactory.Create<Gunter.Data.TestBundle>(x => x.FileName);
        }

        public static class TestCase
        {
            public static readonly IRuntimeValue Level = RuntimeVariableFactory.Create<Gunter.Data.TestCase>(x => x.Level);
            public static readonly IRuntimeValue Message = RuntimeVariableFactory.Create<Gunter.Data.TestCase>(x => x.Message);
        }

        public static class TestCounter
        {
            public static readonly IRuntimeValue GetDataElapsed = RuntimeVariableFactory.Create<Gunter.Data.TestCounter>(x => x.GetDataElapsed);
            public static readonly IRuntimeValue AssertElapsed = RuntimeVariableFactory.Create<Gunter.Data.TestCounter>(x => x.RunTestElapsed);
        }

        public static IEnumerable<IRuntimeValue> Enumerate()
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