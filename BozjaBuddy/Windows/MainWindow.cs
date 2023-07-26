using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using BozjaBuddy.GUI.Tabs;
using BozjaBuddy.GUI.Sections;
using BozjaBuddy.Utils;

namespace BozjaBuddy.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    private LostActionTab mLostActionTab;
    private FateCeTab mFateCeTab;
    private MobTab mMobTab;
    private LoadoutTab mLoadoutTab;
    private GeneralSection mGeneralSection;
    private FieldNoteTab mFieldNoteTab;
    private DrsTab mDrsTab;
    private QuestTab mQuestTab;
    private RelicTab mRelicTab;
 
    public MainWindow(Plugin plugin) : base(
        "Bozja Buddy", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.RespectCloseHotkey = false;
        this.SizeConstraints = plugin.Configuration.SizeConstraints;
        this.Plugin = plugin;

        this.mLostActionTab = new LostActionTab(this.Plugin);
        this.mFateCeTab = new FateCeTab(this.Plugin);
        this.mMobTab = new MobTab(this.Plugin);
        this.mLoadoutTab = new LoadoutTab(this.Plugin);
        this.mGeneralSection = new GeneralSection(this.Plugin);
        this.mFieldNoteTab = new FieldNoteTab(this.Plugin);
        this.mDrsTab = new(this.Plugin);
        this.mQuestTab = new(this.Plugin);
        this.mRelicTab = new(this.Plugin);
    }

    public void RearrangeSection()
    {
        this.mLostActionTab.RearrangeSection();
        this.mFateCeTab.RearrangeSection();
        this.mMobTab.RearrangeSection();
        this.mLoadoutTab.RearrangeSection();
        this.mFieldNoteTab.RearrangeSection();
        this.mDrsTab.RearrangeSection();
        this.mQuestTab.RearrangeSection();
        this.mRelicTab.RearrangeSection();
    }
    public void Dispose()
    {
        this.mLostActionTab.Dispose();
        this.mFateCeTab.Dispose();
        this.mMobTab.Dispose();
        this.mLoadoutTab.Dispose();
        this.mFieldNoteTab.Dispose();
        this.mDrsTab.Dispose();
        this.mQuestTab.Dispose();
        this.mRelicTab.Dispose();
    }

    public override void Draw()
    {
        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)
            && ImGui.GetIO().KeyAlt
            && UtilsGUI.InputPayload.CheckKeyClickValidity())
        {
            this.Plugin.Configuration.mIsAuxiFocused = !this.Plugin.Configuration.mIsAuxiFocused;
            this.Plugin.MainWindow.RearrangeSection();
        }
        this.mGeneralSection.DrawGUI();
        if (ImGui.BeginTabBar("Tab Bat")) {
            this.mRelicTab.DrawGUI();
            this.mLostActionTab.DrawGUI();
            this.mFateCeTab.DrawGUI();
            this.mMobTab.DrawGUI();
            this.mFieldNoteTab.DrawGUI();
            this.mQuestTab.DrawGUI();
            this.mDrsTab.DrawGUI();
            this.mLoadoutTab.DrawGUI();

            ImGui.EndTabBar();
        }
    }
}
