---
name: git-commit-push
description: Stage changes, commit using this repo's tag convention (.claude/rules/git.md), and push. Use when the user asks to commit and push.
---

# git-commit-push

1. Read `.claude/rules/git.md` for the current tag list.
2. Check `git status`/`git diff`, stage relevant files.
3. Commit as `<Tag>: <summary>` with the matching tag.
4. `git push`.

Never force-push, amend, or skip hooks without explicit confirmation.
