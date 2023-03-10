using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using BozjaBuddy.Windows;
using BozjaBuddy.Data;
using ImGuiNET;
using Dalamud.Logging;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState.Fates;
using System.Collections.Generic;

namespace BozjaBuddy
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Bozja Buddy";
        private const string CommandName = "/bb";

        public float TEXT_BASE_HEIGHT = ImGui.GetTextLineHeightWithSpacing();
        public Dictionary<string, string> DATA_PATHS = new Dictionary<string, string>();

        public DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public GameGui GameGui { get; init; }
        public FateTable FateTable { get; init; }
        public DataManager DataManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Bozja Buddy");  
        
        public BBDataManager mBBDataManager;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] FateTable fateTable)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.DataManager = dataManager;
            this.GameGui = gameGui;
            this.FateTable = fateTable;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            string tDir = PluginInterface.AssemblyLocation.DirectoryName!;
            this.DATA_PATHS["db"] = Path.Combine(tDir, @"db\LostAction.db");
            this.DATA_PATHS["loadout.json"] = Path.Combine(tDir, @"db\loadout.json");

            mBBDataManager = new BBDataManager(this);
            mBBDataManager.SetUpAuxiliary();
            WindowSystem.AddWindow(new ConfigWindow(this));
            WindowSystem.AddWindow(new MainWindow(this));

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Open the main menu"
            }); 

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            WindowSystem.GetWindow("Bozja Buddy")!.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            WindowSystem.GetWindow("Bozja Buddy Config")!.IsOpen = true;
        }
    }
}
