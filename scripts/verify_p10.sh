#!/bin/bash
set -euo pipefail

# P10-15/16 Verification Script
# Usage: ./verify_p10.sh <output_dir>
# Defaults to benchmark/results_$(date +%s)

OUT_DIR="${1:-benchmark/p10_results_$(date +%s)}"
mkdir -p "$OUT_DIR"

echo "=== P10 Verification ==="
echo "Output Directory: $OUT_DIR"

SERVER_HOST="127.0.0.1"
SERVER_PORT="4061"
HTTP_URL="http://localhost:5000" 
# Pyrope server defaults: Port 3278, HttpPort?
# I need to check IndexController or Program.cs of Server to know HTTP port.
# IndexController.cs doesn't show port.
# Pyrope.GarnetServer/Program.cs (if exists) would show it.
# Assuming 8080 or something. 
# Wait, previous tool output said: "Listening on: 127.0.0.1:4451" (Running output check - Step 278)
# Ah! Step 278 command status output showed: "Listening on: 127.0.0.1:4451"
# So HTTP port is likely 4451 (or that's logic port? Garnet has Main Port and maybe others. 4451 might be HTTP?)
# "Garnet 1.0.91 ... Listening on: 127.0.0.1:4451" 
# "Garnet 1.0.91 ... Listening on: 127.0.0.1:4061"
# Which one is which?
# Garnet usually implies Redis is the main port.
# If I look at `Pyrope.Benchmarks/Program.cs`, default port is 3278.
# I'll check `src/Pyrope.GarnetServer/Program.cs` to be sure.

# For now, placeholder ports. I will update after checking.

# 1. Baseline: IVF-Flat (Default nlist=100)
echo "--- Running Baseline (IVF nlist=100) ---"
dotnet run --project src/Pyrope.Benchmarks -- \
  --dataset synthetic --dim 128 --base-limit 10000 --query-limit 100 --topk 10 \
  --host "$SERVER_HOST" --port "$SERVER_PORT" \
  --http "$HTTP_URL" --admin-api-key admin --api-key tenant1 \
  --tenant t1 --index idx_baseline \
  --algorithm IVF_FLAT --params "nlist=100" \
  --build-index \
  > "$OUT_DIR/baseline.txt"
echo "Baseline Done. Results in $OUT_DIR/baseline.txt"

# 2. P10-15: IVF-Flat (nlist=1024) - Scaled for synthetic? 10000 points. nlist 1024 is high (10 pts/cluster).
# Maybe use smaller nlist for small synthetic, or nlist=50 vs nlist=5.
# If I use 10,000 vectors:
# P10-15 target is "large datasets".
# I'll use nlist=400 (25 pts/cluster) vs nlist=100 (100 pts/cluster) to show diff.
echo "--- Running P10-15 (IVF nlist=400) ---"
dotnet run --project src/Pyrope.Benchmarks -- \
  --dataset synthetic --dim 128 --base-limit 10000 --query-limit 100 --topk 10 \
  --host "$SERVER_HOST" --port "$SERVER_PORT" \
  --http "$HTTP_URL" --admin-api-key admin --api-key tenant1 \
  --tenant t1 --index idx_ivf_tuned \
  --algorithm IVF_FLAT --params "nlist=400" \
  --build-index \
  > "$OUT_DIR/p10_15_ivf_tuned.txt"
echo "P10-15 Done. Results in $OUT_DIR/p10_15_ivf_tuned.txt"

# 3. P10-16: HNSW (M=16, ef=200)
echo "--- Running P10-16 (HNSW) ---"
dotnet run --project src/Pyrope.Benchmarks -- \
  --dataset synthetic --dim 128 --base-limit 10000 --query-limit 100 --topk 10 \
  --host "$SERVER_HOST" --port "$SERVER_PORT" \
  --http "$HTTP_URL" --admin-api-key admin --api-key tenant1 \
  --tenant t1 --index idx_hnsw \
  --algorithm HNSW --params "m=16,ef_construction=200,ef_search=10" \
  --build-index \
  > "$OUT_DIR/p10_16_hnsw.txt"
echo "P10-16 Done. Results in $OUT_DIR/p10_16_hnsw.txt"

echo "Verification Complete."
