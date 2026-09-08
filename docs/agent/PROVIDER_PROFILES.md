# Provider profiles

The engine must select a model by capability and task role, not by a vendor-specific global
switch. A profile has an id, role, provider family, model, context and output limits, structured
output support, prompt-cache support, price class, and an ordered list of compatible fallbacks.

The initial roles are `prose_generation`, `structured_extraction`, `reader_probe`, `review_judge`,
and `embedding`. A provider is eligible only when it advertises the role's required capabilities;
an Anthropic model id must never be sent to an OpenAI, Gemini, Kimi, or other incompatible hop.

Provider adapters implement the existing `ILlmService` contract. `LlmRouter` resolves a role
profile, skips unavailable providers, drops incompatible pinned model ids, records every hop in
`LlmCallHistories`, and returns the provider/model actually used. Adding an OpenAI-compatible
endpoint is configuration plus credential registration, not a new service call site.

Embeddings remain a separate capability. A profile change that changes vector dimensions requires
an explicit reindex plan and must not silently mix vectors from different models.
