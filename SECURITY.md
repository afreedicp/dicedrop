# Security Policy

## Supported versions

Only the [latest release](https://github.com/i-oms/dicedrop/releases/latest) gets fixes. Peers must run the same protocol version anyway, so please update before reporting.

## Reporting a vulnerability

**Please don't open a public issue.** Report privately through GitHub: open the repository's **Security** tab and click **Report a vulnerability**.

Examples of what to report:

- Defeating the screen-capture protection with ordinary user rights
- Predicting, steering or forging fair-dice rolls, or making peers accept an illegal move
- Crashing, freezing or taking over other peers through crafted network messages
- Anything that makes DiceDrop read or write files it shouldn't

Include the DiceDrop version, your Windows version (`winver`), and steps to reproduce. You should hear back within a week.

## Out of scope

These are documented limits rather than vulnerabilities (see [docs/TECHNICAL.md](docs/TECHNICAL.md#5-screen-capture-protection)):

- Capturing the screen with a phone camera, an HDMI capture card, or from a VM host
- Attacks that need administrator rights, a kernel driver or code injection on the victim's PC
- Peer traffic not being encrypted: DiceDrop is designed for trusted local networks
