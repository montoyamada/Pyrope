using System;
using System.Linq;

namespace Pyrope.GarnetServer.Vector
{
    public static class ScalarQuantizer
    {
        public static byte[] Quantize(float[] vector, out float min, out float max)
        {
            if (vector == null || vector.Length == 0)
            {
                min = 0;
                max = 0;
                return Array.Empty<byte>();
            }

            var quantized = new byte[vector.Length];
            Quantize(vector.AsSpan(), quantized.AsSpan(), out min, out max);
            return quantized;
        }

        public static void Quantize(ReadOnlySpan<float> vector, Span<byte> destination, out float min, out float max)
        {
            if (vector.Length != destination.Length)
            {
                throw new ArgumentException("Vector and destination lengths must match.");
            }

            if (vector.IsEmpty)
            {
                min = 0;
                max = 0;
                return;
            }

            min = float.MaxValue;
            max = float.MinValue;

            for (int i = 0; i < vector.Length; i++)
            {
                if (vector[i] < min) min = vector[i];
                if (vector[i] > max) max = vector[i];
            }

            var range = max - min;

            if (range == 0)
            {
                destination.Fill(0);
                return;
            }

            // Scale to 0..255
            float scale = 255.0f / range;

            for (int i = 0; i < vector.Length; i++)
            {
                float normalized = (vector[i] - min) * scale;
                // Clamp and round
                destination[i] = (byte)Math.Clamp((int)Math.Round(normalized), 0, 255);
            }
        }

        public static float[] Dequantize(byte[] quantized, float min, float max)
        {
            if (quantized == null) return Array.Empty<float>();

            var range = max - min;
            var result = new float[quantized.Length];

            if (range == 0)
            {
                Array.Fill(result, min);
                return result;
            }

            float scale = range / 255.0f;

            for (int i = 0; i < quantized.Length; i++)
            {
                result[i] = min + (quantized[i] * scale);
            }

            return result;
        }
    }
}
