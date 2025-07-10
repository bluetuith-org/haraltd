# Changelog

# v0.0.4 (10th July 2025)
This release brings the following changes:

- Improved Object Push support
- Optimized RPC
- New client-based discovery
- Authentication agents for pairing and object push (the pairing agent is currently a no-op on Windows)

And much more.

The binary size is reduced (from 34MB to 13MB) via NativeAOT, and no longer requires the .NET Runtime
to be installed. It will now run natively.

# v0.0.3 (7th March 2025)

## Changes
With this release, the application will require administrator privileges
to function (except for 'server' commands, which will automatically
elevate privileges).

New:
- RPC server mode, with a system tray icon controller and start/stop commands.
- System watcher which watches for changes from the adapter and devices (including device battery status)
- Automatic connection support for Bluetooth Classic Audio profiles
- Set discoverable state of the adapter

Improved:
- Adapter/device information retrieval
- Pairing and device discovery
- Object push

Applies various bug fixes as well.

# v0.0.2 (19th Oct 2024)

## Changes
- Minor bugfix

# v0.0.1 (19th Oct 2024)

## Changes
- Initial release
