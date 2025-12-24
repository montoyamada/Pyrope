using Xunit;
using Pyrope.GarnetServer.Utils;

namespace Pyrope.GarnetServer.Tests.Utils
{
    public class KeyUtilsTests
    {
        [Fact]
        public void GetIndexConfigKey_ReturnsCorrectFormat()
        {
            var key = KeyUtils.GetIndexConfigKey("tenant1", "indexA");
            Assert.Equal("_meta:tenant:tenant1:index:indexA:config", key);
        }

        [Fact]
        public void GetTenantConfigKey_ReturnsCorrectFormat()
        {
            var key = KeyUtils.GetTenantConfigKey("tenant1");
            Assert.Equal("_meta:tenant:tenant1:config", key);
        }
    }
}
