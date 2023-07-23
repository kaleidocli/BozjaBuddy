using BozjaBuddy.Utils;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Logging;
using ImGuiScene;

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

        public RelicSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.kSectionHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.5f) * 2; // default=(15 + 3.5f)*1
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
                if (this.mPlugin.Configuration.mIsRelicFirstTime)
                {
                    if (ImGui.Selectable(this.mTopicHeaders[RelicStep.None], this.mCurrTopic == RelicStep.None)) this.mCurrTopic = RelicStep.None;
                }
                if (this.mPlugin.mBBDataManager.mQuestChains.TryGetValue(1, out var tQuestChain_Bozja) && tQuestChain_Bozja != null)
                {
                    UtilsGUI.SelectableLink_QuestChain(this.mPlugin, "Bozja Questline", tQuestChain_Bozja);
                }
                ImGui.Separator();
                ImGui.Spacing();

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
                        TextureWrap? tTextureWrap = UtilsGameData.kTextureCollection?.GetTextureFromItemId(
                                                        Convert.ToUInt32(UtilsGameData.kRelicsAndJobs[step][tCurrJob]),
                                                        pSheet: Data.TextureCollection.Sheet.Item,
                                                        pTryLoadTexIfFailed: true
                                                        );
                        if (tTextureWrap != null)
                        {
                            if (UtilsGUI.SelectableLink_Image(this.mPlugin, 0, tTextureWrap, pIsLink: false, pIsAuxiLinked: false, pImageScaling: tIconHeight / tTextureWrap.Height * 1.53f, pCustomLinkSize: new(tIconHeight * 1.33f)))
                            {

                            }
                            else UtilsGUI.SetTooltipForLastItem($"Click to link item to chat.");
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
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetContentRegionAvail().Y - 36f * 2.15f));
                ImGui.Checkbox("##rlcFirst", ref this.mPlugin.Configuration.mIsRelicFirstTime);
                ImGui.SameLine();
                UtilsGUI.TextDescriptionForWidget("First-time");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Tick if this is the first time this character start the relic grind.\n- This helps tailoring the guide to what user's need.");
                // BLU
                ImGui.Checkbox("##rlcBlu", ref this.mPlugin.Configuration.mIsRelicBlu);
                ImGui.SameLine();
                UtilsGUI.TextDescriptionForWidget("BLU");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Tick if you have BLU leveled and ready for grinding.\n- This helps tailoring the guide to what user's need.");
                // Update
                if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowsSpin))
                {

                }
                else UtilsGUI.SetTooltipForLastItem("Update relic progress");
                // Job
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGui.BeginCombo("##rlcJobCmb", this.mPlugin.Configuration.mRelicCurrJob.ToString()))
                {
                    foreach (Job job in UtilsGameData.kRelicValidJobs)
                    {
                        if (ImGui.Selectable(job.ToString()))
                        {
                            this.mPlugin.Configuration.mRelicCurrJob = job;
                            PluginLog.LogDebug($"> RelicSeciton: cJ={this.mPlugin.Configuration.mRelicCurrJob} j={job}");
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
            switch (this.mCurrTopic)
            {
                case RelicStep.None: this.DrawTopic_Intro(); break;
                case RelicStep.Resistance: this.DrawTopic_Resistance(); break;
            }
        }
        public void DrawTopic_Intro()
        {
            ImGui.PushTextWrapPos();
            ImGui.Text("- This plugin mainly serves as a checklist for your relic progress.");
            UtilsGUI.TextDescriptionForWidget("  If you want the overall,");
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
                    UtilsGUI.TextDescriptionForWidget(
                        """
                        1. Progressing Bozja areas. (i.e. unlocking Bozja zones, raids, etc.)
                        2. Progressing Bozja relics. (i.e. unlocking new relic steps)
                        """
                    );
                    UtilsGUI.TextDescriptionForWidget("""As such, you can think of there being 2 mini-questlines running in parallel.""");
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
                        UtilsGUI.TextDescriptionForWidget("""- Collecting mettles, turning them in to level up your rank. Rinse and repeat.""");
                        ImGui.SameLine();
                        UtilsGUI.ShowHelpMarker("- Quickest way to get mettles is doing FATEs and Critical engagement.\r\n- Get the riding maps ASAP (mount speed buff). Sold for 25 clusters, by NPC Quartermaster at the base in the instance.");

                        UtilsGUI.TextDescriptionForWidget("- The wiki site does a great job at the basics.");
                        ImGui.SameLine();
                        UtilsGUI.UrlButton("https://ffxiv.consolegameswiki.com/wiki/The_Bozjan_Southern_Front");

                        UtilsGUI.TextDescriptionForWidget(
                            """
                            - Join a community. 
                            - They're super helpful and will make your experience much more enjoyable.
                            """
                            );
                        ImGui.SameLine();
                        UtilsGUI.ShowHelpMarker("some communities are included in the [DRS/Communities] tab in this plugin");
                        UtilsGUI.TextDescriptionForWidget(
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

                        UtilsGUI.TextDescriptionForWidget("""- To progress in Bozja relics, you slso need to progress in Bozja content.""");
                        UtilsGUI.TextDescriptionForWidget(
                            """
                            - A Bozja relic step generally has two approaches:
                                    1. Bozja approach:                    grinding content related to Bozja.
                                    2. Non-Bozja approach.           grinding content unrelated to Bozja.
                            - If you have BLU, then non-Bozja approach is preferred for some steps.

                            - If it's your first time doing Bozja relic, you will have the following changes:
                                    + Extra two steps: One-time grind 1 & 2
                                    + First step [Resistance] will not require any poetics.
                                    + Other minor stuff.
                            - Make sure to tick your [Fist Time] and [BLU preferred] at the bottom of the list on the left if those apply to you.
                            """);

                        ImGui.PopTextWrapPos();
                    }

                    ImGui.EndChild();
                }
            }

            ImGui.Text("- You can link the relic to chat by clicking the relic item image.");
            ImGui.Text("- You can update your progress by clicking the spinning arrow icon.");

            ImGui.Separator();

            UtilsGUI.TextDescriptionForWidget(
                            """
                            - A Bozja relic step generally has two approaches:
                                    1. Bozja approach:                    grinding content related to Bozja.
                                    2. Non-Bozja approach.           grinding content unrelated to Bozja.
                            - If you have BLU, then non-Bozja approach is preferred for some steps.

                            - If it's your first time doing Bozja relic, you will have the following changes:
                                    + Extra two steps: One-time grind 1 & 2
                                    + First step [Resistance] will not require any poetics.
                                    + Other minor stuff.
                            - Make sure to tick your [Fist Time] and [BLU preferred] at the bottom of the list on the left if those apply to you.
                            """);

            ImGui.PopTextWrapPos();
        }
        public void DrawTopic_Resistance()
        {
            var tConfig = this.mPlugin.Configuration;
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            tHeight = tHeight < this.kSectionHeight ? tHeight : this.kSectionHeight;

            // Content
            if (ImGui.BeginChild("##tr_content", new(ImGui.GetContentRegionAvail().X / 10 * 7.2f, tHeight)))
            {
                if (tConfig.mIsRelicFirstTime)
                {
                    ImGui.Text("Fox jumping something something");
                }
                else
                {

                }
                ImGui.EndChild();
            }

            ImGui.SameLine();

            // Prereq
            if (ImGui.BeginChild("##tr_prereq", new(ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X, tHeight), true))
            {
                ImGui.Text("aaa");
                ImGui.EndChild();
            }

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
