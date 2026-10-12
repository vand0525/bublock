# EmptySignLayoutTests

Unit tests for `RiftRoulette/Boards/EmptySignLayout`.

- The camera spot is exactly the captured spectator start (`setpos_exact`
  / `setang_exact`: origin, pitch -0.59375, yaw 179.3125).
- The sign's center is `Distance` ahead along the view yaw, `Shift` along
  the viewer's right, and `Height` plus the slight upward pitch up.
- Both boards face the camera (`(0, yaw - 90, 90)`).
- The viewer's right when looking down -x is +y.
- Both lines are centered on the view: the anchor plus half the
  estimated width along the viewer's right lands on the center.
- The title's bottom is `Gap` above the center and the join line's top
  `Gap` below it.
- A line's offset (right, up) moves only that line, by exactly that much.
- The lines carry the settings' text, size and color.
