﻿using BozjaBuddy.Utils;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiScene;
using BozjaBuddy.Data;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.GUI.Sections
{
    public class RelicSection : Section, IDisposable
    {
        public static Job kDefaultCurrJob = Job.PLD;
        private readonly float kSectionHeight;

        protected override Plugin mPlugin { get; set; }
        private RelicStep mCurrTopic = 0;
        private RelicSectionGuiFlag mGuiFlag = RelicSectionGuiFlag.None;
        private Dictionary<RelicStep, string> mTopicHeaders = new()
        {
            { RelicStep.None, "Intro" },
            { RelicStep.Resistance, "Resistance" },
            { RelicStep.ResistanceA, "Resistance Aug." },
            { RelicStep.Recollection, "Recollection" },
            { RelicStep.LawsOrder, "Law's Order" },
            { RelicStep.OTG_1, "One-time grind 1" },
            { RelicStep.LawsOrderA, "Law's Order Aug." },
            { RelicStep.OTG_2, "One-time grind 2" },
            { RelicStep.Blades, "Blade's" }
        };
        private List<RelicStep> mOrderedTopics = new()
        {
            RelicStep.None,
            RelicStep.Resistance,
            RelicStep.ResistanceA,
            RelicStep.Recollection,
            RelicStep.LawsOrder,
            RelicStep.OTG_1,
            RelicStep.LawsOrderA,
            RelicStep.OTG_2,
            RelicStep.Blades
        };
        private Dictionary<string, Location> _iterriTypeAndDefaultLocation = new();
        private List<string> _terriTypeToLoad = new()
        {
            "e3ec",             // doman enclave
            "a2f1",            // sea of cloud,
            "r2f1",              // coerthas western highland
            "d2f1",             // drav forelands
            "d2f3",             // churning mist
            "a2f2",             // azys lla
            "d2f2",              // drav hinterlands

            "g3f1",             // the fringes
            "g3f2",             // the peaks
            "g3f3",              // the lochs
            "e3f1",             // ruby sea
            "e3f2",             // yanxia
            "e3f3",             // azim steppe
        };

        public RelicSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.kSectionHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.5f) * 2; // default=(15 + 3.5f)*1
            foreach (string terriType in this._terriTypeToLoad)
            {
                this._iterriTypeAndDefaultLocation.TryAdd(terriType, new(this.mPlugin, terriType));
            }
        }

        public override bool DrawGUI()
        {
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            ImGui.BeginChild("##rlcSectionTpcList", new Vector2(ImGui.GetWindowWidth() / 10 * 2.28f, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopicList();
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("##rlcSectionTpc", new Vector2(
                ImGui.GetWindowWidth() / 10 * 7.72f - ImGui.GetStyle().WindowPadding.X * 2, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopic();
            ImGui.EndChild();
            return true;
        }
        public void DrawTopicList()
        {
            Job tCurrJob = this.mPlugin.Configuration.mRelicCurrJob;
            if (ImGui.BeginListBox("##rlcSectLb", ImGui.GetContentRegionAvail()))
            {
                // Get progress
                if (!this.mPlugin.Configuration.mRelicProgress.TryGetValue(tCurrJob, out var tProgress))
                {
                    tCurrJob = RelicSection.kDefaultCurrJob;
                    tProgress = this.mPlugin.Configuration.mRelicProgress[tCurrJob];
                }

                // Intro
                if (ImGui.Selectable(this.mTopicHeaders[RelicStep.None], this.mCurrTopic == RelicStep.None)) this.mCurrTopic = RelicStep.None;
                // Quest line
                if (this.mPlugin.mBBDataManager.mQuestChains.TryGetValue(1, out var tQuestChain_Bozja) && tQuestChain_Bozja != null)
                {
                    UtilsGUI.SelectableLink_QuestChain(this.mPlugin, "Bozja Questline", tQuestChain_Bozja);
                }
                ImGui.Separator();
                ImGui.Spacing();
                // ===================================
                // Tip
                UtilsGUI.GreyText("(click image to link chat)");
                // Relic steps
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(Vector2.One.X, ImGui.GetStyle().ItemSpacing.Y));
                foreach (var step in this.mOrderedTopics)
                {
                    var header = this.mTopicHeaders[step];
                    if (step == RelicStep.None) continue;
                    if (!this.mPlugin.Configuration.mIsRelicFirstTime && (step == RelicStep.OTG_1 || step == RelicStep.OTG_2)) continue;
                    // Relic progress button
                    ImGui.PushID(header);
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Check, defaultColor: tProgress.HasFlag(step) ? UtilsGUI.AdjustTransparency(UtilsGUI.Colors.Button_Green, 0.7f) : null, hoveredColor: UtilsGUI.Colors.Button_Green))
                    {
                        // Add prog and up to it
                        if (!tProgress.HasFlag(step))
                        {
                            foreach (var progToAdd in this.mOrderedTopics)
                            {
                                this.mPlugin.Configuration.mRelicProgress[tCurrJob] |= progToAdd;
                                if (progToAdd == step) break;
                            }
                        }
                        // Remove prog and after it
                        else
                        {
                            bool isRemoving = false;
                            foreach (var progToRemove in this.mOrderedTopics)
                            {
                                if (progToRemove == step) isRemoving = true;
                                if (isRemoving)
                                {
                                    this.mPlugin.Configuration.mRelicProgress[tCurrJob] &= ~progToRemove;
                                }
                            }
                        }
                    }
                    else UtilsGUI.SetTooltipForLastItem($"Mark as {(tProgress.HasFlag(step) ? "incompleted" : "completed")}.");
                    // Relic icon
                    ImGui.SameLine();
                    float tIconHeight = 15;
                    if (step != RelicStep.None && step != RelicStep.OTG_1 && step != RelicStep.OTG_2)
                    {
                        IDalamudTextureWrap? tIDalamudTextureWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(
                                                        Convert.ToUInt32(UtilsGameData.kRelicsAndJobs[step][tCurrJob]),
                                                        pSheet: Data.TextureCollection.Sheet.Item,
                                                        pTryLoadTexIfFailed: true
                                                        );
                        if (tIDalamudTextureWrap != null)
                        {
                            UtilsGUI.ItemLinkButton_Image(this.mPlugin, UtilsGameData.kRelicsAndJobs[step][tCurrJob], tIDalamudTextureWrap, pImageScaling: tIconHeight / tIDalamudTextureWrap.Height * 1.53f);
                        }
                    }
                    else ImGui.Text("----- ");
                    ImGui.PopID();
                    ImGui.SameLine();
                    // Relic topic selectable
                    if (ImGui.Selectable(header, this.mCurrTopic == step))  this.mCurrTopic = step;
                }
                ImGui.PopStyleVar();

                // Configs
                // First time
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetContentRegionAvail().Y - 12f * 2 * 2.15f));
                if (ImGui.Checkbox("##rlcFirst", ref this.mPlugin.Configuration.mIsRelicFirstTime))
                {
                    this.mPlugin.Configuration.Save();
                }
                ImGui.SameLine();
                UtilsGUI.GreyText("First-time");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Tick if this is the first time this character start the relic grind.\n- This helps tailoring the guide to what user's need.");
                // Job
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.BeginCombo("##rlcJobCmb", this.mPlugin.Configuration.mRelicCurrJob.ToString()))
                {
                    foreach (Job job in UtilsGameData.kRelicValidJobs)
                    {
                        if (ImGui.Selectable(job.ToString()))
                        {
                            this.mPlugin.Configuration.mRelicCurrJob = job;
                            this.mPlugin.PLog.Debug($"> RelicSeciton: cJ={this.mPlugin.Configuration.mRelicCurrJob} j={job}");
                        }
                    }
                    ImGui.EndCombo();
                }
                else UtilsGUI.SetTooltipForLastItem("Job of the relic you are working on");

                ImGui.EndListBox();
            }
        }
        public void DrawTopic()
        {
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.3f));
            switch (this.mCurrTopic)
            {
                case RelicStep.None: this.DrawTopic_Intro(); break;
                case RelicStep.Resistance: this.DrawTopic_Resistance(); break;
                case RelicStep.ResistanceA: this.DrawTopic_ResistanceA(); break;
                case RelicStep.Recollection: this.DrawTopic_Recollection(); break;
                case RelicStep.LawsOrder: this.DrawTopic_LawsOrder(); break;
                case RelicStep.OTG_1: this.DrawTopic_OTG1(); break;
                case RelicStep.LawsOrderA: this.DrawTopic_LawsOrderA(); break;
                case RelicStep.OTG_2: this.DrawTopic_OTG2(); break;
                case RelicStep.Blades: this.DrawTopic_Blades(); break;
            }
            ImGui.PopStyleColor();
        }
        public void DrawTopic_Intro()
        {
            ImGui.PushTextWrapPos();
            ImGui.Text("- This plugin mainly serves as a checklist for your relic progress.");
            UtilsGUI.GreyText("  If you want the overall,");
            ImGui.SameLine();
            // detail
            if (UtilsGUI.SelectableLink(this.mPlugin, $"click me {(this.mGuiFlag.HasFlag(RelicSectionGuiFlag.Intro_ShowingDetail) ? "▼" : "▲")}", -1, pIsClosingPUOnClick: false, pIsAuxiLinked: false))
            {
                if (this.mGuiFlag.HasFlag(RelicSectionGuiFlag.Intro_ShowingDetail)) this.mGuiFlag &= ~RelicSectionGuiFlag.Intro_ShowingDetail;
                else this.mGuiFlag |= RelicSectionGuiFlag.Intro_ShowingDetail;
            }
            if (this.mGuiFlag.HasFlag(RelicSectionGuiFlag.Intro_ShowingDetail))
            {
                if (ImGui.BeginChild("##introDetail", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X * 2, 300), true))
                {
                    ImGui.Text("Players will be doing two things throughout the Bozja content:");
                    UtilsGUI.GreyText(
                        """
                        1. Progressing Bozja areas. (i.e. unlocking Bozja zones, raids, etc.)
                        2. Progressing Bozja relics. (i.e. unlocking new relic steps)
                        """
                    );
                    UtilsGUI.GreyText("""As such, you can think of there being 2 mini-questlines running in parallel.""");
                    ImGui.Text("You can kinda see that in the graph of the ");
                    if (this.mPlugin.mBBDataManager.mQuestChains.TryGetValue(1, out var tQuestChain_ResWp) && tQuestChain_ResWp != null)
                    {
                        ImGui.SameLine();
                        UtilsGUI.SelectableLink_QuestChain(this.mPlugin, "Resistance Weapons questline.", tQuestChain_ResWp);
                    }

                    ImGui.Separator();

                    if (ImGui.CollapsingHeader("Progressing Bozja areas:"))
                    {
                        ImGui.PushTextWrapPos();
                        UtilsGUI.GreyText("""- Collecting mettles, turning them in to level up your rank. Rinse and repeat.""");
                        ImGui.SameLine();
                        UtilsGUI.ShowHelpMarker("- Quickest way to get mettles is doing FATEs and Critical engagement.\r\n- Get the riding maps ASAP (mount speed buff). Sold for 25 clusters, by NPC Quartermaster at the base in the instance.");

                        UtilsGUI.GreyText("- The wiki site does a great job at the basics.");
                        ImGui.SameLine();
                        UtilsGUI.UrlButton("https://ffxiv.consolegameswiki.com/wiki/The_Bozjan_Southern_Front");

                        UtilsGUI.GreyText(
                            """
                            - Join a community. 
                            - They're super helpful and will make your experience much more enjoyable.
                            """
                            );
                        ImGui.SameLine();
                        UtilsGUI.ShowHelpMarker("some communities are included in the [DRS/Communities] tab in this plugin");
                        UtilsGUI.GreyText(
                            """
                        - Rank milestones: 
                                1       unlock Bozja zone 1
                                5       unlock Bozja zone 2
                                8       unlock Bozja zone 3
                                10      unlock CLL, then Delubrum Reginae, then Zadnor
                                15      unlock Delubrum Reginae Savage
                                18      unlock Zadnor zone 2
                                22      unlock Zadnor zone 3
                                25      unlock Dalriada
                        """
                            );
                        ImGui.PopTextWrapPos();
                    }
                    
                    if (ImGui.CollapsingHeader("Progressing Bozja relics:"))
                    {
                        ImGui.PushTextWrapPos();

                        UtilsGUI.GreyText("""- To progress in Bozja relics, you slso need to progress in Bozja content.""");
                        UtilsGUI.GreyText(
                            """
                            - A Bozja relic step generally has two approaches:
                                    1. Bozja approach:                    grinding content related to Bozja.
                                    2. Non-Bozja approach.           grinding content unrelated to Bozja.
                            - If you have BLU, then non-Bozja approach is preferred for some steps.

                            - If it's your first time doing Bozja relic, you will have the following changes:
                                    + Extra two steps: One-time grind 1 & 2
                                    + First step [Resistance] will not require any poetics.
                                    + Other minor stuff.
                            - Tick the [Fist Time] box at the bottom of the list on the left if it applies to you.
                            """);

                        ImGui.PopTextWrapPos();
                    }

                    ImGui.EndChild();
                }
            }

            UtilsGUI.GreyText("- Tick the [Fist Time] box at the bottom of the list on the left if it applies to you.");
            ImGui.Text(
                    """
                    - Prerequisites:
                            + Lv 80+
                            + Post-ShB 5.55 MSQ: "Death Unto Dawn"
                            + Completion of quest line "Return to Ivalice".
                    """
                );
            ImGui.Text("- All steps must be done synced, and can be done using any class and any gear/weapon.");

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Separator();

            UtilsGUI.GreyText(
                            """
                            - A Bozja relic step generally has two approaches:
                                    1. Bozja approach:                    grinding content related to Bozja.
                                    2. Non-Bozja approach.           grinding content unrelated to Bozja.
                            - If you have BLU, then non-Bozja approach is preferred for some steps.

                            - If it's your first time doing Bozja relic, you will have the following changes:
                                    + Extra two steps: One-time grind 1 & 2
                                    + First step [Resistance] will not require any poetics.
                                    + Other minor stuff.
                            """);

            ImGui.PopTextWrapPos();
        }
        public void DrawTopic_Resistance()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                if (tConfig.mIsRelicFirstTime)
                {
                    ImGui.Text("1. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69370);
                    ImGui.Text("2. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69371);
                    ImGui.Text("3. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69372);
                    ImGui.Text("4. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69380);
                }
                else
                {
                    ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69381);
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, 30273, pMaxCount: 4);
                    this._textSectionEnd(); ImGui.SameLine(); ImGui.Text("|___ ");
                    ImGui.SameLine(); UtilsGUI.GreyText("Vendor:");
                    if (this.mPlugin.mBBDataManager.mVendors.TryGetValue(3, out var tVendor) && tVendor != null && tVendor.mLocation != null)
                    {
                        ImGui.SameLine();
                        ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                        UtilsGUI.LocationLinkButton(this.mPlugin, tVendor.mLocation, pDesc: $"{tVendor.mName} ({tVendor.mLocation.ToStringFull()})");
                        ImGui.PopStyleColor();
                    }
                    this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("(250 poetics each)");

                }
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_ResistanceA()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69506);
                // Tortured
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Tortured, pMaxCount: 20);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Drops from FATEs/CE in Bozja Zone 1"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Drops from FATEs in:");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("a2f1", "Sea of Clouds"); ImGui.SameLine(); this._textLoc("r2f1");
                // Sorrowful
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Sorrowful, pMaxCount: 20);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Drops from FATEs/CE in Bozja Zone 2"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Drops from FATEs in:");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("d2f1"); ImGui.SameLine(); this._textLoc("d2f3");
                // Harrowing
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Harrowing, pMaxCount: 20);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Drops from FATEs/CE in Bozja Zone 3"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Drops from FATEs in:");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("a2f2"); ImGui.SameLine(); this._textLoc("d2f2");
                ImGui.EndChild();
            }

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tHeight), true, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    UtilsGUI.GreyText("Prerequisites");
                    ImGui.EndMenuBar();
                }
                UtilsGUI.GreyText("Quests -------------------");
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69477);
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69505);
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }
        public void DrawTopic_Recollection()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69507);
                // Bitter
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Bitter, pMaxCount: 6);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Guaranteed from lv60 SYNCED dungeons. (recommending 'The Antitower')");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Guaranteed from Duty roulette: Leveling. (once a day)");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("Random drops from mobs and CEs in Bozja. (low chance)"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_LawsOrder()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69574);
                // Loathsome
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Loathsome, pMaxCount: 15);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 5) guaranteed per Castrum Lacus Litore clear"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) (low chance) per Bozja CE"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) guaranteed per any \"Crystal Tower\" raid clear");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- Syrcus Tower"); ImGui.SameLine(); this._textRecSign("Recommended for quickest");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The World of Darkness");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The Labyrinth of the Ancients");
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tHeight), true, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    UtilsGUI.GreyText("Prerequisites");
                    ImGui.EndMenuBar();
                }
                UtilsGUI.GreyText("Quests -------------------");
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69477);
                UtilsGUI.GreyText("Cleared ------------------");
                ImGui.Text("Castrum Lacus Litore");
                ImGui.Text("Delubrum Reginae");
                ImGui.EndChild();
            }
        }
        public void DrawTopic_OTG1()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69575);
                // Haunting
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Haunting, pMaxCount: 18);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 3) guaranteed per any \"Shadow of the Mhach\" raid clear");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The Void Ark"); ImGui.SameLine(); this._textRecSign("Recommended for quickest");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The Weeping City of Mhach");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- Dun Scaith");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                ImGui.TextUnformatted("(x 1) (66% chance) drops from FATEs in:");
                ImGui.PopStyleColor();
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("g3f1"); ImGui.SameLine(); this._textLoc("g3f2"); ImGui.SameLine(); this._textLoc("g3f3"); ImGui.SameLine(); this._textRecSign();
                // Vexing
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Vexatious, pMaxCount: 18);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 3) guaranteed per any \"Return to Ivalice\" raid clear");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The Royal City of Rabanastre");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The Ridorana Lighthouse");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- The Orbonne Monastery");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); 
                ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                ImGui.TextUnformatted("(x 1) (66% chance) drops from FATEs in:");
                ImGui.PopStyleColor();
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); this._textLoc("e3f1", "Ruby Sea"); ImGui.SameLine(); this._textLoc("e3f2"); ImGui.SameLine(); this._textLoc("e3f3", "Azim Steppes"); ImGui.SameLine(); this._textRecSign();

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_LawsOrderA()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69576);
                // Timeworn
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.TimeWorn, pMaxCount: 15);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) every 10 floors of Palace of the Dead");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- low chance, increase as progressing further");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 3) guaranteed per Delubrum Reginae clear");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionEnd(); ImGui.SameLine(); UtilsGUI.GreyText("- must use PF for this, or may join a community");

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();
        }
        public void DrawTopic_OTG2()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;
            float tContent1Height = 20 * 6;

            // Content 1
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            if (ImGui.BeginChild("##tr_content1", new(ImGui.GetContentRegionAvail().X / 10 * 6f, tContent1Height)))
            {
                ImGui.Text("1. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69632);
                ImGui.Text("2. Grab these 3 following quests:");
                this._textSectionCont(); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69633);
                this._textSectionCont(); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69634);
                this._textSectionCont(); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69635);
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tContent1Height), true, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {
                    UtilsGUI.GreyText("Prerequisites");
                    ImGui.EndMenuBar();
                }
                UtilsGUI.GreyText("Quests -------------------");
                UtilsGUI.SelectableLink_Quest(this.mPlugin, 69620);
                ImGui.PushTextWrapPos();
                UtilsGUI.GreyText("(which unlock Zadnor, where this step is)");
                ImGui.PopTextWrapPos();
                ImGui.EndChild();
            }

            // Content 2
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            if (ImGui.BeginChild("##tr_content2", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X * 2, tHeight - tContent1Height - ImGui.GetStyle().FramePadding.Y * 2 + 2)))
            {
                // Path options
                ImGui.Text("3. Grind by");
                ImGui.SameLine();
                if (ImGui.BeginCombo("##cmbRelicOTG2Path", tConfig.mRelicOTG2Path == 0 ? "Zadnor Fate/CE" : "Raids"))
                {
                    Vector2 tAnchor = ImGui.GetCursorScreenPos();
                    if (ImGui.Selectable("Zadnor Fate/CE")) tConfig.mRelicOTG2Path = 0;
                    ImGui.SetCursorScreenPos(tAnchor + new Vector2(90, 0));
                    UtilsGUI.GreyText("recommended until rank 25 for Dalriada");
                    if (ImGui.Selectable("Raids")) tConfig.mRelicOTG2Path = 1;
                    ImGui.EndCombo();
                }
                if (tConfig.mRelicOTG2Path == 0)
                {
                    ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                }
                ImGui.SameLine(); UtilsGUI.ShowHelpMarker("You can switch between these 2 ways to grind, but it's probably more efficient to focus on a single one.");
                // Path
                if (tConfig.mRelicOTG2Path == 0)        // zadnor
                {
                    // zone 1
                    UtilsGUI.GreyText("| Zone 1");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactAxle, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("((x 1) per Fate)");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactSpring, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("((x 2) per CE)");
                    // zone 2
                    UtilsGUI.GreyText("| Zone 2");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RealmBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("((x 1) per Fate)");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RiftBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("((x 2) per CE)");
                    // zone 3
                    UtilsGUI.GreyText("| Zone 3");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Bleak, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("((x 1) per Fate)");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Lurid, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("((x 2) per CE)");
                }
                else                                    // raids
                {
                    // alexander
                    UtilsGUI.GreyText("| Alexander");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactAxle, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) per Alexander (1, 2, 5, 6, 9, 10) clear");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.CompactSpring, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) per Alexander (3, 4, 7, 8, 11, 12) clear");
                    // omega
                    UtilsGUI.GreyText("| Omega");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RealmBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) per Omega (1, 2, 5, 6, 9, 10) clear");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RiftBook, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) per Omega (3, 4, 7, 8, 11, 12) clear");
                    // eden
                    UtilsGUI.GreyText("| Eden");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Bleak, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) per Eden (1, 2, 5, 6, 9, 10) clear");
                    this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.Memory_Lurid, pMaxCount: 30); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) per Eden (3, 4, 7, 8, 11, 12) clear");
                }
                ImGui.Text("4. Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69636);

                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }
        public void DrawTopic_Blades()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            // Content
            ImGui.PushStyleColor(ImGuiCol.Button, UtilsGUI.AdjustTransparency(UtilsGUI.Colors.BackgroundText_Grey, 0.15f));
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X * 2, tHeight)))
            {
                ImGui.Text("Q:"); ImGui.SameLine(); UtilsGUI.SelectableLink_Quest(this.mPlugin, 69637);
                // Loathsome
                this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.InventoryItemWidget(this.mPlugin, (int)UtilsGameData.LuminaItemId.RawEmotion, pMaxCount: 15);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 3) guaranteed per Dalriada clear"); ImGui.SameLine(); this._textLoc("e3ec", pUseIcon: true);
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 2) guaranteed per Delubrum Reginae clear");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) guaranteed per any lv. 70 dungeon clear");
                this._textSectionCont(); ImGui.SameLine(); this._textSectionBegin(); ImGui.SameLine(); UtilsGUI.GreyText("(x 1) every 10 floors of Heavens-on-High (low chance, increase as progress further)");
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();
        }

        private void _textSectionBegin() => ImGui.Text("|___ ");
        private void _textSectionCont() => ImGui.Text("\t\t ");
        private void _textSectionEnd() => ImGui.Text("\t\t ");
        private void _textLoc(string territoryType, string? pDesc = null, bool pUseIcon = false)
        {
            if (this._iterriTypeAndDefaultLocation.TryGetValue(territoryType, out var tLoc) && tLoc != null)
            {
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, UtilsGUI.Colors.BackgroundText_Grey);
                UtilsGUI.LocationLinkButton(this.mPlugin, tLoc, pIsTeleporting: true, pDesc: pDesc ?? tLoc.ToString(), pUseIcon: pUseIcon);
                ImGui.PopStyleColor();
            }
        }
        private void _textRecSign(string? pDesc = null)
        {
            ImGui.Text("*");
            UtilsGUI.SetTooltipForLastItem(pDesc ?? "Recommended");
        }

        public override void DrawGUIDebug() { }

        public override void Dispose() { }

        [Flags]
        public enum RelicStep
        {
            None = 0,
            Resistance = 1,
            ResistanceA = 2,
            Recollection = 4,
            LawsOrder = 8,
            OTG_1 = 16,
            LawsOrderA = 32,
            OTG_2 = 64,
            Blades = 128
        }

        [Flags]
        private enum RelicSectionGuiFlag
        {
            None = 0,
            Intro_ShowingDetail = 1
        }
    }
}
