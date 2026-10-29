## What

<!-- What does this change do? -->

## Why

<!-- The problem it solves. Use "Closes #123" to link an issue. -->

## How I tested

<!-- e.g. "dotnet test: all passing, plus 2 new tests"; "3 players on 2 PCs, played to the finish".
     The game window is excluded from screen capture, so describe UI changes in words or attach phone photos. -->

## Checklist

- [ ] `dotnet build DiceDrop.slnx` succeeds with zero warnings
- [ ] `dotnet test DiceDrop.slnx` passes
- [ ] New Domain/Application logic has tests
- [ ] Tested in a real multiplayer game if networking, sync or UI changed
- [ ] Follows the [project rules](https://github.com/i-oms/dicedrop/blob/main/CONTRIBUTING.md#11-project-rules-you-must-follow) (architecture, N players, capture protection, determinism)
- [ ] Wire format unchanged, or `ProtocolInfo.Version` bumped and called out above
- [ ] README / docs/TECHNICAL.md updated if behaviour, ports, protocol or build steps changed
