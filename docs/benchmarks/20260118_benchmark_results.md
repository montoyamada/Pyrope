# Benchmark Results: Synthetic Dataset (Post-P10 Optimization)

**Date**: 2026-01-18
**Environment**: Local Development (macOS/Arm64)
**Version**: P10 Optimization Suite Enabled

## Configuration
- **Dataset**: Synthetic (Random Float32)
- **Dimension**: 128
- **Base Vectors**: 1,000
- **Queries**: 100
- **TopK**: 10
- **Concurrency**: 4

## Results

### Throughput (Load)
- **Rate**: 10,486.4 vec/s
- **Total Time**: 0.10s

### Search Performance
- **QPS**: 1,894.3
- **Total Time**: 0.05s

### Latency Distribution
| Metric | Value |
| :--- | :--- |
| **Min** | 1.192 ms |
| **Mean** | 2.069 ms |
| **P50** | 2.022 ms |
| **P95** | 2.320 ms |
| **P99** | 3.054 ms |
| **Max** | 3.091 ms |

## Analysis

The benchmark demonstrates highly efficient performance for small-scale vector operations, validating the recent P10 optimizations (Memory Layout, Span usage, and Pooling).

1.  **Low Latency**: The P99 latency of ~3ms is exceptionally low, indicating that the `Span<T>` and memory pooling optimizations have effectively minimized GC pressure and overhead in the hot path.
2.  **High Throughput**: A load rate of >10k vectors/second on a local machine suggests the storage engine (Garnet + Custom Vector Index) handles writes efficiently.
3.  **Stability**: The tight spread between P50 (2.02ms) and P99 (3.05ms) confirms predictable performance characteristics, likely benefiting from the `ArrayPool` usage which avoids allocation spikes.

These results serve as a verified baseline for the current "optimized" state. Future tests with larger datasets (e.g., SIFT1M) are recommended to assess scalability.
