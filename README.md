# Pyrope: AI-Controlled Vector Database

**Cut AI search costs by 50% and speed up responses by 3x.**

Pyrope is an "AI Cache Controlled" Vector Database designed to optimize both latency (P99) and compute costs (GPU/CPU seconds) for AI applications. It uniquely combines a high-performance frontend, an industry-standard ANN engine, and an intelligent AI controller to manage caching and query routing dynamically.

## 🚀 Key Features

*   **AI-Driven Caching**: A 2-stage architecture (Hot/Warm paths) that intelligently decides what to cache (Admission), how long (Variable TTL), and when to evict based on predicted future utility and cost.
*   **Cost-Aware Semantic Cache**: Goes beyond exact matches. Pyrope uses clustering and quantization to answer "similar" queries from cache, trading off a specific error margin for significant cost savings (30-50% reduction in FAISS compute).
*   **Cost-Aware Query Routing**: Automatically adjusts search parameters (e.g., `nprobe`, `efSearch`) based on predicted query cost and tenant quotas, ensuring stability under load.
*   **Query Prediction & Prefetching**: Learns from user session history to pre-calculate results for potential next queries, drastically reducing latency for interactive RAG applications.
*   **SLO Guardrails**: Includes fail-safes that automatically downgrade precision or fallback to rule-based caching if P99 latency spikes, ensuring service reliability.

## 🏗 Architecture

Pyrope uses a robust, layered architecture:

1.  **Front/Cache Layer (Garnet)**:
    *   Handles RESP-compatible commands.
    *   Manages Result, Candidate, and Meta caches.
    *   Executes lightweight "Hot Path" policy decisions (< 0.1ms).
2.  **ANN Engine (FAISS)**:
    *   Performs core vector indexing and searching.
    *   Supports dynamic "Delta Indexing" (Head + Tail strategy) for real-time updates.
3.  **AI Cache Controller**:
    *   **Warm Path (Sidecar)**: Runs complex inference (Python/ONNX) to update caching policies and scoring models asynchronously (10-50ms).
    *   Learns and evolves caching strategies continuously based on query logs.

## ⚙️ Configuration

Pyrope uses standard .NET configuration (appsettings/environment variables). Sidecar settings:

| Setting | Default | Description |
| --- | --- | --- |
| `Sidecar:Endpoint` | (unset) | gRPC endpoint for the AI sidecar (also supports `PYROPE_SIDECAR_ENDPOINT`). |
| `Sidecar:MetricsIntervalSeconds` | 10 | Interval in seconds between metrics reports to the sidecar. |
| `Sidecar:WarmPathTimeoutMs` | 50 | Timeout in milliseconds for warm-path responses before falling back to cached rules and incrementing `ai_fallback_total`. |

## 🎯 Use Cases

*   **RAG (Retrieval-Augmented Generation)**: Stabilize P99 latency and reduce the cost of repetitive semantic queries.
*   **Search Infrastructure**: Simplify ANN tuning and operation complexity for ML engineers.
*   **High-Traffic AI Services**: Manage "noisy neighbor" problems with multi-tenant QoS and strict quotas.
*   **FinOps**: Directly control and reduce the unit cost of vector search requests.

## 🛠 Usage (Conceptual)

Pyrope speaks the **RESP** protocol (Redis serialization), making it compatible with many existing clients for basic commands, while offering extended commands for vector operations.

### Vector Search
```bash
# Search for top 10 similar vectors
VEC.SEARCH my_app main_idx TOPK 10 VECTOR \x00\x01...
```

### Adding Vectors
```bash
# Add a new vector with metadata
VEC.ADD my_app main_idx "doc1" VECTOR \x00\x01... META {"category":"news"}
```

## 📈 Benchmarking (P1-6)

Pyrope includes a simple benchmarking tool to load common datasets and measure baseline search latency/QPS.

### Start server (RESP)

**重要**: ベンチマークでHTTP API経由のテナント自動登録を使用する場合は、`PYROPE_ADMIN_API_KEY`環境変数を設定してサーバーを起動してください。

```bash
# Admin API Keyを設定してサーバーを起動
PYROPE_ADMIN_API_KEY=your_admin_key dotnet run --project src/Pyrope.GarnetServer -- --port 3278 --bind 127.0.0.1
```

### Run benchmark (Synthetic data - クイックテスト)

合成データを使用した簡単なテスト（データセットのダウンロード不要）:

```bash
./scripts/bench_vectors.sh \
  --dataset synthetic \
  --dim 128 \
  --base-limit 1000 \
  --query-limit 100 \
  --topk 10 \
  --concurrency 4 \
  --api-key your_tenant_key \
  --http http://localhost:5000 \
  --admin-api-key your_admin_key
```

### Run benchmark (SIFT1M fvecs)

Prepare a directory containing `sift_base.fvecs` and `sift_query.fvecs`, then run:

```bash
./scripts/bench_vectors.sh \
  --dataset sift \
  --sift-dir ./datasets/sift1m \
  --base-limit 100000 \
  --query-limit 1000 \
  --topk 10 \
  --concurrency 16 \
  --warmup 100 \
  --api-key your_tenant_key \
  --http http://localhost:5000 \
  --admin-api-key your_admin_key
```

### Run benchmark (GloVe txt)

```bash
./scripts/bench_vectors.sh \
  --dataset glove \
  --glove-path ./datasets/glove/glove.6B.100d.txt \
  --dim 100 \
  --base-limit 200000 \
  --query-limit 2000 \
  --api-key your_tenant_key \
  --http http://localhost:5000 \
  --admin-api-key your_admin_key
```

### Benchmark Options

| オプション | 説明 |
| --- | --- |
| `--api-key` | (必須) テナントAPIキー。VEC.*コマンドの認証に使用 |
| `--http` | HTTP APIのベースURL (例: `http://localhost:5000`)。指定するとテナントを自動作成 |
| `--admin-api-key` | `--http`使用時に必須。Admin APIキー |
| `--dataset` | `synthetic`, `sift`, `glove` のいずれか |
| `--dim` | ベクトル次元数 (synthetic/glove で必須) |
| `--base-limit` | ロードするベースベクトル数 |
| `--query-limit` | 実行するクエリ数 |
| `--topk` | 検索で返す上位K件 |
| `--concurrency` | 並列ワーカー数 |

## 📊 Comparison

| Feature | Pyrope | Pinecone | Milvus | Weaviate |
| :--- | :--- | :--- | :--- | :--- |
| **AI Cache Control** | ✅ Unique | ❌ | ❌ | ❌ |
| **Semantic Cache** | ✅ Cost-Aware | ❌ | ❌ | ❌ |
| **SLO Guardrails** | ✅ Pro | ❌ | ❌ | 部分 |
| **Query Prediction** | ✅ | ❌ | ❌ | ❌ |

## 📄 License

[TBD]
