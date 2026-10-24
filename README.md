# ShareView

A Unity VR research platform for studying motion sickness mitigation in visual sharing, where an observer watches a task from an operator's first-person view. Since the shared viewpoint is decoupled from the observer's own movement, it can cause VR motion sickness; this project implements motion-compensated points — sparse peripheral dots driven by the difference between the operator's and observer's head motion — as a mitigation method.

## Environment

- Unity 6000.3.10f1
- Main dependencies: Mirror, Meta XR SDK, XR Interaction Toolkit, URP, Newtonsoft Json
- Target platforms: Meta Quest (Android) and Windows Standalone

## Features

- Shared viewpoint rendering with synchronized main / sub / FOV cameras
- Runtime-switchable mitigation methods: motion-compensated points, FOV restriction, and mask overlays
- Networked operator–observer mode via Mirror (host/client, LAN discovery)
- Recording and fixed-rate local replay of operator motion for repeatable experiment sessions
- Per-frame observer pose logging for offline analysis

## Setup

1. Install [Git LFS](https://git-lfs.com) and run `git lfs install` before cloning. Textures, models, audio and other binaries are stored in LFS; if the repository was cloned without it, run `git lfs pull`.
2. Install Unity 6000.3.10f1 through Unity Hub. Add Android Build Support to build for Meta Quest.
3. Open the project folder in Unity. Packages, including the Meta XR SDK, resolve from the Unity registry on first open.
4. Copy the experiment recordings into the project. They are not in the repository; see [Data not in the repository](#data-not-in-the-repository).
5. Optional, for agent-driven checks against the open Editor: install the Unity CLI and sign in, as described in [docs/unity-cli.md](docs/unity-cli.md) (in Chinese). The Unity Pipeline package it connects through is already listed in `Packages/manifest.json`.

## Run

Open `Assets/MirrorExamplesVR/Scenes/SceneVR-UnityDemo/SceneVR-UnityDemo.unity`, the only scene in the build, and enter Play mode. For PC VR, connect a Quest headset through Quest Link first.

- Local replay (one machine, no network): press Local Replay in the HUD, or A on the right controller (the A key on a keyboard). Then press B (the B key) to play the selected recording. Recordings are read from `Assets/StreamingAssets/RecordData/`.
- Networked: press Host on one instance and Client on another on the same LAN. The client connects to the first host it discovers. Auto searches for about three seconds and starts as Host if it finds none.

## Verify

- After the project opens, the Console shows no compile errors.
- In local replay, the recording plays to the end, the Console logs `LocalReplay: observer log saved`, and the log files appear in `ObserverLogs/` at the project root. If the recordings are missing, the Console logs `LocalReplay: no replay data loaded.` instead.

## Data not in the repository

These folders are excluded by `.gitignore`. They cannot be regenerated, so back them up and copy them separately when moving the project to another machine.

- `Assets/RecordData/`: operator recordings. The recorder writes `MainPosData.json`, `MainRotData.json`, `BoxPosData.json` and `BoxRotData.json` here; earlier recordings are kept in numbered subfolders. In networked mode the client also writes `PaPosData.json` and `PaRotData.json` here.
- `Assets/StreamingAssets/RecordData/`: the four recording files read by local replay and bundled into builds.
- `ObserverLogs/`: observer logs written at the end of each local replay, in the project root when running in the Editor and next to the executable in Windows builds.

## Documentation

- [AGENTS.md](AGENTS.md): working rules for contributors and coding agents.
- [docs/architecture.md](docs/architecture.md): project structure, key scripts, run modes and data paths (in Chinese).
- [docs/unity-cli.md](docs/unity-cli.md): connecting to and driving the open Editor through the Unity CLI (in Chinese).
- [PROGRESS.md](PROGRESS.md) and [DECISIONS.md](DECISIONS.md): current status and design decisions (in Chinese).

## License

All rights reserved.
