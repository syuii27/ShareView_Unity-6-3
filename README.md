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

## License

All rights reserved.
