# Demo Prompts — Copy / Paste Ready

The exact user messages to type into the chat UI during each demo. Each block is one demo so you can grab the whole thing and not lose your place.

---

## Demo 1 — Pirate Skill (Stage 1)

**Setup:** `pirate-voice` skill is loaded but OFF. Open Skills page on screen.

**Action:** Toggle `pirate-voice` ON. Open chat. Paste:

```
Explain how HTTP cookies work in two short paragraphs.
```

**Expected:** Reply opens with "Arrr" / "Ahoy", uses nautical metaphors for cookies (treasure chests, ship logs), ends with "Yarrr!". Same factual content, different voice.

**Follow-up (optional, if time):**

```
Now translate that into plain English for a junior developer.
```

**Expected:** Still in pirate voice — proves the skill is system-prompt-level, not just a one-shot transformation.

**Reset for next demo:** Toggle `pirate-voice` OFF.

---

## Demo 2 — Awesome-Copilot Skill Import (Stage 1, manual walkthrough)

**Setup:** Skills page open. Awesome-Copilot import button visible.

1. **Click** "Import from awesome-copilot" → catalog opens.
2. **Filter / scroll to:** `security-auditor`.
3. **Preview pane** — point out: repo (`github/awesome-copilot`), pinned commit SHA, SHA-256 hash. Say: *"Pinned to a commit. No surprise updates."*
4. **Click** Install. File lands in `{StorageRoot}\skills\installed\security-auditor\SKILL.md`.
5. **Toggle** `security-auditor` ON.
6. **Open chat.** Paste:

```csharp
[HttpGet("/users")]
public IActionResult GetUsers(string filter)
{
    var sql = $"SELECT * FROM Users WHERE Name LIKE '%{filter}%'";
    return Ok(db.ExecuteQuery(sql));
}
```

(Just paste the snippet — no extra prose.)

**Expected:** Agent flags SQL injection on the `filter` param, rates `high` or `critical`, suggests parameterized query, quotes the offending line.

**Fallback if awesome-copilot is unreachable:** Drop `demos-resources/skills/security-auditor.md` directly into the runtime skills folder and reload. Same demo from step 5.

---

## Stage 2 — Memory Demo

**Setup:** A long conversation (≥ 20 messages) is already loaded — see `memory-seed-conversation.md`. Open the running chat.

**Trigger summarization. Paste:**

```
Summarize what we've discussed so far in 5 bullet points.
```

**Expected:** Agent returns a compact recap touching all major topics from the seed conversation. Then point at the dashboard / memory panel showing the token count compressed from ~4k down to a few hundred.

**Follow-up to prove memory persistence:**

```
What was the second framework I asked about?
```

**Expected:** Agent answers correctly even though the original mention is far back in history — proves the summarized memory still holds key facts.

---

## Optional Q&A Backstops

If audience questions stall, prompts to seed the discussion:

- *"Can a skill call a tool? Show me what happens if I ask `pirate-voice` to read a file."*
- *"What stops a malicious skill from exfiltrating data?"* (Lead-in to the awesome-copilot pinned-SHA story.)
- *"How does memory differ from RAG?"* (Lead-in to summarization vs. retrieval.)
