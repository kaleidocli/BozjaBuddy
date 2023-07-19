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

namespace BozjaBuddy.GUI.Sections
{
    public class RelicSection : Section, IDisposable
    {
        private static Job kDefaultCurrJob = Job.PLD;
        private readonly float kSectionHeight;

        protected override Plugin mPlugin { get; set; }
        private RelicStep mCurrTopic = 0;
        private Job mCurrJob = RelicSection.kDefaultCurrJob;
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
            ImGui.BeginChild("##rlcSectionTpcList", new Vector2(ImGui.GetWindowWidth() / 10 * 2.25f, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopicList();
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("##rlcSectionTpc", new Vector2(
                ImGui.GetWindowWidth() / 10 * 7.75f - ImGui.GetStyle().WindowPadding.X * 2, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopic();
            ImGui.EndChild();
            return true;
        }
        public void DrawTopicList()
        {
            if (ImGui.BeginListBox("##rlcSectLb", ImGui.GetContentRegionAvail()))
            {
                // Get progress
                if (!this.mPlugin.Configuration.mRelicProgress.TryGetValue(this.mCurrJob, out var tProgress))
                {
                    this.mCurrJob = RelicSection.kDefaultCurrJob;
                    tProgress = this.mPlugin.Configuration.mRelicProgress[this.mCurrJob];
                }

                // Intro
                if (ImGui.Selectable(this.mTopicHeaders[RelicStep.None], this.mCurrTopic == RelicStep.None)) this.mCurrTopic = RelicStep.None;
                UtilsGUI.TextDescriptionForWidget("======== ------ ========");
                // Relic steps
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(Vector2.One.X, ImGui.GetStyle().ItemSpacing.Y));
                foreach (var step in this.mOrderedTopics)
                {
                    var header = this.mTopicHeaders[step];
                    if (step == RelicStep.None) continue;
                    // Relic progress button
                    ImGui.PushID(header);
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Check, defaultColor: tProgress.HasFlag(step) ? UtilsGUI.AdjustTransparency(UtilsGUI.Colors.Button_Green, 0.7f) : null, hoveredColor: UtilsGUI.Colors.Button_Green))
                    {
                        // Add prog and up to it
                        if (!tProgress.HasFlag(step))
                        {
                            foreach (var progToAdd in this.mOrderedTopics)
                            {
                                this.mPlugin.Configuration.mRelicProgress[this.mCurrJob] |= progToAdd;
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
                                    this.mPlugin.Configuration.mRelicProgress[this.mCurrJob] &= ~progToRemove;
                                }
                            }
                        }
                    }
                    else UtilsGUI.SetTooltipForLastItem($"Mark as {(tProgress.HasFlag(step) ? "incompleted" : "completed")}.");
                    ImGui.SameLine();
                    // Relic icon
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Comment))
                    {

                    }
                    else UtilsGUI.SetTooltipForLastItem($"Link item to chat.");
                    ImGui.PopID();
                    ImGui.SameLine();
                    // Relic topic selectable
                    if (ImGui.Selectable(header, this.mCurrTopic == step))  this.mCurrTopic = step;
                }
                ImGui.PopStyleVar();
                ImGui.EndListBox();
            }
        }
        public void DrawTopic()
        {
            switch (this.mCurrTopic)
            {
                
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
        public enum RelicType
        {
            None = 0,
            
        }
    }
}
