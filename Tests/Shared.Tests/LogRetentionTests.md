# LogRetentionTests

Checks `LogRetention.DeleteOlderThan` with a fixed date (2026-09-26):

- Deletes dated `.log` files older than 7 days (including size-rolled
  `.N.log` files).
- Keeps the file exactly 7 days old, undated `.log` files, and non-`.log`
  files.
- A missing folder returns 0.
