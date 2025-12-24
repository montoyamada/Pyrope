using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pyrope.GarnetServer.Model
{
    public class IndexConfig
    {
        [JsonPropertyName("dim")]
        public int Dimension { get; set; }

        [JsonPropertyName("metric")]
        public string Metric { get; set; } = "L2"; // L2, IP, COSINE

        [JsonPropertyName("algo")]
        public string Algorithm { get; set; } = "HNSW"; // HNSW, FLAT

        [JsonPropertyName("params")]
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        [JsonPropertyName("train_config")]
        public TrainingConfig? TrainConfig { get; set; }

        public void Validate()
        {
            if (Dimension <= 0)
            {
                throw new ArgumentException("Dimension must be positive.", nameof(Dimension));
            }
            if (string.IsNullOrWhiteSpace(Metric))
            {
                throw new ArgumentException("Metric cannot be empty.", nameof(Metric));
            }
        }
    }

    public class TrainingConfig
    {
        [JsonPropertyName("sample_size")]
        public int SampleSize { get; set; }
    }
}
