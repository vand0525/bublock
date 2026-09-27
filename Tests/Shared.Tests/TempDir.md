# TempDir

Test helper: creates a unique folder under the system temp directory
(`bublock-tests/<guid>`) and deletes it on `Dispose`.

- `Files()` — sorted file names in the folder.
- `Read(name)` — file contents, opened with `FileShare.ReadWrite | Delete`
  so it works while a log writer still has the file open (required on
  Windows).
