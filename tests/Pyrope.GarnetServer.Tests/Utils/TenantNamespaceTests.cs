using Pyrope.GarnetServer.Utils;
using Xunit;

namespace Pyrope.GarnetServer.Tests.Utils
{
    public class TenantNamespaceTests
    {
        [Theory]
        [InlineData("tenant1")]
        [InlineData("tenant_1")]
        [InlineData("tenant-1")]
        [InlineData("TENANT")]
        public void TryValidateTenantId_AllowsSafeSegments(string tenantId)
        {
            Assert.True(TenantNamespace.TryValidateTenantId(tenantId, out var error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("tenant:bad")]
        [InlineData("tenant/bad")]
        [InlineData("tenant bad")]
        public void TryValidateTenantId_RejectsUnsafeSegments(string tenantId)
        {
            Assert.False(TenantNamespace.TryValidateTenantId(tenantId, out var error));
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("index1")]
        [InlineData("index_1")]
        [InlineData("index-1")]
        public void TryValidateIndexName_AllowsSafeSegments(string indexName)
        {
            Assert.True(TenantNamespace.TryValidateIndexName(indexName, out var error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("index:bad")]
        [InlineData("index bad")]
        public void TryValidateIndexName_RejectsUnsafeSegments(string indexName)
        {
            Assert.False(TenantNamespace.TryValidateIndexName(indexName, out var error));
            Assert.NotNull(error);
        }
    }
}
