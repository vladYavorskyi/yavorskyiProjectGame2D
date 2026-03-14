# 2D Tower Defense (Unity, WebGL)

MVP Tower Defense project:
- Defender (player) builds towers in `Preparation`.
- Attacker (AI) generates enemy waves by budget.
- Round phases: `Menu -> Preparation -> Battle -> RoundEnd -> GameOver`.
- Target platform: WebGL (browser).

## Requirements

- Unity `6.3 LTS (6000.3.10f1)` or compatible Unity 6 LTS.
- Linux/Windows/macOS with Unity Editor.
- For local WebGL run: `python3`.

Optional (audio import troubleshooting):
- `ffmpeg` installed on system:
  - Ubuntu/Debian: `sudo apt install -y ffmpeg`

## Open Project

1. Open **Unity Hub**.
2. Add project folder: `/home/vladyslav/yavorskyiProjectGame2D` (or your cloned path).
3. Open scene:
   - `Assets/Scenes/MainScene.unity`

## Run In Editor

1. Press `Play`.
2. In menu:
   - `Start` to begin.
   - `Sound On/Off` to mute/unmute.
   - `Exit` to stop in Editor / leave page in WebGL.
3. During `Preparation`:
   - choose/drag towers from build menu and place on valid cells.
4. During `Battle`:
   - building is disabled, towers auto-attack.

## Build WebGL

1. Open `Build Profiles` (or `Build Settings`).
2. Select platform `Web` and make it Active.
3. Ensure scene list contains `MainScene`.
4. Build to a clean folder, e.g.:
   - `WebGLRelease`

## Run WebGL Locally

```bash
cd /path/to/WebGLRelease
python3 -m http.server 8082
```

Open:
- `http://localhost:8082/index.html`

## Stress Test (Methodology Acceptance)

Use `EnemySpawner` context menu:
- `Apply Extreme Spawn Test Preset`

This preset is tuned for stress:
- high budget,
- high wave limit,
- very low spawn interval,
- extended preparation time,
- boosted start gold for testing.

Expected acceptance scenario:
- ~20 towers on map,
- at least 50 enemies in active wave,
- no long freezes,
- rounds finish correctly.

Detailed checklist:
- `TESTING_PROTOCOL.md`

## Controls / Gameplay Notes

- Build allowed only in `Preparation`.
- Towers cannot be placed on path cells.
- Enemies follow fixed waypoints.
- Targeting rule: enemy with highest path progress in range.
- Enemy HP bars are shown and update on damage.

## Audio Mapping (Current)

- Menu music, round-start cue, defeat cue.
- Tower shoot SFX per tower type.
- Enemy hit and death SFX per enemy type.
- Base hit animation + optional base hit SFX.

## Common Issues

1. Audio clips fail to import:
   - Install `ffmpeg`.
   - Reimport `Assets/Audio`.
   - For problematic clips set Audio Compression to `PCM` or `Vorbis`.

2. WebGL fails with compressed `.br` assets on simple local server:
   - Build with a compatible compression setting for local testing, or
   - Use proper web server config for Brotli headers.

3. Build menu/tooltip UI flicker:
   - Ensure tooltip UI has `Raycast Target = false` on panel/text.

