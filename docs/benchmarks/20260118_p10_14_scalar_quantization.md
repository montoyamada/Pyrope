# Benchmark Result: Scalar Quantization (P10-14)

**Date**: 2026-01-18
**Task**: P10-14 Scalar Quantization (SQ8)
**Environment**: Local (Mac, .NET 8.0)

## Summary

Implemented `ScalarQuantizer` (Float -> Byte) and SIMD-accelerated `L2Squared8Bit` using `unsafe` pointers and `Vector.Widen`.
Benchmarked on Synthetic 128-dim dataset (100k vectors).

## Results

| Metric | Baseline (Float32) | SQ8 (Optimized) | Speedup |
| :--- | :--- | :--- | :--- |
| **Throughput (QPS)** | 298.8 QPS | **461.4 QPS** | **1.54x** |
| **Mean Latency** | 13.4 ms | **8.7 ms** | - |
| **P50 Latency** | 4.2 ms | 8.1 ms | - |

**Observation**: SQ8 provides a **54% throughput increase** over optimized Float32.
Lower memory bandwidth usage (12MB vs 50MB) and efficient SIMD instructions contribute to the speedup.
P50 latency for Float32 was lower (4ms) but had high tail latency (P99 144ms), whereas SQ8 was more consistent (P99 20ms).
This suggests SQ8 reduces GC pressure significantly (less memory churn or better cache locality).

## Implementation Details
- **Quantization**: Min-Max scaling per vector (stored as `byte[]` and `(min, max)` tuple).
- **Distance**: `L2Squared8Bit` uses `fixed` pointers, 4x loop unrolling, and `Vector.Widen` chain.
- **Index**: `BruteForceVectorIndex` updated to support `EnableQuantization` flag.

## Discussion & Analysis

### 1. Throughput & Stability
- **Throughput (1.54x)**: The improvement is significant, primarily driven by reduced memory bandwidth (32-bit float → 8-bit byte). The speedup is not 4x due to the computational overhead of `Vector.Widen` and integer arithmetic in the SIMD path.
- **P99 Latency (144ms → 20ms)**: The dramatic drop in tail latency indicates improved system stability. The smaller memory footprint likely reduces CPU cache thrashing and GC pauses, preventing latency spikes under load.
- **P50 Regression (4.2ms → 8.1ms)**: The increase in median latency suggests a higher fixed cost per query, likely due to:
  - Dictionary iteration overhead (`Dictionary<string, byte[]>`) vs direct array access.
  - Runtime quantization overhead for the query vector.
  - JIT or cold-start effects on the new code path.

### 2. Accuracy & Recall
- **Current Limitation**: The implementation uses **Local Scalar Quantization (LSQ)** (min/max stored per vector) but performs distance calculations on raw bytes (`DotProduct8Bit`). This ignores the scaling factors, effectively treating all vectors as if they had the same dynamic range.
- **Impact**: Ranking accuracy (Recall@K) is likely degraded compared to Float32.
- **Next Steps**:
  - Implement a Re-ranking phase: Use SQ8 to retrieve top N*K candidates, then re-score with Float32.
  - Or switch to **Global Scalar Quantization** (shared min/max) if vector distribution allows.

### 3. Future Optimization (P10-12)
- **Memory Layout**: Moving from `Dictionary<string, byte[]>` to a contiguous `byte[]` buffer (SoA or Flat Blob) will eliminate pointer chasing and drastically improve cache locality, potentially doubling the speedup.