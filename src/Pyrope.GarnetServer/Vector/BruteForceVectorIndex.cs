using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Pyrope.GarnetServer.Vector
{
    public class BruteForceVectorIndex : IVectorIndex
    {
        // Dense Index Mapping (P10-12)
        private readonly List<VectorEntry> _vectors = new();
        private readonly List<string> _ids = new();
        private readonly Dictionary<string, int> _idMap = new();
        private readonly List<bool> _isDeleted = new();
        
        // Quantized Data (P10-12)
        private readonly List<byte[]> _quantizedVectors = new();
        private readonly List<(float Min, float Max)> _quantizationParams = new();
        
        private readonly ReaderWriterLockSlim _lock = new();

        public bool EnableQuantization { get; set; } = false;

        public BruteForceVectorIndex(int dimension, VectorMetric metric)
        {
            if (dimension <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dimension), "Dimension must be positive.");
            }

            Dimension = dimension;
            Metric = metric;
        }

        public int Dimension { get; }
        public VectorMetric Metric { get; }

        public void Build() { }

        public void Snapshot(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path cannot be empty.", nameof(path));

            _lock.EnterReadLock();
            try
            {
                // Serialize as dictionary to maintain compatibility (slow but safe)
                var dto = new Dictionary<string, VectorEntryDto>(_vectors.Count);
                for (int i = 0; i < _vectors.Count; i++)
                {
                    if (_isDeleted[i]) continue;
                    dto[_ids[i]] = new VectorEntryDto { Vector = _vectors[i].Vector, Norm = _vectors[i].Norm };
                }
                var json = System.Text.Json.JsonSerializer.Serialize(dto);
                System.IO.File.WriteAllText(path, json);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path cannot be empty.", nameof(path));
            if (!System.IO.File.Exists(path)) throw new System.IO.FileNotFoundException("Snapshot file not found.", path);

            var json = System.IO.File.ReadAllText(path);
            var dto = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, VectorEntryDto>>(json);

            if (dto != null)
            {
                _lock.EnterWriteLock();
                try
                {
                    Clear();
                    foreach (var kvp in dto)
                    {
                        InternalAdd(kvp.Key, kvp.Value.Vector, kvp.Value.Norm);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        private void Clear()
        {
            _vectors.Clear();
            _ids.Clear();
            _idMap.Clear();
            _isDeleted.Clear();
            _quantizedVectors.Clear();
            _quantizationParams.Clear();
        }

        public IndexStats GetStats()
        {
            _lock.EnterReadLock();
            try
            {
                int activeCount = _idMap.Count; // idMap only contains active keys
                return new IndexStats(activeCount, Dimension, Metric.ToString());
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Add(string id, float[] vector)
        {
            ValidateId(id);
            ValidateVector(vector);

            _lock.EnterWriteLock();
            try
            {
                if (_idMap.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Vector with id '{id}' already exists.");
                }

                float norm = Metric == VectorMetric.Cosine ? VectorMath.ComputeNorm(vector) : 0f;
                // Copy vector to avoid external modification
                var vecCopy = new float[vector.Length];
                Array.Copy(vector, vecCopy, vector.Length);

                InternalAdd(id, vecCopy, norm);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void InternalAdd(string id, float[] vector, float norm)
        {
            int index = _vectors.Count;
            
            _idMap.Add(id, index);
            _ids.Add(id);
            _vectors.Add(new VectorEntry(vector, norm));
            _isDeleted.Add(false);

            if (EnableQuantization)
            {
                var qv = ScalarQuantizer.Quantize(vector, out float min, out float max);
                _quantizedVectors.Add(qv);
                _quantizationParams.Add((min, max));
            }
            else
            {
                // Keep list structure aligned even if disabled, or handle logic?
                // If disabled, we might have empty list or need to fill with null/empty to keep index sync.
                // Simpler: Add placeholder or handle index mismatch.
                // Let's add empty/default to keep indices aligned for simplicity.
                _quantizedVectors.Add(Array.Empty<byte>());
                _quantizationParams.Add(default);
            }
        }

        public void Upsert(string id, float[] vector)
        {
            ValidateId(id);
            ValidateVector(vector);

            _lock.EnterWriteLock();
            try
            {
                float norm = Metric == VectorMetric.Cosine ? VectorMath.ComputeNorm(vector) : 0f;
                var vecCopy = new float[vector.Length];
                Array.Copy(vector, vecCopy, vector.Length);

                if (_idMap.TryGetValue(id, out int index))
                {
                    // Update existing
                    _vectors[index] = new VectorEntry(vecCopy, norm);
                    // Ensure it is not deleted (if reviving) - though idMap usually implies active.
                    // If we support revive logic, we check isDeleted.
                    _isDeleted[index] = false;

                    if (EnableQuantization)
                    {
                        var qv = ScalarQuantizer.Quantize(vector, out float min, out float max);
                        _quantizedVectors[index] = qv;
                        _quantizationParams[index] = (min, max);
                    }
                }
                else
                {
                    InternalAdd(id, vecCopy, norm);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Delete(string id)
        {
            ValidateId(id);

            _lock.EnterWriteLock();
            try
            {
                if (_idMap.TryGetValue(id, out int index))
                {
                    _isDeleted[index] = true;
                    _idMap.Remove(id);
                    // We don't remove from Lists to keep indices stable.
                    // This creates fragmentation, but we assume simple usage or later compaction.
                    return true;
                }
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IEnumerable<KeyValuePair<string, float[]>> Scan()
        {
            _lock.EnterReadLock();
            try
            {
                // Create snapshot of active vectors
                var list = new List<KeyValuePair<string, float[]>>(_idMap.Count);
                for(int i=0; i<_vectors.Count; i++)
                {
                    if (!_isDeleted[i])
                    {
                        list.Add(new KeyValuePair<string, float[]>(_ids[i], _vectors[i].Vector));
                    }
                }
                return list;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<SearchResult> Search(float[] query, int topK, SearchOptions? options = null)
        {
            ValidateVector(query);
            if (topK <= 0) throw new ArgumentOutOfRangeException(nameof(topK), "topK must be positive.");

            var maxScans = options?.MaxScans;

            _lock.EnterReadLock();
            try
            {
                int count = _vectors.Count;
                if (count == 0) return Array.Empty<SearchResult>();

                int scanLimit = maxScans.HasValue ? Math.Min(maxScans.Value, count) : count;
                if (scanLimit <= 0) return Array.Empty<SearchResult>();

                var heap = new PriorityQueue<SearchResult, float>();
                int scanned = 0;
                
                // P10-11: Pooling
                byte[]? qQuery = null;
                try
                {
                    if (EnableQuantization)
                    {
                        qQuery = ArrayPool<byte>.Shared.Rent(Dimension);
                        ScalarQuantizer.Quantize(query, qQuery.AsSpan(0, Dimension), out _, out _);
                        
                        // Optimized Loop P10-12
                        // Access List directly by index
                        for (int i = 0; i < count; i++)
                        {
                            if (_isDeleted[i]) continue;
                            if (scanned >= scanLimit) break;
                            scanned++;

                            var qTarget = _quantizedVectors[i];
                            // Check if valid quantization exists (in case it was added while disabled)
                            if (qTarget == null || qTarget.Length == 0) continue; // fallback or skip?

                            float score = Metric switch
                            {
                                VectorMetric.L2 => -VectorMath.L2Squared8Bit(qQuery, qTarget),
                                VectorMetric.InnerProduct => VectorMath.DotProduct8Bit(qQuery, qTarget),
                                VectorMetric.Cosine => VectorMath.DotProduct8Bit(qQuery, qTarget), // approx
                                _ => throw new InvalidOperationException()
                            };

                            heap.Enqueue(new SearchResult(_ids[i], score), score);
                            if (heap.Count > topK) heap.Dequeue();
                        }
                    }
                    else
                    {
                        float queryNorm = Metric == VectorMetric.Cosine ? VectorMath.ComputeNorm(query) : 0f;

                        for (int i = 0; i < count; i++)
                        {
                            if (_isDeleted[i]) continue;
                            if (scanned >= scanLimit) break;
                            scanned++;

                            var entry = _vectors[i];
                            float score = Metric switch
                            {
                                VectorMetric.L2 => -VectorMath.L2SquaredUnsafe(query, entry.Vector),
                                VectorMetric.InnerProduct => VectorMath.DotProductUnsafe(query, entry.Vector),
                                VectorMetric.Cosine => (queryNorm < 1e-6f || entry.Norm < 1e-6f) ? 0f : VectorMath.DotProductUnsafe(query, entry.Vector) / (queryNorm * entry.Norm),
                                _ => throw new InvalidOperationException()
                            };

                            heap.Enqueue(new SearchResult(_ids[i], score), score);
                            if (heap.Count > topK) heap.Dequeue();
                        }
                    }
                }
                finally
                {
                    if (qQuery != null) ArrayPool<byte>.Shared.Return(qQuery);
                }

                if (heap.Count == 0) return Array.Empty<SearchResult>();

                var results = new List<SearchResult>(heap.Count);
                while (heap.Count > 0) results.Add(heap.Dequeue());
                results.Sort((a, b) => b.Score.CompareTo(a.Score));
                return results;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void ValidateVector(float[] vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (vector.Length != Dimension) throw new ArgumentException("Vector dimension mismatch.", nameof(vector));
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id cannot be empty.", nameof(id));
        }

        private sealed record VectorEntry(float[] Vector, float Norm);

        private class VectorEntryDto
        {
            public float[] Vector { get; set; } = Array.Empty<float>();
            public float Norm { get; set; }
        }
    }
}