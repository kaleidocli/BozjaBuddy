using ImGuiNET;
using System.Numerics;
using System;
using System.Collections.Generic;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;

namespace BozjaBuddy.GUI.Sections
{
    internal class DrsSection : Section, IDisposable
    {
        private readonly float kSectionHeight;

        protected override Plugin mPlugin { get; set; }
        private int mCurrTopicId = 0;
        private Dictionary<int, string> mTopics = new()
        {
            { 0, "Intro/How-to-join" },
            { 1, "Communities" },
            { 2, "Slimes/Golem" },
            { 3, "Trinity Seeker" },
            { 4, "Dahu" },
            { 5, "Queens Guard" },
            { 6, "Wrath" },
            { 7, "Queen's Guard" },
            { 8, "T. Avowed (Hot/Cold)" },
            { 9, "Stygimoloch" },
            { 10, "Queen" }
        };
        private Dictionary<string, List<List<string>>> mCommunitiesInfo = new() {
            { 
                "na", new() {
                    new() { "PEBE", "Primal Eureka/Bozja Enjoyer", "(static/reclears)", "https://discord.gg/PEBE" },
                    new() { "ABBA", "Aether Bozja/Baldesion Arsenal", "(anyprog)", "https://discord.gg/abbaffxiv" },
                    new() { "LegoStepper", "", "(anyprog)", "https://discord.gg/YKP76AsMw8" },
                    new() { "CEM", "Crystal Exploratory Missions", "(static/reclears)", "https://discord.gg/cem" },
                    new() { "CAFE", "Crystalline Adventuring Forays & Expeditions", "(static/reclears)", "https://discord.gg/c-a-f-e" },
                    new() { "THL", "The Help Lines ™", "(static/reclears)", "https://discord.gg/thehelplines" }
                }
            },
            {
                "eu", new() {
                    new() { "Late night [Chaos]", "   ", "(static/reclears)", "https://discord.gg/28SRRADTK3" }
                }
            }
        };

