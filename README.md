# WndoLand

My attempt at a Hyprland-like window manager for Windows.

WndoLand runs quietly in the system tray and tiles your open windows into a grid, with ten workspaces you can switch between using global hotkeys.

## What it does

- **Tiling:** windows are laid out automatically as they open and close, and title bars and borders are removed so they sit flush.
- **Workspaces:** ten workspaces, with hotkeys to switch between them and to send a window to another one.
- **Window control:** focus, swap, float, fullscreen and close windows from the keyboard, and send them to another monitor.
- **Active window highlight:** the focused window's own frame is drawn in a colour you choose (Windows 11).
- **Custom hotkeys:** launch an app or run a command with a key combination you choose.
- **Per-app rules:** make certain apps float, or always open on a specific workspace.

## Default hotkeys

### Workspaces

| Keys | Action |
| --- | --- |
| `Alt` + `1`–`0` | Switch to workspace 1–10 (`0` is workspace 10) |
| `Alt+Shift` + `1`–`0` | Send the active window to workspace 1–10 |

### Windows

| Keys | Action |
| --- | --- |
| `Alt` + arrow keys | Focus the window in that direction |
| `Alt+Shift` + arrow keys | Swap the active window with its neighbour in that direction |
| `Alt+Shift` + `F` | Toggle floating |
| `Alt+Shift` + `Enter` | Toggle fullscreen |
| `Alt` + `Q` | Close window |
| `Alt+Shift` + `Q` | Force close window (ends its process) |

### Monitors

| Keys | Action |
| --- | --- |
| `Ctrl+Alt` + arrow keys | Focus the monitor in that direction |
| `Ctrl+Alt+Shift` + arrow keys | Send the active window to the monitor in that direction |
| `Alt+Shift` + `N` | Send the active window to the next monitor |

### Apps

| Keys | Action |
| --- | --- |
| `Alt` + `W` | Launch Terminal |
| `Alt+Shift` + `W` | Show or hide the WndoLand control panel |

All of these can be changed and you can add your own from the control panel.

## Installing

Download [WndoLandApp-win-Setup.exe from the latest release](https://github.com/hvarnadoe/WndoLand/releases/latest) and run it. By default it will be registered to start with windows and automatically update. These options can be disabled in the settings.

## Recomendations

By default, WndoLand is not set to run as administrator. This can be changed through the settings as well and I recommend enabling it. When it is disabled WndoLand will be unable to tile elevated windows such as task manager which can feel clunky.

I also suggest using this alongside [powertoys](https://learn.microsoft.com/en-us/windows/powertoys/), specifically [power toys run](https://learn.microsoft.com/en-us/windows/powertoys/run), as the spotlight search works quite well with this system and allows you to easilly find the location of executables for creating hotkeys.

I also like to set the taskbar to autohide as it gives more usable space on the screen and after getting used to WndoLand I felt like I didn't need it anymore. I hope you enjoy using my program!
