using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using SamplePlugin.Windows;
using SamplePlugin.Data;
using ImGuiNET;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using Lumina.Extensions;
using Lumina.Data.Files;
using Dalamud.Game.Gui;
using Dalamud.Logging;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Bozja Buddy";
        private const string CommandName = "/bb";

        public float TEXT_BASE_HEIGHT = ImGui.GetTextLineHeightWithSpacing();

        public DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public GameGui GameGui { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("Bozja Buddy");  

        public DataManager DataManager { get; init; }
        
        public BBDataManager mBBDataManager;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] GameGui gameGui)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.DataManager = dataManager;
            this.GameGui = gameGui;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            string tDir = PluginInterface.AssemblyLocation.DirectoryName!;
            var tDataPath = Path.Combine(tDir, @"db\LostAction.db");

            mBBDataManager = new BBDataManager(this, tDataPath);
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
