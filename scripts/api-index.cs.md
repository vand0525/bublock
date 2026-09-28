# api-index.cs

.NET 10 file-based app that writes `knowledge/generated/deadworks-api.md`:
an index of everything the Deadworks API exposes, so modders can grep for a
hook, event, enum value or method before assuming it exists (`.rules` §8).

## Usage

```bash
dotnet run scripts/api-index.cs [lib dir] [output .md]
```

Defaults: `<repo>/lib` and `knowledge/generated/deadworks-api.md`.
`scripts/knowledge.sh` passes the right lib folder for either layout.

## Behavior

- Loads `DeadworksManaged.Api.dll` by reflection (`Assembly.LoadFrom`;
  `Google.Protobuf.dll` resolved from the same folder). No game code runs.
- Reads summaries from `DeadworksManaged.Api.xml` (shipped in Deadworks
  releases from `scripts/fetch-deadworks.sh`); missing XML just leaves
  summaries empty.
- Sections: plugin hooks (`IDeadworksPlugin`), game events (`GameEvent`
  subclasses and their fields), every enum with numeric values, every
  public type with properties, fields and methods, and the protobuf net
  message names.
- Front matter records the Deadworks version from `lib/.version`.

## Invariants

- Output is generated; never hand-edit it. Behavior notes belong in
  `knowledge/effects-catalog.md` or `reference/resources.md`.
