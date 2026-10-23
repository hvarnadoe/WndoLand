using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using static WndoLand.Win32;

namespace WndoLand
{
    internal class KeybindsFile
    {
        [JsonPropertyName("shortcuts")]
        public List<Keybinds> Shortcuts { get; init; } = new();
    }
    internal static class KeybindStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static List<Keybinds> Read(string path, bool optional)
        {
            if (optional && !File.Exists(path)) return new List<Keybinds>();
            var file = JsonSerializer.Deserialize<KeybindsFile>(File.ReadAllText(path))
                       ?? throw new Exception($"Unable to parse hotkeys in {path}");
            return file.Shortcuts;
        }

        private static void WriteUser(List<Keybinds> user) =>
            File.WriteAllText(AppPaths.UserKeybinds, JsonSerializer.Serialize(new KeybindsFile { Shortcuts = user }, JsonOptions));

        public static List<Keybinds> LoadDefault() => Read(AppPaths.DefaultKeybinds, optional: false);

        public static List<Keybinds> LoadUser() => Read(AppPaths.UserKeybinds, optional: true);

        public static List<Keybinds> LoadAll()
        {
            var all = LoadDefault();
            all.AddRange(LoadUser());
            return all;
        }

        public static void AddUser(Keybinds keybind)
        {
            var user = LoadUser();
            user.Add(keybind);
            WriteUser(user);
        }

        public static void ReplaceUser(int index, Keybinds keybind)
        {
            var user = LoadUser();
            user[index] = keybind;
            WriteUser(user);
        }

