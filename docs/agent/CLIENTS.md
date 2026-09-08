# Client setup

All clients use the same Prose MCP stdio server and the same protocol document. The client only
needs to provide a way to launch this command from the repository root:

```text
dotnet run --project v3/Prose.Mcp/Prose.Mcp.csproj --no-build --configuration Release
```

Claude, Codex/OpenAI, Gemini, Kimi, and other MCP-capable hosts should register that command in
their native MCP configuration, then load `AGENTS.md`. Hosts without MCP use the CLI commands in
`docs/CLI_COMMANDS.md` or the loopback Hub endpoints documented in `docs/CONSUMER_ONBOARDING.md`.
No client-specific file may redefine universe rules, approval policy, or verification behavior.

For prompt-command hosts, bind `/do`, `/quicksave`, `/quickload`, and other aliases to
`tools/prose-agent.ps1` according to [`PROMPT_COMMANDS.md`](PROMPT_COMMANDS.md). Hosts that do
not support slash commands can invoke the same script directly.
