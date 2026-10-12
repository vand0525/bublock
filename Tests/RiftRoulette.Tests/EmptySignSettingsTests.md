# EmptySignSettingsTests

Unit tests for `RiftRoulette/Boards/EmptySignSettings`.

- Defaults are the tuned sign (300 / 100 / 0, sizes 1.2 / 0.45, gap 8,
  both lines green 0 190 0, the title and join text) and valid.
- Distance below 50 or above 3000 is a problem.
- Shift, height, both sizes, gap, both colors and text length are range
  checked; the edges (±3000, gap 0) are valid.
- JSON round trips; missing fields keep their defaults; `null` and broken
  JSON throw.
- `SignColor.ToWorldText` is the same color, fully opaque.
