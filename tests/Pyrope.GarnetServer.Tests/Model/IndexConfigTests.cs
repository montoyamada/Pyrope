using System.Text.Json;
using Pyrope.GarnetServer.Model;
using Xunit;

namespace Pyrope.GarnetServer.Tests.Model
{
    public class IndexConfigTests
    {
        [Fact]
        public void CanSerializeAndDeserialize()
        {
            var config = new IndexConfig
            {
                Dimension = 128,
                Metric = "L2",
                Algorithm = "HNSW",
                TrainConfig = new TrainingConfig { SampleSize = 1000 }
            };

            var json = JsonSerializer.Serialize(config);
            var deserialized = JsonSerializer.Deserialize<IndexConfig>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(128, deserialized.Dimension);
            Assert.Equal("L2", deserialized.Metric);
            Assert.Equal(1000, deserialized?.TrainConfig?.SampleSize);
        }

        [Fact]
        public void Validate_WithInvalidDimension_ThrowsException()
        {
            var config = new IndexConfig { Dimension = 0 };
            Assert.Throws<ArgumentException>(() => config.Validate());
        }

        [Fact]
        public void Validate_WithValidConfig_DoesNotThrow()
        {
            var config = new IndexConfig { Dimension = 64, Metric = "COSINE", Algorithm = "FLAT" };
            config.Validate();
        }
    }
}
