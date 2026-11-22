#!/bin/sh
# PostToolUse hook for Kiro CLI agents: the tool call JSON arrives on stdin.
# Generated EF Core migrations are produced by `dotnet ef` (a shell command), so any
# direct file-tool write into Migrations/ is a hand edit — tell the agent to undo it.
if grep -Eq '"[a-z_]*path"[[:space:]]*:[[:space:]]*"[^"]*Migrations(/|\\\\)[^"/\\]+\.cs"'; then
  echo "A generated migration under Migrations/ was edited by hand. Revert this edit and create a new migration with 'dotnet ef migrations add <Name>' instead, unless the user explicitly asked for a manual change." 1>&2
  exit 1
fi
exit 0
