using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.GUIExtension
{
    /// <summary>
    /// Represents an ImGui draw that attaches to an Addon
    /// </summary>
    internal abstract class ExtGui
    {
        public abstract string mId { get; set; }
        public abstract string mAddonName { get; set; }

        public abstract void Draw();
    }
}
