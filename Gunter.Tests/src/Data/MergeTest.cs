using Gunter.Data;
using Xunit;

namespace Gunter.Tests.Data
{
    public class MergeTest
    {
        [Fact]
        public void Can_parse_name_and_id()
        {
            var merge = Merge.Parse("some-name#some-id");
            
            Assert.Equal("some-name", merge.Name);
            Assert.Equal("some-id", merge.Id);
        }
        
        [Fact]
        public void Can_parse_name_only()
        {
            var merge = Merge.Parse("some-name");
            
            Assert.Equal("some-name", merge.Name);
            Assert.Null(merge.Id);
        }
    }
}