        public DrsSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.kSectionHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.5f);
        }

        public override bool DrawGUI()
        {
            ImGui.BeginChild("##drsSectionTpcList", new Vector2(ImGui.GetWindowWidth() / 5, this.kSectionHeight));
            this.DrawTopicList();
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("##drsSectionTpc", new Vector2(ImGui.GetWindowWidth() / 5 * 4 - ImGui.GetStyle().WindowPadding.X * 2, this.kSectionHeight));
            this.DrawTopic();
            ImGui.EndChild();
            return true;
        }
        public void DrawTopicList()
        {
            if (ImGui.BeginListBox("##drsSectLb", ImGui.GetContentRegionAvail()))
            {
                // Intro
                UtilsGUI.TextDescriptionForWidget("======= ------ =======");
                if (ImGui.Selectable(this.mTopics[0], this.mCurrTopicId == 0)) this.mCurrTopicId = 0;
                if (ImGui.Selectable(this.mTopics[1], this.mCurrTopicId == 1)) this.mCurrTopicId = 1;
                // Notes
                UtilsGUI.TextDescriptionForWidget("======= TIPS =======");
                foreach (var t in this.mTopics)
                {
                    if (t.Key < 2) continue;
                    if (ImGui.Selectable(t.Value, this.mCurrTopicId == t.Key)) this.mCurrTopicId = t.Key;
                }
                ImGui.EndListBox();
            }
        }
        public void DrawTopic()
        {
            switch (this.mCurrTopicId)
            {
                case 0: this.DrawTopic_Intro(); break;
                case 1: this.DrawTopic_Communities(); break;
                default: this.DrawTopic_Intro(); break;
            }
        }
        public void DrawTopic_Intro()
        {
            ImGui.PushTextWrapPos();
            // #1
            ImGui.Text("Delubrum Reginae (Savage) is a 48-man duty.");
            UtilsGUI.TextDescriptionForWidget("- The difficulty is only at around EX, mechanic wise.\n- Community runs happen quite often and are super friendly, newbies and vets alike.");

            // #2
            ImGui.Text("But I'll have to grind for stuff to run, right?");
            UtilsGUI.TextDescriptionForWidget("- Yes, generally it goes like this:");
            UtilsGUI.TextDescriptionForWidget("\t1. You register for a role of your choice.");
            UtilsGUI.TextDescriptionForWidget("\t2. You check what stuff are required for your role (Reraiser, Essence & Actions)");
            UtilsGUI.TextDescriptionForWidget("\t3. You farm those stuff.");
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("+ Reraisers => sprite farming (super easy).\r\n+ Essence   => cluster farming (moderately easy).\r\n+ Actions     => cluster farming.\n\t\t\t\t\t\t\tOr a specific mob farming.\n\t\t\t\t\t\t\tOr marketboard.\r\nYou can use this plugin to find which actions drop from which fragments and their sources, and proceed from there.");
            UtilsGUI.TextDescriptionForWidget("- Three hours of farming are prob sufficient for 3-5 runs. A clear is usually before 15 runs.");

            // #3
            ImGui.Text("This is overwhelming. Where do I even start?");
            UtilsGUI.TextDescriptionForWidget("- First step of getting into DRS is to");
            ImGui.SameLine(); ImGui.Text("join a community and seek advice.");
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker("Wikis and plugins can only get you so far. The resources and supports provided by DRS communities will get you to the end and beyond.\n\nFor that reason, this plugin will not try to teach you how to do DRS, but rather pointers to the resources and communities that excel in such task.");
            UtilsGUI.TextDescriptionForWidget("- Requirements are relatively clear-cut. Most DRS hosts only ask participants to:");
            UtilsGUI.TextDescriptionForWidget("\t• Respect their guidelines.");
            UtilsGUI.TextDescriptionForWidget("\t• Prepare appropriate actions.");
            UtilsGUI.TextDescriptionForWidget("\t• Don't be toxic and have fun.");
            UtilsGUI.TextDescriptionForWidget("- No parse, no hardcore raiding experience required.");
            UtilsGUI.TextDescriptionForWidget("- Don't hesitate to DC travel between communities. Just make sure to stick with your static til the end, even if you've already cleared in another group!");
            ImGui.Text("Out of respect, we ask our users to AVOID mentioning this plugin in any community.");
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("Info in this plugin should be taken with a grain of salt.\nWe don't want people to annoy the mods/hosts by bringing this plugin up as an argument or excuse (i.e. '...but Bozja Buddy made me bring wrong stuff').\nWe created this plugin to makes life easier, not to become a nuisance to anyone.");

            ImGui.PopTextWrapPos();
        }
        public void DrawTopic_Communities()
        {
            UtilsGUI.TextDescriptionForWidget("- There are three types of run: Static, Anyprog, and Reclear.");
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker("- Anyprog are runs that anyone can join, regardless of their prog progress; usually on a first-come-first-serve basis. No clear promise!\n- Reclears may also accept TA enrage/Queen's prog, depends on host. Please make sure to check with the host.");

            ImGui.Text("NA region");
            ImGui.Separator();
            if (ImGui.BeginTable("cinfo", 4, ImGuiTableFlags.SizingStretchProp))
            {
                foreach (var tRow in this.mCommunitiesInfo["na"])
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(tRow[0]);
                    ImGui.TableNextColumn();
                    UtilsGUI.TextDescriptionForWidget(tRow[1]);
                    ImGui.TableNextColumn();
                    UtilsGUI.TextDescriptionForWidget(tRow[2]);
                    ImGui.TableNextColumn();
                    UtilsGUI.UrlButton(tRow[3]);
                }
                ImGui.EndTable();
            }

            ImGui.Text("");

            ImGui.Text("EU region");
            ImGui.Separator();
            if (ImGui.BeginTable("cinfo2", 4, ImGuiTableFlags.SizingStretchProp))
            {
                foreach (var tRow in this.mCommunitiesInfo["eu"])
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(tRow[0]);
                    ImGui.TableNextColumn();
                    UtilsGUI.TextDescriptionForWidget(tRow[1]);
                    ImGui.TableNextColumn();
                    UtilsGUI.TextDescriptionForWidget(tRow[2]);
                    ImGui.TableNextColumn();
                    UtilsGUI.UrlButton(tRow[3]);
                }
                ImGui.EndTable();
            }
        }

        public override void DrawGUIDebug()
        {

        }

        public override void Dispose() { }
    }
}
