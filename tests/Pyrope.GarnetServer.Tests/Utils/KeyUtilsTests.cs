using System;
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

        [Theory]
        [InlineData("tenant:bad", "indexA")]
        [InlineData("tenant1", "index:bad")]
        public void GetIndexConfigKey_RejectsInvalidSegments(string tenantId, string indexName)
        {
            Assert.Throws<ArgumentException>(() => KeyUtils.GetIndexConfigKey(tenantId, indexName));
        }

        [Theory]
        [InlineData("tenant:bad")]
        [InlineData("tenant bad")]
        public void GetTenantConfigKey_RejectsInvalidTenant(string tenantId)
        {
            Assert.Throws<ArgumentException>(() => KeyUtils.GetTenantConfigKey(tenantId));
        }
    }
}
