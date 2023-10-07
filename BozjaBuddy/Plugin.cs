using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using BozjaBuddy.Windows;
using BozjaBuddy.Data;
using BozjaBuddy.Data.Alarm;
using ImGuiNET;
using Dalamud.Logging;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Fates;
using System.Collections.Generic;
using BozjaBuddy.GUI.GUIAssist;
using System;
using Dalamud.Game.ClientState;
using BozjaBuddy.Utils;
using BozjaBuddy.GUI;
using System.Numerics;
using BozjaBuddy.GUI.NodeGraphViewer;
using BozjaBuddy.GUI.NodeGraphViewer.ext;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace BozjaBuddy
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Bozja Buddy";
        private const string CommandName = "/bb";
        public bool mIsMainWindowActive = false;
        private DateTime _mCycle1 = DateTime.Now;
        public static bool _isImGuiSafe = false;
        private static Dictionary<string, Window> WINDOWS = new();

        public float TEXT_BASE_HEIGHT = ImGui.GetTextLineHeightWithSpacing();
        public Dictionary<string, string> DATA_PATHS = new Dictionary<string, string>();

        public DalamudPluginInterface PluginInterface { get; init; }
        public ICommandManager CommandManager { get; init; }
        public IGameGui GameGui { get; init; }
        public IFateTable FateTable { get; init; }
        public IChatGui ChatGui { get; init; }
        public IClientState ClientState { get; init; }
        public IDataManager DataManager { get; init; }
        public IKeyState KeyState { get; init; }
        public ITextureProvider TextureProvider { get; init; }
        public BBDataManager mBBDataManager;

        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Bozja Buddy");
        public AlarmManager AlarmManager { get; init; }
        public GuiScraper GuiScraper { get; init; }
        public GUIAssistManager GUIAssistManager { get; init; }
        public MainWindow MainWindow { get; init; }
        public NodeGraphViewer NodeGraphViewer_Auxi { get; init; }
        public IFramework Framework { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IDataManager dataManager,
            [RequiredVersion("1.0")] IGameGui gameGui,
            [RequiredVersion("1.0")] IFateTable fateTable,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] IKeyState keyState,
            [RequiredVersion("1.0")] IFramework framework,
            [RequiredVersion("1.0")] ITextureProvider textureProvider)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.DataManager = dataManager;
            this.GameGui = gameGui;
            this.FateTable = fateTable;
            this.ChatGui = chatGui;
            this.ClientState = clientState;
            this.KeyState = keyState;
            this.Framework = framework;
            this.TextureProvider = textureProvider;

            string tDir = PluginInterface.AssemblyLocation.DirectoryName!;
            this.DATA_PATHS["db"] = Path.Combine(tDir, @"db\LostAction.db");
            this.DATA_PATHS["loadout.json"] = Path.Combine(tDir, @"db\loadout.json");
            this.DATA_PATHS["loadout_preset.json"] = Path.Combine(tDir, @"db\loadout_preset.json");
            this.DATA_PATHS["alarm_audio"] = Path.Combine(tDir, @"db\audio\epicsaxguy.mp3");
            this.DATA_PATHS["alarm.json"] = Path.Combine(PluginInterface.GetPluginConfigDirectory(), @"alarm.json");
            this.DATA_PATHS["UIMap_LostAction.json"] = Path.Combine(tDir, @"db\UIMap_LostAction.json");
            this.DATA_PATHS["YurukaStd-UB-AlphaNum.ttf"] = Path.Combine(tDir, @"db\YurukaStd-UB-AlphaNum.otf");

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            PluginLog.LogDebug($"> configPath: {this.PluginInterface.ConfigDirectory}");
            this.Configuration.Initialize(this.PluginInterface);
            if (this.Configuration.mAudioPath == null) this.Configuration.mAudioPath = this.DATA_PATHS["alarm_audio"];
            this.Configuration.Save();

            BBNode.kPlugin = this;
            mBBDataManager = new BBDataManager(this);
            this.NodeGraphViewer_Auxi = new(this.Configuration.mAuxiNGVSaveData);
            UtilsGameData.Init(this);
            this.MainWindow = new(this);
            Plugin.AddWindow(new ConfigWindow(this));
            Plugin.AddWindow(this.MainWindow);
            Plugin.AddWindow(new AlarmWindow(this));
            Plugin.AddWindow(new CharStatsWindow(this));
            Plugin.AddWindow(new TestWindow(this));

            this.Configuration.mAudioPath = this.DATA_PATHS["alarm_audio"];

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the main menu"
            }); 

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.AlarmManager = new AlarmManager(this);
            this.AlarmManager.Start();

            this.GuiScraper = new(this);
            this.Framework.Update += this.OnUpdate;

            this.GUIAssistManager = new(this);

            if (this.Configuration.UserLoadouts == null) { this.mBBDataManager.ReloadLoadoutsPreset(); }    // for first install
            this.Configuration.SizeConstraints = new()      // overwrite on-disk config's size constraint
            {
                MinimumSize = new Vector2(675, 509),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            this.PluginInterface.UiBuilder.BuildFonts += this.BuildFont;
            this.PluginInterface.UiBuilder.RebuildFonts();

            this.MainWindow.RearrangeSection();
        }
        
        private void BuildFont()
        {
            unsafe
            {
                UtilsGameData.kFont_Yuruka = ImGui.GetIO().Fonts.AddFontFromFileTTF(this.DATA_PATHS["YurukaStd-UB-AlphaNum.ttf"], 30);
            }
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            this.AlarmManager.Dispose();
            this.PluginInterface.UiBuilder.BuildFonts -= this.BuildFont;
            UtilsGameData.Dispose();
            this.mBBDataManager.Dispose();
            this.NodeGraphViewer_Auxi.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            Plugin.GetWindow("Bozja Buddy")!.IsOpen = true;
        }
        // Called every frame, before Imgui's draw.
        private void OnUpdate(IFramework framework)
        {
            if (!ClientState.IsLoggedIn) return;
            this.GuiScraper.Scrape();
        }

        private void DrawUI()
        {
            Plugin._isImGuiSafe = true;
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
            this.WindowSystem.Draw();
            this.GUIAssistManager.Draw();
            ImGui.PopStyleVar();

            if ((DateTime.Now - this._mCycle1).TotalSeconds > 2)
            {
                this.mIsMainWindowActive = Plugin.GetWindow("Bozja Buddy")!.IsOpen;

                this._mCycle1 = DateTime.Now;
            }
        }

        public void DrawConfigUI()
        {
            Plugin._isImGuiSafe = true;
            Plugin.GetWindow("Config - BozjaBuddy")!.IsOpen = true;
        }

        public bool isKeyPressed(VirtualKey key) => this.KeyState.IsVirtualKeyValid(key) && KeyState[key];

        private static bool AddWindow(Window window) => Plugin.WINDOWS.TryAdd(window.WindowName, window);
        public static Window? GetWindow(string windowName) => Plugin.WINDOWS.TryGetValue(windowName, out Window? window) && window != null ? window : null;
    }
}
