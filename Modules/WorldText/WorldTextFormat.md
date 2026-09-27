# WorldTextFormat

Pure text helpers for board commands. No game calls; unit tested in
`Tests/Modules.Tests`.

- `FromArgs(parts)`: joins command arguments with single spaces and turns the
  two-character sequence `\n` into a real line break, so admins can type
  multi-line boards (`/wt_create info "Line one\nLine two"`).
- `Preview(text, maxLength = 40)`: one-line preview for listings. Line breaks
  become ` / `; longer text is cut to `maxLength` characters plus `...`.
