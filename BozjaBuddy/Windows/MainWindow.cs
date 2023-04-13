using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using BozjaBuddy.GUI.Tabs;
using BozjaBuddy.GUI.Sections;

namespace BozjaBuddy.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private LostActionTab mLostActionTab;
    private FateCeTab mFateCeTab;
    private MobTab mMobTab;
    private LoadoutTab mLoadoutTab;
    private GeneralSection mGeneralSection;
 
    public MainWindow(Plugin plugin) : base(
        "Bozja Buddy", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(850, 485),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.Plugin = plugin;

        this.mLostActionTab = new LostActionTab(this.Plugin);
        this.mFateCeTab = new FateCeTab(this.Plugin);
        this.mMobTab = new MobTab(this.Plugin);
        this.mLoadoutTab = new LoadoutTab(this.Plugin);
        this.mGeneralSection = new GeneralSection(this.Plugin);
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
        this.mGeneralSection.DrawGUI();
        if (ImGui.BeginTabBar("Tab Bat")) {
            this.mLostActionTab.DrawGUI();
            this.mFateCeTab.DrawGUI();
            this.mMobTab.DrawGUI();
            this.mLoadoutTab.DrawGUI();

            ImGui.EndTabBar();
        }
    }
}
