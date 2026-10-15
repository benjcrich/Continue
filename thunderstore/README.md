# Continue

Adds a **Continue** button to Valheim's main menu that resumes the last character in the last local world or multiplayer server you started.

## Features

- One-click resume for local worlds and joined servers (Steam, PlayFab/crossplay, dedicated)
- Small subtitle under the button with the character and world/server name
- Restores previous host / public / crossplay flags for local worlds (password is left empty)
- Never stores server passwords — Valheim's normal password prompt still appears
- Works from vanilla last-selected character/world prefs until you play a session with the mod
- Configurable button label in `BepInEx/config/minortom.Continue.cfg`

## Install

Install with r2modman / Thunderstore Mod Manager, or place `Continue.dll` in `BepInEx/plugins`.

Client-side only. No server install needed.

## Notes

- The button stays hidden until a session exists.
- If the saved character or world is missing, Continue shows a short message and stays on the menu.
