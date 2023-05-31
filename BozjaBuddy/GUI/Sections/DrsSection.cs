using ImGuiNET;
using System.Numerics;
using System;
using System.Collections.Generic;
using BozjaBuddy.Utils;
using Dalamud.Interface.Components;
using ImGuiScene;

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
            { 7, "T. Avowed (Hot/Cold)" },
            { 8, "Stygimoloch" },
            { 9, "Queen" }
        };
        private Dictionary<string, List<List<string>>> mCommunitiesInfo = new() {
            { 
                "na", new() {
                    new() { "PEBE", "Primal Eureka/Bozja Enjoyer", "(static/reclears)", "https://discord.gg/PEBE" },
                    new() { "ABBA", "Aether Bozja/Baldesion Arsenal", "(anyprog)", "https://discord.gg/abbaffxiv" },
                    new() { "LegoSteppers", "(Aether)", "(anyprog)", "https://discord.gg/YKP76AsMw8" },
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
            this.kSectionHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.5f) * 2; // default=(15 + 3.5f)*1
        }

        public override bool DrawGUI()
        {
            var tHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetStyle().WindowPadding.Y;
            ImGui.BeginChild("##drsSectionTpcList", new Vector2(ImGui.GetWindowWidth() / 5, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopicList();
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("##drsSectionTpc", new Vector2(
                ImGui.GetWindowWidth() / 5 * 4 - ImGui.GetStyle().WindowPadding.X * 2, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
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
                case 2: this.DrawTopic_SlimeGolem(); break;
                case 3: this.DrawTopic_TrinitySeeker(); break;
                case 4: this.DrawTopic_Dahu(); break;
                case 5: this.DrawTopic_QueensGuard(); break;
                case 6: this.DrawTopic_Wrath(); break;
                case 7: this.DrawTopic_TrinityAvowed(); break;
                case 8: this.DrawTopic_Stygimoloch(); break;
                case 9: this.DrawTopic_Queen(); break;
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
            UtilsGUI.TextDescriptionForWidget("\t• Respect their guidelines.\t• Prepare appropriate actions.\t• Don't be toxic and have fun.");
            UtilsGUI.TextDescriptionForWidget("- No parse, no hardcore raiding experience required.");
            UtilsGUI.TextDescriptionForWidget("- Don't hesitate to DC travel between communities. Just make sure to stick with your static til the end, even if you've already cleared in another group!");
            ImGui.Text("Out of respect, we ask our users to AVOID mentioning this plugin in any community.");            
            ImGui.SameLine();
            UtilsGUI.ShowHelpMarker("First off, plugins are against ToS. Hence don't talk about it in the first place.\n- Info in this plugin should be taken with a grain of salt.\n- We don't want people to annoy the mods/hosts by bringing this plugin up as an argument or excuse (i.e. '...but Bozja Buddy made me bring wrong stuff').\n- We created this plugin to makes life easier, not to become a nuisance to anyone.");
            UtilsGUI.TextDescriptionForWidget("- This plugin is not affiliated with any community or website. All content included in this plugin is NOT created/owned by the dev; they are community-effort.");

            ImGui.PopTextWrapPos();
        }
        public void DrawTopic_Communities()
        {
            UtilsGUI.TextDescriptionForWidget("- Typically, there are three types of run: Static, Anyprog, and Reclear.");
            ImGui.SameLine(); UtilsGUI.ShowHelpMarker("- Anyprog are runs that anyone can join, regardless of their prog progress; usually on a first-come-first-serve basis. No clear promised!\n- Reclears may also accept TA enrage/Queen's prog, depends on host. Please make sure to check with the host.");

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
        public void DrawTopic_SlimeGolem()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Separator();
            ImGui.Text("PEBE");
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_1.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_2.png", pIsScaledToRegionWidth: true);
            ImGui.Separator();
            ImGui.Text("ABBA/LegoSteppers");
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_3.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_4.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_5.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_6.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_7.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "brk_8.png", pIsScaledToRegionWidth: true);
        }
        public void DrawTopic_TrinitySeeker()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Separator();
            UtilsGUI.TextDescriptionForWidget("Just remember to re-apply your Reraiser after getting rezzed.");
        }
        public void DrawTopic_Dahu()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Separator();
            ImGui.PushTextWrapPos();
            ImGui.Text("Split flame. If you have a number of dots above your head, bring them to appropriate number marker.");
            ImGui.Text("POV example:");
            ImGui.PopTextWrapPos();
            ImGui.SameLine();
            UtilsGUI.UrlButton("https://imgur.com/K5YuRvd");
            UtilsGUI.DrawImgFromDb(this.mPlugin, "dh_1.png", pIsScaledToRegionWidth: true);
        }
        public void DrawTopic_QueensGuard()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Separator();

            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_1.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_2.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_3.png", pIsScaledToRegionWidth: true);
            ImGui.EndGroup();
            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_4.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_5.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.EndGroup();
            ImGui.SameLine();
            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_6.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_7.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_8.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qg_9.png", pIsScaledToRegionWidth: true);
            ImGui.EndGroup();
        }
        public void DrawTopic_Wrath()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Separator();

            UtilsGUI.TextDescriptionForWidget("Beware of Ice spike.");
        }
        public void DrawTopic_TrinityAvowed()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Text("Sword example:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://imgur.com/3SGy2Ih");
            ImGui.SameLine(); ImGui.Text("\t\t\tArrow ladder:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://imgur.com/8cOz6V8");
            ImGui.Separator();

            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_2.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_4.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.EndGroup();
            ImGui.SameLine();
            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_1.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_3.png", pIsScaledToRegionWidth: true);
            ImGui.EndGroup();

            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_5.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_6.png", pIsScaledToRegionWidth: true);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_7.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.SameLine();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "tav_8.png", pIsScaledToRegionWidth: true);
        }
        public void DrawTopic_Stygimoloch()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Text("POV #2:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://imgur.com/5HjEeEt");
            ImGui.Separator();

            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "mnt_2.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "mnt_4.png", pIsScaledToRegionWidth: true, pExtraScaling: 0.5f);
            ImGui.EndGroup();
            ImGui.SameLine();
            ImGui.BeginGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "mnt_3.png", pIsScaledToRegionWidth: true);
            ImGui.EndGroup();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "mnt_1.png", pIsScaledToRegionWidth: true);
        }
        public void DrawTopic_Queen()
        {
            ImGui.Text("These are merely for references. Prioritize your host's instructions! POV playlist:");
            ImGui.SameLine(); UtilsGUI.UrlButton("https://youtube.com/playlist?list=PLfH2VGgD6CCwYv-7nTniyXBgIxnBZZYeQ");
            ImGui.Separator();
            UtilsGUI.DrawImgFromDb(this.mPlugin, "qun_1.png", pIsScaledToRegionWidth: true);
        }
        public override void DrawGUIDebug() { }

        public override void Dispose() { }
    }
}
