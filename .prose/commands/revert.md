# /revert — revert a commit

Usage: `/revert <commit-hash>`

When invoked with a commit hash:

1. Run `git log --oneline -5` to confirm the commit exists.
2. Run `git revert <commit-hash>` to create a revert commit.
3. Push to remote.
4. Print confirmation with the new revert commit hash.
