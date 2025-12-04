# haraltd
A daemon and command-line tool to use Bluetooth Classic features.<br />
Part of the cross-platform work for the [bluetuith](https://github.com/darkhz/bluetuith) project.

This is alpha-grade software.

## Funding

This project is funded through [NGI Zero Core](https://nlnet.nl/core), a fund established by [NLnet](https://nlnet.nl) with financial support from the European Commission's [Next Generation Internet](https://ngi.eu) program. Learn more at the [NLnet project page](https://nlnet.nl/project/bluetuith).

[<img src="https://nlnet.nl/logo/banner.png" alt="NLnet foundation logo" width="20%" />](https://nlnet.nl)
[<img src="https://nlnet.nl/image/logos/NGI0_tag.svg" alt="NGI Zero Logo" width="20%" />](https://nlnet.nl/core)

# Features
The available features per-platform are [here](https://github.com/bluetuith-org/bluetooth-classic?tab=readme-ov-file#feature-matrix).

# Requirements
## Windows
- Windows 10 19041 or later.
- Administrator access for certain Registry-related APIs.

> [!Note]
> These builds are currently not signed, which means while launching this application,
> Microsoft SmartScreen warnings may pop up. Press "Run anyway" to run the application.
> Also, Windows Security (i.e. Antimalware Service Executable) may try to scan the application while it is being launched,
> which will delay and increase the startup time.

### Download and Installation
All downloads can be found within the 'Releases' page of the repository.
Select a download with the "win-" prefix and the appropriate CPU architecture.

- Download the zip archive to a known path and extract it.
- Open CMD or Powershell, and run the binary (or alternatively, double-click on the executable to launch the daemon).

## MacOS
- Preferably Ventura or later (The daemon was tested on Sequoia).

> [!CAUTION]
> Do not attempt to execute this application as root.
> This application only requires the __Bluetooth__ and __Full Disk Access__ permissions.

> [!NOTE]
> These builds are ad-hoc signed within the CI, which means additional steps must be done
> before executing the application, which is described in the next section.

### Download and Installation
All downloads can be found within the 'Releases' page of the repository.
Select a download with the "osx-" prefix and the appropriate CPU architecture.

- Download the zip archive to a known path and extract it to the **/Applications** folder.
- Execute the following command, which removes the 'com.apple.quarantine' bit from the file, so that
  it can be launched:
  ```sh
  xattr -dr com.apple.quarantine /Applications/Haraltd.app
  ```
- Double-click on the application, and wait for it to launch. If the system prompts for any permissions, press "OK".

Alternatively, to execute it as a command-line tool, use the following command:
```sh
/Applications/Haraltd.app/Contents/MacOS/haraltd <commands>
```

> [!WARNING]
> The binary must remain within the application. Do not attempt to place the binary in a location other than the application container.

# Documentation
This documentation is mostly a quick-start guide. Detailed documentation will come later.

Interaction with the daemon can be done via:
- The console (command-line mode)
- A socket (rpc mode)

## Command-line
As with most command-line tools,
- `--help` or `-h` will show the command's help page.
- `--version` or `-v` will show the version information.

The 'help' option can be used with subcommands as well, for example:
```
haraltd adapter --help
```

### Adapter commands
- To switch on/off the adapter:
```
haraltd adapter set-power-state <on|off>
```

- To get information about the Bluetooth adapter:
```
haraltd adapter properties
```

- To scan for Bluetooth devices:
```
haraltd adapter discovery start
```
(Press Ctrl-C to stop discovery)

- To get the currently paired devices:
```
haraltd adapter get-paired-devices
```

### Device commands
Most subcommands of the 'device' command have a mandatory `--address` or `-a` parameter to specify the Bluetooth address of the device.<br />

- To pair a device:
```
haraltd device pair -a <address>
```
While pairing, a prompt may appear (to confirm whether PINs match between host and remote device, for example).<br />
Press "y" or "n" to accept or decline pairing with the remote device.<br />
Note that a default timeout of 10 seconds is set to wait for a reply from the user.<br />

- Similarly, to unpair a device:
```
haraltd device remove -a <address>
```

- To view information about a device:
```
haraltd device properties -a <address>
```

Once a device has been paired, a connection can be made to the device.<br />
To connect to a Bluetooth device, a Bluetooth profile must be specified.<br />
Currently, within the daemon, each connectable profile appears as a subcommand of the `connect` command.<br />

To view all connectable/supported profiles:
```
haraltd device connect --help
```

To automatically connect to a device:
```
haraltd device connect -a <address>
```

**Some connection examples:**
- To start an A2DP session with a device:
```
haraltd device connect a2dp -a <address>
```
(Note that the application has to remain open to maintain the session. Closing the app will close the session.)

- To disconnect a connected device:
```
haraltd device disconnect -a <address>
```

## RPC
A Unix Domain socket is required, currently, to perform RPC with the daemon.<br />
Future versions may support named pipes as well.

JSON is the primary data format used to exchange information to and from the daemon.<br />

**Note: Pbap and Map commands aren't ready for use via RPC yet.**

### Start/Stop the server
To start the server over a socket:
```
haraltd server start
```
A socket will be automatically created, a popup will be displayed, and a system tray icon will be created once the service starts.
The tray icon can be right-clicked to show options (for example, to stop the daemon).

To stop the server:
```
haraltd server stop
```

Once the server has started, commands can be sent to the daemon via the socket.

For more information on how to communicate with the server, see the [RPC specification](https://github.com/bluetuith-org/haraltd/blob/master/docs/daemon-rpc-spec.md).

# Credits
These repositories were invaluable during the development of the daemon.<br />
Please star these repositories individually, and possibly submit contributions to them.<br />

- For very extensive Bluetooth related functionality, wrappers and documentation: [32feet](https://github.com/inthehand/32feet).<br />
(A clone of the '32feet' library is [here](https://github.com/bluetuith-org/32feet))

- For very good PBAP and MAP support: [MyPhone](https://github.com/BestOwl/MyPhone).<br />
(A clone of the 'MyPhone.OBEX' library is [here](https://github.com/bluetuith-org/MyPhone.Obex), with added basic OPP support)

- For native adapter related functionalities: [Nefarius' Bluetooth Utilities](https://github.com/nefarius/Nefarius.Utilities.Bluetooth).
