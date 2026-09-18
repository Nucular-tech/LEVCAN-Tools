# LEVCAN-Tools
Tools for LEVCAN (Light Electric Vehicle CAN).
Check releases page for latest Windows app.

![levcantool](https://github.com/Nucular-tech/LEVCAN-Tools/assets/1153192/4f864961-1681-4bc3-867a-f0425ca9780b)

## Features

- **LEVCAN Configurator** — Real-time device configuration, telemetry dashboard, parameter inspection, and firmware update manager for LEVCAN devices.
- **MEF Plugin Architecture** — Drop-in plugins loaded dynamically from `Plugins/` (e.g. Diagnostics, CAN Test, Debug Probe).
- **Embedded AI Debug Probe** — Built-in HTTP loopback interface (default port `8585`) allowing local AI agents (Pi agent, scripts, web tools) to query live bus status, telemetry, parameter trees, and trigger firmware updates.

## Debug Probe API

When LEVCAN Configurator is running with the Debug Probe enabled, access the embedded documentation at:
```text
http://127.0.0.1:8585/help
```

### Quick Endpoints
- `GET /api/status` — Probe and CAN adapter status, online nodes count.
- `GET /api/nodes` — Discovered devices on the CAN bus.
- `GET /api/telemetry` — Live telemetry snapshot (speed, currents, voltages, temperatures).
- `GET /api/nodes/{id}/data/{objectId}` — Query standard LEVCAN objects over CAN (e.g. `Temperature`, `Speed`, `DCSupply`).
- `GET /api/objects?{filter}` — Search the object catalog (also aliased to `/v1/models` and `/api/models`).
- `POST /api/nodes/{id}/force-update` — Trigger firmware force update for controller bootloader.

