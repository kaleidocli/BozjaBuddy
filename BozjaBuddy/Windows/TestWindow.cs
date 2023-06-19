using BozjaBuddy.GUI.NodeGraphViewer;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace BozjaBuddy.Windows
{
    internal class TestWindow : Window, IDisposable
    {
        public static string kHandle = "Testing - Bozja Buddy";

        private Plugin mPlugin;
        private NodeGraphViewer mGraphViewer = new();

        public TestWindow(Plugin plugin) : base("Testing - Bozja Buddy")
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 290),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
            this.SizeCondition = ImGuiCond.Once;

            this.mPlugin = plugin;
        }

        public override void Draw()
        {
            this.mGraphViewer.Draw();
        }

        public void Dispose()
        {
            this.mGraphViewer.Dispose();
        }
    }
}
