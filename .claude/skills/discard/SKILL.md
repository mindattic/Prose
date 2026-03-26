---
name: discard
description: Discard all uncommitted changes, reverting to the last commit. Usage /discard
---

When invoked:

1. Run `git status` and show the user what will be discarded
2. Ask the user to confirm: "This will discard all uncommitted changes. Continue? (y/n)"
3. **Only if confirmed**, run:
   - `git checkout -- .` to revert tracked file changes
   - `git clean -fd` to remove untracked files and directories
4. Run `git status` to confirm clean state
5. Print confirmation message
