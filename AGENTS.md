# Project Guidelines

- This is a Unity 6 project using PurrNet and PurrDiction. Keep gameplay-affecting state deterministic and rollback-safe.
- Use simulation `delta` for gameplay timers. Do not use coroutines or `Time.time` for predicted gameplay.
- Read predicted state for simulation decisions and `viewState`/`verifiedState` for client presentation.
- Keep irreversible effects, UI, audio, scene changes, and network teardown out of speculative simulation.
- Only the server may initiate authoritative session changes such as `GameOverBroadcaster.EndGame()`.
- Use `ServiceLocator` for scene-wide gameplay services and unregister scene services on destruction.
- Prefer small changes that follow existing project patterns. Do not add compatibility layers without a concrete need.
- Use Unity CLI against the connected Editor for scene, prefab, and asset edits. Do not hand-edit Unity YAML while an Editor is reachable.
- Do not create prefab assets unless explicitly requested.
- Preserve existing user changes and never revert unrelated worktree changes.
- Run Unity compilation checks and `git diff --check` after code changes. Enter Play Mode only when explicitly requested or necessary.
- UI must work for host and remote clients; drive it from replicated view/verified state rather than host-only simulation events.
