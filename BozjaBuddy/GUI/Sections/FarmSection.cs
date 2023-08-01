using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BozjaBuddy.GUI.Sections.RelicSection;
using BozjaBuddy.Utils;
using Dalamud.Logging;

namespace BozjaBuddy.GUI.Sections
{
    internal class FarmSection : Section, IDisposable
    {
        private readonly float kSectionHeight;

        protected override Plugin mPlugin { get; set; }
        private FarmTopic mCurrTopic = FarmTopic.Cluster;
        private Dictionary<FarmTopic, string> mTopicHeaders = new()
        {
            { FarmTopic.Cluster, "Cluster" }
        };
        private List<FarmTopic> mOrderedTopics = new()
        {
            FarmTopic.Cluster
        };

        public FarmSection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.kSectionHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.5f) * 2; // default=(15 + 3.5f)*1
        }
        public override bool DrawGUI()
        {
            var tHeight = this.mPlugin.TEXT_BASE_HEIGHT * (15 + 3.48f);
            ImGui.BeginChild("##rlcSectionTpcList", new Vector2(ImGui.GetWindowWidth() / 10, tHeight < this.kSectionHeight
                                                                                            ? tHeight
                                                                                            : this.kSectionHeight));
            this.DrawTopicList();
            ImGui.EndChild();
            ImGui.SameLine();
            ImGui.BeginChild("##rlcSectionTpc", new Vector2(
                ImGui.GetWindowWidth() / 10 * 9 - ImGui.GetStyle().WindowPadding.X * 2, tHeight < this.kSectionHeight
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
                foreach (var topic in this.mOrderedTopics)
                {
                    if (!this.mTopicHeaders.TryGetValue(topic, out var header)) continue;
                    if (ImGui.Selectable(header, this.mCurrTopic == topic)) this.mCurrTopic = topic;
                }
                ImGui.EndListBox();
            }
        }
        public void DrawTopic()
        {
            switch (this.mCurrTopic)
            {
                case FarmTopic.Cluster: this.DrawTopic_Cluster(); break;
                default: break;
            }
        }
        public void DrawTopic_Cluster()
        {
            UtilsGUI.GreyText("From PEBE's Discord");
            UtilsGUI.DrawImgFromDb(this.mPlugin, "farm_cluster_2.png", pIsScaledToRegionWidth: true, pExtraScaling: 1f);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "farm_cluster_1.png", pIsScaledToRegionWidth: true, pExtraScaling: 1f);
            UtilsGUI.DrawImgFromDb(this.mPlugin, "farm_cluster_3.png", pIsScaledToRegionWidth: true, pExtraScaling: 1f);
        }


        public override void DrawGUIDebug() { }

        public override void Dispose() { }

        private enum FarmTopic
        {
            None = 0,
            Cluster = 1,
            Sprite = 2
        }
    }
}