        public static void RemoveUser(int index)
        {
            var user = LoadUser();
            user.RemoveAt(index);
            WriteUser(user);
        }
    }

    internal static class KeyNames
    {
        public static List<Keys> ModifierKeys(uint bits)
        {
            var keys = new List<Keys>();
            if ((bits & Win32.MOD_ALT) != 0) keys.Add(Keys.Menu);
            if ((bits & Win32.MOD_CONTROL) != 0) keys.Add(Keys.ControlKey);
            if ((bits & Win32.MOD_SHIFT) != 0) keys.Add(Keys.ShiftKey);
            if ((bits & Win32.MOD_WIN) != 0) keys.Add(Keys.LWin);
            return keys;
        }

        public static string Display(Keys key) => key switch
        {
            Keys.ControlKey => "Ctrl",
            Keys.ShiftKey => "Shift",
            Keys.Menu => "Alt",
            Keys.LWin => "Win",
            >= Keys.D0 and <= Keys.D9 => ((int)key - (int)Keys.D0).ToString(),
            _ => key.ToString()
        };
    }

    internal class Keybinds
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("modifiers")]
        [JsonConverter(typeof(HexFlagsConverter))]
        public uint Modifiers { get; init; }

        [JsonPropertyName("key")]
        [JsonConverter(typeof(HexEnumConverter<Keys>))]
        public Keys Key { get; init; }

        [JsonPropertyName("target")]
        public Target Target { get; init; }

        [JsonPropertyName("action")]
        [JsonConverter(typeof(HexEnumConverter<Actions>))]
        public Actions Action { get; init; }

        [JsonPropertyName("process_path")]
        public string? ProcessPath { get; init; }
        [JsonPropertyName("command")]
        public string? Command { get; init; }
        [JsonPropertyName("target_workspace")]
        public int WorkspaceNum { get; init; }

        [JsonPropertyName("direction")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Direction? Direction { get; init; }

        [JsonIgnore]
        public string HotkeyText => string.Join(" + ", KeyNames.ModifierKeys(Modifiers).Append(Key).Select(KeyNames.Display));

        [JsonIgnore]
        public string Summary => Target switch
        {
            Target.Process => $"Launch {ProcessPath}",
            Target.Cmd => $"Run {Command} ({(Action == Actions.RunCmdInWindow ? "in a window" : "hidden")})",
            Target.Workspace => $"Switch to workspace {WorkspaceNum}",
            Target.App => "Show or hide the WndoLand window",
            _ => Action switch
            {
                Actions.ChangeWindowWorkspace => $"Send window to workspace {WorkspaceNum}",
                Actions.FocusWindow => $"Focus window {Direction}",
                Actions.MoveWindow => $"Swap window {Direction}",
                Actions.CloseWindow => "Close window",
                Actions.ForceCloseWindow => "Force close window (ends its process)",
                Actions.ToggleFloat => "Toggle floating",
                Actions.ToggleFullscreen => "Toggle fullscreen",
                Actions.MoveWindowToMonitor => "Move window to next monitor",
                _ => Action.ToString()
            }
        };

        public bool Register(int hkId, nint handle) => RegisterHotKey(handle, hkId, Modifiers, (uint)Key);

        public Func<Task> CreateAction(Workspaces? workspace = null, int workspaceNum = 0, Action? toggleMainWindow = null) => Target switch
        {
            Target.Process => CreateProcessAction(),
            Target.Window => CreateWindowAction(workspace ?? new Workspaces(), workspaceNum),
            Target.Cmd => CreateCmdAction(),
            Target.App => CreateAppAction(toggleMainWindow ?? throw new ArgumentNullException(nameof(toggleMainWindow))),
            Target.Workspace => CreateWorkspaceAction(workspace ?? new Workspaces(), workspaceNum),
            _ => throw new InvalidOperationException("Invalid type of hotkey")
        };

        private Func<Task> CreateProcessAction()
        {
            var path = ProcessPath ?? throw new ArgumentNullException(nameof(ProcessPath));

            return () =>
            {
                Log.Info($"Launching process '{path}' (hotkey '{Name}')");
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return Task.CompletedTask;
            };
        }

        private Func<Task> CreateAppAction(Action toggleMainWindow)
        {
            return () =>
            {
                Log.Debug($"App hotkey '{Name}' -> toggle main window");
                toggleMainWindow();
                return Task.CompletedTask;
            };
        }

        private Func<Task> CreateCmdAction()
        {
            var command = Command ?? throw new ArgumentNullException(nameof(Command));
            bool inWindow = Action == Actions.RunCmdInWindow;

            return () =>
            {
                Log.Info($"Running command '{command}' {(inWindow ? "in a window" : "hidden")} (hotkey '{Name}')");
                Process.Start(new ProcessStartInfo("cmd.exe", (inWindow ? "/k " : "/c ") + command)
                {
                    UseShellExecute = inWindow,
                    CreateNoWindow = !inWindow
                });
                return Task.CompletedTask;
            };
        }

        private Func<Task> CreateWindowAction(Workspaces workspace, int workspaceNum)
        {
            return Action switch
            {
                Actions.ChangeWindowWorkspace => () =>
                {
                    Log.Debug($"Window hotkey '{Name}' -> send active window to workspace {workspaceNum}");
                    workspace.MoveActiveWindowToWorkspace(workspaceNum);
                    return Task.CompletedTask;
                },
                Actions.CloseWindow => () =>
                {
                    Log.Debug($"Window hotkey '{Name}' -> close active window");
                    workspace.CloseActiveWindow();
                    return Task.CompletedTask;
                },
                Actions.FocusWindow => FocusAction(workspace),
                Actions.MoveWindow => SwapAction(workspace),
                Actions.ForceCloseWindow => () =>
                {
                    Log.Debug($"Window hotkey '{Name}' -> force close active window");
                    workspace.ForceCloseActiveWindow();
                    return Task.CompletedTask;
                },
                Actions.ToggleFloat => () =>
                {
                    Log.Debug($"Window hotkey '{Name}' -> toggle floating");
                    workspace.ToggleFloat();
                    return Task.CompletedTask;
                },
                Actions.ToggleFullscreen => () =>
                {
                    Log.Debug($"Window hotkey '{Name}' -> toggle fullscreen");
                    workspace.ToggleFullscreen();
                    return Task.CompletedTask;
                },
                Actions.MoveWindowToMonitor => () =>
                {
                    Log.Debug($"Window hotkey '{Name}' -> move window to next monitor");
                    workspace.MoveActiveWindowToNextMonitor();
                    return Task.CompletedTask;
                },
                _ => throw new NotImplementedException($"Window action {Action} is not implemented")
            };
        }

        private Func<Task> SwapAction(Workspaces workspace)
        {
            var dir = Direction ?? throw new InvalidOperationException($"Keybind '{Name}' needs a \"direction\"");
            return () =>
            {
                Log.Debug($"Window hotkey '{Name}' -> swap window {dir}");
                workspace.SwapInDirection(dir);
                return Task.CompletedTask;
            };
        }

        private Func<Task> FocusAction(Workspaces workspace)
        {
            var dir = Direction ?? throw new InvalidOperationException($"Keybind '{Name}' needs a \"direction\"");
            return () =>
            {
                Log.Debug($"Window hotkey '{Name}' -> focus window {dir}");
                workspace.FocusInDirection(dir);
                return Task.CompletedTask;
            };
        }

        private Func<Task> CreateWorkspaceAction(Workspaces workspace, int workspaceNum)
        {
            return () => {
                Log.Debug($"Workspace hotkey '{Name}' -> workspace {workspaceNum}");
                workspace.SetActiveWorkspace(workspaceNum);
                return Task.CompletedTask;
            };
        }
    }
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }
    public enum Target
    {
        Window,
        Process,
        Cmd,
        Workspace,
        App
    }
    public enum Actions
    {
        MoveWindow = 0x00,
        ChangeWindowWorkspace = 0x01,
        EnlargeWindow = 0x02,
        ShrinkWindow = 0x03,
        CloseWindow = 0x04,
        DuplicateWindow = 0x05,
        FocusWindow = 0x06,
        ToggleFloat = 0x07,
        ToggleFullscreen = 0x08,
        MoveWindowToMonitor = 0x09,
        ForceCloseWindow = 0x0A,
        LaunchProcess = 0x10,
        RunCmdHidden = 0x20,
        RunCmdInWindow = 0x21,
        SwitchToWorkspaceNum = 0x30,
        ToggleMainWindow = 0x40,
    }

    internal static class HexParser
    {
        public static ulong Read(ref Utf8JsonReader reader)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetUInt64();

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString()!.Trim();
                if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return Convert.ToUInt64(s[2..], 16);
                return ulong.Parse(s);
            }

            throw new JsonException($"Unexpected token {reader.TokenType} for hex value.");
        }
    }
    internal class HexFlagsConverter : JsonConverter<uint>
    {
        public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                return (uint)HexParser.Read(ref reader);

            uint result = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                result |= (uint)HexParser.Read(ref reader);
            return result;
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue($"0x{value:X4}");
            writer.WriteEndArray();
        }
    }
    internal class HexEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = HexParser.Read(ref reader);
            return (T)Enum.ToObject(typeof(T), value);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"0x{Convert.ToUInt64(value):X}");
        }
    }
}

