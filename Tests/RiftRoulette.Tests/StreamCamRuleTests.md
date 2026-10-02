# StreamCamRuleTests

Unit tests for `RiftRoulette/Lobby/StreamCamRule.ParkStep`.

- Outside fly cam: parks for a new side, stays until the repark interval,
  then reparks.
- In fly cam: never parks while the viewer moves the camera; parks for a
  new side even if the last one landed or was handled; a landed or handled
  camera stays; a park that did not land is resent once the interval has
  passed (or when no park was sent yet).
