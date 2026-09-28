# knowledge.sh

Rebuilds every generated part of the knowledge base in one go.

## Usage

```bash
./scripts/knowledge.sh
```

## Steps

1. `dotnet run scripts/api-index.cs` → `knowledge/generated/deadworks-api.md`
   (skipped with a note when no Deadworks API is in `lib/`; the committed
   index stays).
2. `scripts/knowledge-graph.py` → `knowledge/generated/graph.json`,
   `tree.md`, `indexes.md`.
3. `scripts/docs-site.py` → `site/index.html` (git-ignored).

## When to run

After adding or renaming files, commands or game dependencies, after
editing anything under `knowledge/`, and on patch day after updating
`lib/`. Commit the regenerated `knowledge/generated/` files with the change.

## Side effects

Writes only `knowledge/generated/` and `site/`. No network except the
first `dotnet run` (restores the .NET SDK's file-based app support), no
server access.
