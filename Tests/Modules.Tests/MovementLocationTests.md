# MovementLocationTests

Unit tests for `Modules/Movement/MovementLocation`.

- `Offset` at yaw 0 maps forward to +x and right to -y; at yaw 90 forward
  to +y and right to +x; at yaw -135 forward points down both axes.
- `Offset` keeps the angle and replaces the name only when one is given.
- `LocalOf` undoes `Offset` at yaw 0, 90 and -135.
