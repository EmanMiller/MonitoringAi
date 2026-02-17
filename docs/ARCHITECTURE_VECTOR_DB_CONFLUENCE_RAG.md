# Architecture: Vector DB — Confluence Search for Common Q&A (RAG)

**Status:** Planning / Design  
**Card:** TASK_CARDS_CHAT_LOGGING_PII_VECTOR.md — CARD 3

---

## Vision

In Common Q&A, the AI searches within specific Confluence document(s) so answers are grounded in that content. Users ask questions → the system retrieves relevant chunks from Confluence → the LLM answers using that context (RAG).

---

## Data Flow

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   Confluence    │     │  Indexing        │     │   Vector DB     │
│   (REST API)    │────▶│  Pipeline        │────▶│   (pgvector)    │
│                 │     │  chunk → embed   │     │                 │
└─────────────────┘     └──────────────────┘     └────────┬────────┘
        │                                                      │
        │ Fetch page content                                    │ nearest-neighbor
        │ (existing ConfluenceService)                          │ search
        │                                                      ▼
┌───────────────────────────────────────────────────────────────────────────────┐
│                        Search Flow (Common Q&A)                                │
│                                                                               │
│   User query  ──▶  embed query  ──▶  search vector DB  ──▶  top-k chunks      │
│                                                                    │          │
│                                                                    ▼          │
│   LLM (Gemini)  ◀──  prompt + retrieved context  ◀──  chunks as context       │
│        │                                                                      │
│        ▼                                                                      │
│   Answer grounded in Confluence content                                       │
└───────────────────────────────────────────────────────────────────────────────┘
```

**Steps:**
1. **Indexing (periodic):** Fetch Confluence page(s) → chunk text → embed chunks (Gemini Embedding API or similar) → store in vector DB.
2. **Search (per query):** User asks in Common Q&A → embed query → nearest-neighbor search → retrieve top-k chunks.
3. **Answer:** Pass chunks + user query to Gemini with a prompt instructing it to cite/use the context.

---

## Chosen Vector DB: pgvector

**Recommendation:** **pgvector** (PostgreSQL extension)

| Criterion | pgvector | Pinecone | Weaviate | Chroma |
|-----------|----------|----------|----------|--------|
| Same DB as app | ✅ Yes | ❌ New service | ❌ New service | ❌ New service |
| Ops overhead | Low | Medium | Medium | Low |
| Scale for use case | ✅ Adequate | Overkill | Overkill | Adequate |
| EF Core / migrations | Supported (Npgsql) | N/A | N/A | N/A |

**Rationale:**
- App already uses PostgreSQL (ConnectionStrings:Postgres); pgvector keeps everything in one database.
- No additional services or infrastructure.
- Suitable for “single Confluence doc or small set” scale.
- HNSW/IVFFlat indexes provide good recall for nearest-neighbor search.
- EF Core 9+ and Npgsql support pgvector; migrations can add the extension and vector column.

---

## Confluence Page(s) to Index

| Page | Source | Purpose |
|------|--------|---------|
| **Dashboard tracking page** | `Confluence:PageId` (appsettings) | Primary target. This is the doc the app creates/updates (dashboard tracking table). Answers about dashboards should be grounded here. |
| **Knowledge base (optional)** | New config: `Confluence:KnowledgeBasePageIds` | Additional pages for product/API docs, FAQs, or runbooks. Can be added in a later phase. |

**Initial scope:** Index the page identified by `Confluence:PageId`. Expand to multiple pages once the pipeline is stable.

---

## Schema (pgvector)

**Table:** `ConfluenceChunk` (or `VectorDocumentChunk`)

| Column | Type | Description |
|--------|------|-------------|
| `Id` | uuid (PK) | Unique chunk ID |
| `ContentChunk` | text | Chunk text |
| `Embedding` | vector(768) or vector(1536) | Embedding (dimension depends on model) |
| `SourcePageId` | varchar, indexed | Confluence page ID |
| `Metadata` | jsonb (optional) | section title, chunk index, `updated_at`, etc. |
| `CreatedAt` | timestamptz | For sync/refresh |

**Extension:** `CREATE EXTENSION IF NOT EXISTS vector;`

**Index:** HNSW (or IVFFlat) on `Embedding` for nearest-neighbor, e.g.:
```sql
ORDER BY embedding <-> query_embedding LIMIT k
```

---

## Chunking Strategy

- **Option A:** By section (Confluence headings) — preserves structure, variable chunk size.
- **Option B:** Fixed token size (e.g., 512 tokens with overlap) — uniform, good for embeddings.
- **Recommended:** Hybrid — split by section first, then sub-split large sections into fixed-size chunks with overlap.

---

## Follow-Up Implementation Cards

Detailed task cards live in **`task-cards/TASK_CARDS_CHAT_LOGGING_PII_VECTOR.md`** (CARD 3a–3d). Summary:

| Card | Owner | Scope |
|------|-------|-------|
| **3a: pgvector migration** | Marcus | Add pgvector extension, `ConfluenceChunk` table, vector column, HNSW/IVFFlat index. EF migration with raw SQL for vector type if needed. |
| **3b: Indexing pipeline** | Gary | Periodic job: fetch Confluence page(s) via REST API → chunk → embed (Gemini) → upsert into `ConfluenceChunk`. Trigger: scheduler or manual. |
| **3c: Search API** | Gary | `POST /api/Confluence/search` or `/api/CommonQA/search`: accepts query → embeds → searches vector DB → returns top-k chunks. |
| **3d: Common Q&A RAG** | George + Gary | Backend: search → chunks → Gemini with context. George: prompt design so LLM cites context. Frontend: call new endpoint, show answer/sources. |

---

## Dependencies

- **Embedding model:** Gemini Embedding API (e.g., `text-embedding-004`) or alternative (OpenAI, Cohere) if preferred.
- **PostgreSQL:** Must use Postgres (not SQLite) for pgvector. Local dev can use SQLite; vector features require Postgres.
- **Confluence:** `Confluence:ApiUrl`, `Username`, `ApiToken`, `PageId` (existing config).

---

## Open Questions

1. **Embedding model:** Gemini vs OpenAI vs Cohere — cost, rate limits, dimension.
2. **Indexing frequency:** On-demand vs scheduled (e.g., hourly/daily).
3. **Multiple pages:** When to add `Confluence:KnowledgeBasePageIds` and how to scope search (e.g., by page).
