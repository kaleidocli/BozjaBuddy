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
                    UtilsGUI.TextDescriptionForWidget("========== ------ ==========");
                }
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
                ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetContentRegionAvail().Y - 23 * 2.15f));
                ImGui.Checkbox("##rlcFirst", ref this.mPlugin.Configuration.mIsRelicFirstTime);
                //ImGuiComponents.ToggleButton("##rlcFirst", ref this.mPlugin.Configuration.mIsRelicFirstTime);
                ImGui.SameLine();
                UtilsGUI.TextDescriptionForWidget("First-time");
                ImGui.SameLine();
                UtilsGUI.ShowHelpMarker("Tick if this is the first time this character start the relic grind.\n- This helps tailoring the guide to what user's need.");
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
                
            }
        }
        public void DrawTopic_Intro()
        {

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
    }
}
