using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using BozjaBuddy.GUI.Tabs;

namespace BozjaBuddy.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private LostActionTab mLostActionTab;
    private FateCeTab mFateCeTab;
    private MobTab mMobTab;
    private LoadoutTab mLoadoutTab;
 
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
        this.mLoadoutTab = new LoadoutTab(this.Plugin);
    }

    public void Dispose()
    {
        this.mLostActionTab.Dispose();
        this.mFateCeTab.Dispose();
        this.mMobTab.Dispose();
        this.mLoadoutTab.Dispose();
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("Tab Bat")) {
            this.mLostActionTab.DrawGUI();
            this.mFateCeTab.DrawGUI();
            this.mMobTab.DrawGUI();
            this.mLoadoutTab.DrawGUI();

            ImGui.EndTabBar();
        }
    }
}
