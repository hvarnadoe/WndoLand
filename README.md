# WndoLand

My attempt at a Hyprland-like window manager for Windows.

WndoLand runs quietly in the system tray and tiles your open windows into a grid, with ten workspaces you can switch between using global hotkeys.

## What it does

- **Tiling:** windows are laid out automatically as they open and close, and title bars and borders are removed so they sit flush.
- **Workspaces:** ten workspaces, with hotkeys to switch between them and to send a window to another one.
- **Window control:** focus, swap, float, fullscreen and close windows from the keyboard, and send them to another monitor.
- **Active window highlight:** a thin coloured border shows which window has focus.
- **Custom hotkeys:** launch an app or run a command with a key combination you choose.
- **Per-app rules:** make certain apps float, or always open on a specific workspace.

The app starts hidden in the tray. Open its window from the tray icon or with `Alt+Shift+W` to manage hotkeys and settings.

## Default hotkeys

| Keys | Action |
| --- | --- |
| `Alt` + `1`–`0` | Switch to workspace |
| `Alt+Shift` + `1`–`0` | Send window to workspace |
| `Alt` + arrow keys | Focus window in that direction |
| `Alt+Shift` + arrow keys | Swap window in that direction |
| `Alt` + `Q` | Close window |
| `Alt+Shift` + `Q` | Force close window (ends its process) |
| `Alt+Shift` + `F` | Toggle floating |
| `Alt+Shift` + `Enter` | Toggle fullscreen |
| `Alt+Shift` + `N` | Move window to the next monitor |
| `Alt` + `W` | Launch Terminal |
| `Alt+Shift` + `W` | Show or hide the WndoLand window |

## Installing

Download the most recent setup.exe from the releases tab and run it. By default it will be registered to start with windows this can be disabled in the settings. You can view the control panel with `Alt + Shift + W`.
