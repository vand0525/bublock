# EmptySignLayoutTests

Unit tests for `RiftRoulette/Boards/EmptySignLayout`.

- The camera spot is exactly the captured spectator start (`setpos_exact`
  / `setang_exact`: origin, pitch -0.59375, yaw 179.3125).
- The sign's center is `Distance` (300) ahead along the view yaw (toward
  -x), `SideShift` along the viewer's right, and raised by the slight
  upward pitch.
- Both boards face the camera (`(0, yaw - 90, 90)`).
- The viewer's right when looking down -x is +y.
- Both lines are centered on the view: the anchor plus half the
  estimated width along the viewer's right lands on the center.
- The title sits above the center and above the join line's top; the join
  line sits below the center.
- The lines read `RIFT ROULETTE` and `Join with connect 66.94.102.245:27015`.
