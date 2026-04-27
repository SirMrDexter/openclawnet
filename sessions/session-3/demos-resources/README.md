# Session 3 — Demo Resources

Prep files for the Session 3 live demos. Everything the speaker needs to copy/paste, drop into a folder, or have on screen — collected in one place so the pre-session checklist is a 2-minute job, not a 10-minute scramble.

## What's here

| File | Used for | Checklist item it satisfies |
|------|----------|----------------------------|
| `skills/security-auditor.md` | **Demo 2** — Awesome-Copilot import walkthrough. The skill the speaker pretends to "discover" in the catalog. Drop into `{StorageRoot}\skills\installed\security-auditor\SKILL.md` if the live import flow is unavailable. | "Prepare a test skill file for live demo (e.g., `security-auditor.md`)" |
| `skills/pirate-voice.skill.md` | **Demo 1** — Pirate persona. Backup copy in case the runtime skills folder is missing it; identical to the canonical version under `code/01-SkillOnOff/skills/`. | "Navigate to Skills page — toggle one skill to confirm loading works" |
| `skills/dotnet-expert.md` | **Stage 1 explainer** — sample skill to display on screen when introducing the YAML + Markdown contract. | "Have sample skills ready to display: `skills/built-in/dotnet-expert.md`" |
| `demo-prompts.md` | **All demos** — the exact user messages the speaker types into chat during each demo. Copy/paste so you never have to invent a prompt under stage lights. | "Pre-run 20+ messages in a conversation to have summarization data ready" (see seed prompts) |
| `memory-seed-conversation.md` | **Stage 2 — Memory** — a 20-message conversation transcript. Run before the talk so the summarization demo has real history to compress. | "Pre-run 20+ messages in a conversation to have summarization data ready" |

## How to use during pre-session

1. **Skills folder check.** Verify `pirate-voice` is loaded (Skills page lists it). If not, copy `skills/pirate-voice.skill.md` into the runtime skills folder and reload.
2. **Open files in VS Code tabs.** Pin `skills/dotnet-expert.md` and `MemoryEndpoints.cs` so they're one click away during the explainer slides.
3. **Seed memory.** Open a fresh chat and paste the messages from `memory-seed-conversation.md` one by one (the last message asks the agent to summarize — that's your demo trigger).
4. **Stage demo prompts.** Have `demo-prompts.md` open in a side panel — copy the next prompt as you transition between demos.

## Naming convention

Skills under `skills/` use the `.skill.md` suffix when they are designed for the in-process file-skill loader (matches the runtime convention in `code/01-SkillOnOff/skills/` and `code/04-SkillPicker/skills/`). The `security-auditor.md` file uses the awesome-copilot directory layout (one folder per skill, file named `SKILL.md` after install) — the version stored here is the source artifact you'd see in the catalog before installing.
