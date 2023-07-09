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
        private NodeGraphViewer mGraphViewer;

        public TestWindow(Plugin plugin) : base("Testing - Bozja Buddy", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(300, 290),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
            this.SizeCondition = ImGuiCond.Once;

            this.mPlugin = plugin;
            this.mGraphViewer = new(null);
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
