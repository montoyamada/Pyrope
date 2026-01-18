# Implementation Plan: P10 Optimizations (P10-10, P10-11, P10-12)

This document outlines the detailed design and implementation plan for the P10 optimization tasks.
The goal is to improve throughput, reduce latency/GC pressure, and optimize sidecar communication.

## Tasks Overview

*   **P10-12: Vector Memory Layout Optimization** (Priority: High)
    *   Transition from `Dictionary<string, float[]/byte[]>` to contiguous memory structures.
    *   Improve CPU cache locality and simplify SIMD access.
*   **P10-11: Memory Pool / Object Reuse** (Priority: Medium)
    *   Utilize `ArrayPool` for temporary buffers to reduce GC allocations.
*   **P10-10: Sidecar Communication Optimization** (Priority: Low)
    *   Optimize gRPC interaction overhead.

---

## Detailed Design

### 1. P10-12: Vector Memory Layout Optimization

**Problem:**
Current `BruteForceVectorIndex` uses `Dictionary<string, VectorEntry>` (Float32) and `Dictionary<string, byte[]>` (SQ8).
Iterating over a dictionary for search involves pointer chasing (jumping around heap memory), which causes cache misses and stalls SIMD execution.

**Solution:**
Implement a **Dense Index Mapping** strategy.

1.  **Internal ID Mapping:**
    *   Maintain a bidirectional map: `string ExternalID <-> int InternalID`.
    *   `InternalID` is a dense integer index `0..N`.
    *   Use `List<string> _ids` for `Internal -> External`.
    *   Use `Dictionary<string, int> _idMap` for `External -> Internal`.

2.  **Storage Layout (SoA-like or Contiguous Blocks):**
    *   **SQ8 Store:** Use `List<byte[]>` where `_quantizedVectors[i]` corresponds to `InternalID=i`.
        *   *Optimization*: Eventually, a single flattened `byte[]` (`N * Dim`) is best, but `List<byte[]>` is a good first step (better than Dictionary) and easier to manage dynamic growth. Let's aim for `List<byte[]>` first, as `BruteForceVectorIndex` is simple.
        *   Wait, for maximum performance as requested, a flattened `Memory<byte>` or `List<byte>` is better. But resizing a huge array is costly.
        *   **Decision**: Use `List<byte[]>` for simplicity of resizing/deletion logic for now, OR `Array<byte[]>` (jagged array). The key is avoiding the Dictionary bucket lookup. Accessing `List[i]` is fast.
    *   **Float32 Store:** Similarly `List<float[]>` (or `VectorEntry` struct if we need Norm).
    *   **Metadata:** `List<(float Min, float Max)>` for quantization params.

3.  **Deletion Handling:**
    *   Use a `BitArray` or `HashSet<int>` to mark `deleted` InternalIDs.
    *   Or use a "Swap and Pop" strategy? No, that invalidates external IDs if we change internal IDs.
    *   **Strategy**: Soft delete using a `BitArray _deleted`. Reclaim slots? Complicated. For MVP optimization, just mark deleted. Compaction can happen on `Build()` or explicitly.

**Changes:**
*   Modify `BruteForceVectorIndex`.
*   Refactor `Add`, `Upsert`, `Delete`, `Search`.
*   Search loop iterates `0` to `_count`, checks `!_deleted[i]`, then computes distance.

### 2. P10-11: Memory Pool / Object Reuse

**Problem:**
Every `Search` call allocates temporary arrays (e.g., `qQuery` for quantized query).
Every `Add` might allocate intermediate buffers.

**Solution:**
1.  **Search Path:**
    *   Use `ArrayPool<byte>.Shared.Rent(dim)` for the quantized query vector in `Search`.
    *   Return to pool with `finally` block.
2.  **Add/Upsert Path:**
    *   If intermediate quantization buffers are needed, use Pool.
3.  **Vector Storage (Advanced):**
    *   If we use `List<float[]>`, the arrays themselves can be rented? No, `ArrayPool` arrays must be returned. They are not suitable for long-term storage unless we manage a custom pool. We will restrict P10-11 to **transient buffers**.

**Changes:**
*   Modify `BruteForceVectorIndex` and `ScalarQuantizer` to accept/return pooled arrays.

### 3. P10-10: Sidecar Communication Optimization

**Problem:**
`SidecarMetricsReporter` sends metrics potentially too often or synchronously waiting for locks.

**Solution:**
1.  **Fire-and-Forget:**
    *   Ensure `PushMetrics` is purely async and doesn't block critical paths.
    *   (Already partially async, need to verify).
2.  **Batching:**
    *   Accumulate metrics in `MetricsCollector` and send every X seconds (e.g., 10s or 60s) instead of per-event or short interval.
    *   Tune `SidecarMetricsReporter` interval.

---

## Implementation Plan (TDD Steps)

### Step 1: P10-12 Layout Refactoring (The big change)
*   **Test**: Create `VectorLayoutTests.cs` verifying `Add/Delete/Search` behavior with the new mapping logic. Ensure ID mapping is correct.
*   **Impl**:
    *   Refactor `BruteForceVectorIndex` to replace `_entries` dictionary with `_vectors` (List), `_ids` (List), `_idMap` (Dict).
    *   Implement `InternalID` management.
    *   Update `Search` to iterate by index.
    *   Fix `Delete` to mark bitmask.

### Step 2: P10-11 Pooling
*   **Test**: Verify no memory leaks (difficult in unit test, but check logic).
*   **Impl**:
    *   Update `ScalarQuantizer.Quantize` to generic `Quantize(ReadOnlySpan<float>, Span<byte>)` to allow caller-supplied buffers.
    *   Update `Search` to Rent/Return buffers for quantization.

### Step 3: P10-10 Sidecar Tuning
*   **Impl**: Review and adjust `SidecarMetricsReporter` config/code.

### Step 4: Verification
*   Run Benchmarks (`bench_vectors.sh`) to confirm speedup.

## Request for Review
Please review this design for potential pitfalls, especially regarding:
1.  Thread safety of `List<T>` access during `Search` vs `Add` (Need ReaderWriterLock still).
2.  Memory fragmentation concerns with `List<byte[]>`.
3.  Deletion strategy (BitArray vs Swap-remove).
