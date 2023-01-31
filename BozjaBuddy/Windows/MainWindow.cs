using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Data;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Excel.GeneratedSheets;
using Lumina.Extensions;
using Newtonsoft.Json;
using SamplePlugin.Data;
using SamplePlugin.Filter;
using SamplePlugin.GUI.Tabs;

namespace SamplePlugin.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private LostActionTab mLostActionTab;
    private FateCeTab mFateCeTab;
    private MobTab mMobTab;
 
    public MainWindow(Plugin plugin) : base(
        "Bozja Buddy", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.Plugin = plugin;

        this.mLostActionTab = new LostActionTab(this.Plugin);
        this.mFateCeTab = new FateCeTab(this.Plugin);
        this.mMobTab = new MobTab(this.Plugin);
    }

    public void Dispose()
    {
        this.mLostActionTab.Dispose();
        this.mFateCeTab.Dispose();
    }

    unsafe ImGuiTextFilterPtr GetTextFilterPtr() => new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

    public override void Draw()
    {
        if (ImGui.BeginTabBar("Tab Bat")) {
            this.mLostActionTab.DrawGUI();
            this.mFateCeTab.DrawGUI();
            this.mMobTab.DrawGUI();

            ImGui.EndTabBar();
        }

        ImGui.Spacing();

    }
}
