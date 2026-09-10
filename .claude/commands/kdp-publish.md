---
description: Build the KDP manifest, work out which books are new-and-ready or stale on Amazon, confirm with the user, then drive KdpPublish to auto-publish/republish them.
argument-hint: "[--dry-run] [CODE1,CODE2,... to restrict to specific books]"
allowed-tools: Bash, PowerShell, Read, Grep, Glob, AskUserQuestion, Monitor
---

Canonical definition: **`.prose/commands/kdp-publish.md`**. Read it and follow it — this file is a
thin pointer only; the real, maintained runbook lives in `.prose/` so every client shares one copy.
