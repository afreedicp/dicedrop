<p align="center">
  <img src="assets/logo.png" width="120" alt="DiceDrop Logo" />
</p>

<h1 align="center">DiceDrop</h1>

<p align="center">
  <strong>One EXE. No server. No account. No cheating.</strong><br>
  Serverless, peer-to-peer Ludo for 2 to 8 players on Windows.
</p>

<p align="center">
  <a href="https://github.com/i-oms/dicedrop/releases/latest"><img src="https://img.shields.io/github/v/release/i-oms/dicedrop?style=flat-square&label=Download&color=blue" /></a>
  <a href="https://github.com/i-oms/dicedrop/actions"><img src="https://img.shields.io/github/actions/workflow/status/i-oms/dicedrop/release.yml?style=flat-square&label=Build" /></a>
  <img src="https://img.shields.io/github/license/i-oms/dicedrop?style=flat-square" />
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square" />
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue?style=flat-square" />
  <img src="https://img.shields.io/github/downloads/i-oms/dicedrop/total?style=flat-square" />
</p>

<p align="center">
  <a href="https://github.com/i-oms/dicedrop/releases/latest">⬇️ Download DiceDrop.exe</a> •
  <a href="#-demo">Demo</a> •
  <a href="#-how-it-works">How it works</a> •
  <a href="docs/TECHNICAL.md">Technical Docs</a>
</p>

---

### 🎲 What is this?

**DiceDrop** is a portable Ludo that works like SHAREit works — open the EXE on every PC in the same Wi-Fi/LAN and you instantly share a lobby. No central server, no login, no install, no data leaves your network.

> `DiceDrop.exe` — double-click and play. Everything is P2P encrypted, chats are never saved, window can't be screen-captured.

[Board Preview](assets/screenshot-board.png)

### ✨ Features

- **2 to 8 Players** — Classic cross board for 2-4, star board for 5-8 players (built dynamically)
- **Zero Server** — Full mesh P2P over TCP, discovery via UDP broadcast. Host is just the lobby keeper.
- **Fair Dice, No Trusted Roller** — Each peer commits a hash chain anchor at game start. Dice = `SHA256(round || all_reveals) mod 6 + 1`. No one can steer or predict it.
- **Spectator Mode** — Not playing? Watch live. Leave a game and keep watching. Late joiners replay full history.
- **Built-in Messenger** — Lobby group chat + private chats + Nudge (shake + sound)
- **Anti-Cheat by Design** — `WDA_EXCLUDEFROMCAPTURE` — blocks PrintScreen, Snipping Tool, OBS, Teams/Zoom/Discord screen share
- **Portable & Private** — One EXE ~59MB self-contained, settings in `%APPDATA%\DiceDrop\settings.json`, nothing in registry
- **Auto-Update Aware** — Checks GitHub releases on start + every 6h, shows banner, never auto-installs

### 🎬 Demo

> Replace these with your actual recordings (phone recording is fine since screen capture is blocked!)

| Lobby & Chat | Inviting & Rolling | 8-Player Star Board |
|---|---|---|
| [lobby](assets/demo-lobby.gif) | [invite](assets/demo-invite.gif) | [star](assets/screenshot-8p.png) |

### 📥 Quick Start

1. Download **[DiceDrop.exe from Latest Release](https://github.com/i-oms/dicedrop/releases/latest)**
2. Copy to every friend's PC (Windows 10 2004+ / 11)
3. Allow firewall when Windows asks (Private network)
4. Open — you auto-join the lobby. Pick name in ⚙️ Settings.

That's it. No port forwarding.

**Playing over Internet?** Use a virtual LAN that forwards broadcasts: **ZeroTier** or **Radmin VPN** works. Tailscale does NOT.

<details>
<summary><b>When PCs don't find each other? (Click for fix)</b></summary>

Press `F12` or `Network log` button. It shows in order:
- `env`: adapters + firewall rules (look for `PROBLEM`)
- `udp`: which discovery ports opened, who we heard
- `tcp`: connection fails (timeout = host firewall, refused = nothing listening)
- `lobby`: auto-lobby decision

Copy logs from both PCs and compare. 90% of cases = Windows Firewall BLOCK rule created when prompt was cancelled. Delete it and re-allow.

</details>

### 🕹️ How to Play

1.  **Lobby auto-forms** — first opener is host, if two open at same time, smaller merges into bigger in ~1s. If host leaves, longest-standing member takes over.
2.  **Invite** — Anyone clicks a game on right -> Invite players, or Invite to Ludo in private chat.
3.  **Roll** — Click dice or press Space, then click bouncing pawn. 6 brings token out, 6 / capture / home = extra roll, 3x6 = forfeit, ★ and start = safe, exact roll to finish.
4.  **Watch / Leave** — Watch live, leave anytime, initiator can Stop for all.

### 🛠️ Build from Source

```bash
# Windows, any shell
build.cmd                # tests + dist\DiceDrop.exe (self-contained)

# Dev
dotnet build DiceDrop.slnx
dotnet test DiceDrop.slnx
```

Release version comes from git tag `vX.Y.Z` or `DICEDROP_VERSION` env. Untagged build = `0.0.0` and no update check.

### 📚 Documentation

Deep dives are in **[docs/TECHNICAL.md](docs/TECHNICAL.md)**:
- Architecture (Clean Architecture, project map)
- How peers stay in sync (mesh, ordering, desync detection)
- Fair dice protocol in detail
- Ports & discovery protocol
- Screen-capture protection internals
- Limitations & Roadmap

### 🤝 Contributing

Contributions welcome! Bug reports, fixes, tests, docs.

1. Fork -> branch `feat/my-thing` -> `dotnet test`
2. Open PR

See [CONTRIBUTING.md](CONTRIBUTING.md) for full git commands.

Good first issues: turn timer, rule variants, reconnection, sounds.

### 📜 License

MIT — free for commercial use. Keep copyright notice. See [LICENSE](LICENSE).

---

<p align="center">Built with C# + WPF + a lot of UDP beacons. If you like it, ⭐ star the repo — it helps others find it.</p>
