# AdminCommand

Shared gate and reply helper for admin `[Command]` wrappers.

## Behavior

- `Authorize(caller, log, command)`
  - `caller == null` (server console): trusted; logs
    `Admin command {Command} from server console` at Information.
  - Caller not in `AdminAuth`: logs `Unauthorized command attempted
    {Command}` as a Warning with the caller's `PlayerRef` (also copied to the
    master log), then throws `CommandException(RejectedMessage)`, which
    Deadworks replies to the caller and which stops the command.
  - Authorized caller: logs `Admin command {Command}` at Information with the
    caller's `PlayerRef` and returns.
- `Reply(caller, message)`: command result to the caller's console
  (`PrintToConsole`), or to the server console (`Console.WriteLine`) when
  `caller` is null. This is the allowed "command result" console output in
  `.rules` §7, not diagnostics.

## Inputs / outputs

- `log`: the feature logger of the calling plugin class (so accepted and
  rejected calls land in that feature's log file).
- `command`: the command name as registered (e.g. `wt_list`).

## Invariants

- Call `Authorize` first in every admin command, before any side effect.
- Handlers must take `CCitadelPlayerController?` (nullable) so server-console
  calls reach `Authorize` as `null`.
- DevTools still uses its own `Authenticate` (silent rejection) until the
  Stage 12 pass.
