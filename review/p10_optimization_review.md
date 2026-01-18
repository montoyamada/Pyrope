# Code Review Request: P10 Optimizations (Layout, Pooling, Span)

## Summary
Implemented Phase 10 performance optimizations focusing on memory layout and allocation reduction.

## Changes

### 1. P10-12: Vector Memory Layout Optimization
*   **Target**: `BruteForceVectorIndex`
*   **Change**: Replaced `Dictionary<string, VectorEntry>` with `List<VectorEntry>` + `Dictionary<string, int>`.
*   **Benefit**: Improved CPU cache locality by iterating over a List (array backing store) instead of a Dictionary (hash buckets).
*   **Deletion**: Implemented soft deletion using `List<bool> _isDeleted` to maintain index stability without costly removals.

### 2. P10-11: Memory Pool / Object Reuse
*   **Target**: `BruteForceVectorIndex.Search`, `ScalarQuantizer`
*   **Change**: 
    *   Updated `ScalarQuantizer` to support `ReadOnlySpan<float>` -> `Span<byte>` quantization.
    *   `Search` now uses `ArrayPool<byte>.Shared` for the temporary quantized query buffer, eliminating per-search byte array allocations.

### 3. Span & SIMD Updates
*   **Target**: `VectorMath`
*   **Change**: Added `ReadOnlySpan<byte>` overloads for `L2Squared8Bit` and `DotProduct8Bit`. Refactored existing byte array methods to use these overloads.

## Verification
*   Added `ScalarQuantizerTests.Quantize_SpanOverload_Works`.
*   Verified `BruteForceVectorIndex` functionality via existing tests (Add/Search/Delete/Persistence).
*   Passed `dotnet test tests/Pyrope.GarnetServer.Tests/Pyrope.GarnetServer.Tests.csproj`.

## Review Checklist
*   [ ] Is the ID mapping logic in `Add`/`Delete` correct?
*   [ ] Is `ArrayPool` usage leak-free (correctly Returned in finally block)?
*   [ ] Are SIMD implementations correct with `fixed` Span pointers?
