# Code Review: P10 Optimizations Design

## Summary
The plan is sound and addresses the key performance bottlenecks identified in P10-14.

## Feedback

1.  **Dense Index Mapping**:
    *   Using `List<T>` + `Dictionary<string, int>` is a significant improvement over the current dictionary-based storage.
    *   *Note*: `List<byte[]>` stores references contiguously, but the byte arrays themselves are scattered on the heap. A single flattened `byte[]` would be optimal for cache locality but adds complexity (resizing). Proceed with `List<byte[]>` for now as a balanced step.

2.  **Thread Safety**:
    *   `ReaderWriterLockSlim` correctly protects the new collections.
    *   Ensure `_deleted` (BitArray) is accessed safely. Since `Search` (ReadLock) reads and `Delete` (WriteLock) writes, it is safe.

3.  **ArrayPool Usage**:
    *   **Critical**: `ScalarQuantizer.Quantize` MUST be refactored to accept `Span<byte>` (destination) to truly benefit from `ArrayPool`. If it returns a `new byte[]`, the pool is useless.

4.  **Additional Recommendations**:
    *   **PriorityQueue**: The current `PriorityQueue` implementation might allocate. For small `topK`, consider simpler structures if GC is still high.
    *   **Alignment**: SIMD prefers 32-byte aligned data. `byte[]` allocation in .NET is generally aligned well enough for AVX2, but keep it in mind.

## Approval
Approved to proceed with the implementation, prioritizing the `Span` refactoring in `ScalarQuantizer`.
