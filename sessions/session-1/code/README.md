# Session 1 — Staged Demo Code

This folder contains the **incremental build stages** for Session 1 of OpenClawNet. Each stage is a self-contained .NET solution that can be opened and explored independently.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Ollama](https://ollama.com/) **or** [Foundry Local](https://github.com/microsoft/foundry) (for running a local LLM)

## Stages

### Stage 1 — Core Abstractions (`stage-1/`)

**Projects:** `OpenClawNet.Models.Abstractions`

Introduces the architecture foundation:

- `IModelClient` — the provider-agnostic interface for chat completions
- `ChatRequest` / `ChatResponse` — the request/response records
- `ChatMessage`, `ModelOptions`, `ToolDefinition` — supporting types
- `ServiceCollectionExtensions` — DI registration helpers

This stage is **library-only** (no runnable app). Open it to explore the abstraction layer that every provider implements.

### Stage 2 — Local LLM Providers + Storage (`stage-2/`)

**Projects:** Models.Abstractions, Models.Ollama, Models.FoundryLocal, Storage, ServiceDefaults

Adds concrete implementations on top of the abstractions:

- `OllamaModelClient` — streaming chat completions via Ollama's HTTP API
- `FoundryLocalModelClient` — alternative local provider using Foundry Local
- `ConversationStore` + EF Core entities — persist chat sessions and messages
- `ServiceDefaults` — shared .NET Aspire service configuration

This stage is still **library-only** but you can inspect how providers implement `IModelClient` and how EF Core entities map the domain.

### Stage 3 (Final) — Gateway + SignalR + Blazor UI (`stage-3-final/`)

**Projects:** All 8 Session 1 projects (Abstractions, Ollama, FoundryLocal, Storage, ServiceDefaults, Gateway, Web, AppHost)

The complete runnable application for Session 1:

- **Gateway** — ASP.NET Core API with `/api/chat` and `/api/sessions` endpoints, plus a SignalR `ChatHub` for real-time streaming
- **Web** — Blazor Server UI with chat interface, session management, and health dashboard
- **AppHost** — .NET Aspire orchestrator that wires everything together

#### Running Stage 3

```bash
cd docs/sessions/session-1/code/stage-3-final
dotnet run --project src/OpenClawNet.AppHost
```

The Aspire dashboard will open in your browser. From there you can access the Web UI and Gateway endpoints.

> **Note:** Make sure Ollama (or Foundry Local) is running before starting the app. The Gateway's provider is configurable — see `appsettings.json` or environment variables.

## Opening a Stage

Each stage folder contains an `OpenClawNet.slnx` solution file:

- **Visual Studio 2022 17.12+** — File → Open → Solution, select the `.slnx` file
- **VS Code** — Open the stage folder; the C# Dev Kit will detect the solution
- **CLI** — `dotnet build` from the stage folder
