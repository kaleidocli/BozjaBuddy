using ImGuiNET;
using BozjaBuddy.Data;
using BozjaBuddy.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterRole : Filter
    {
        public override string mFilterName { get; set; } = "role";
        private new RoleFlag mLastValue = default!;
        private new RoleFlag mCurrValue = default!;

        public FilterRole()
        {
            Init();
        }
        public FilterRole(bool pIsFilteringActive)
        {
            Init();
            EnableFiltering(pIsFilteringActive);
        }
        protected override void Init()
        {
            mCurrValue = new RoleFlag();
            mLastValue = new RoleFlag();
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | pLostAction.mRole.mRoleFlagBit.HasFlag(mCurrValue.mRoleFlagBit);
        public override bool CanPassFilter(Fragment pFragment) => true;

        public override void DrawFilterGUI()
        {
            if (ImGui.Selectable(mFilterName.ToUpper(),
                    true,
                    ImGuiSelectableFlags.None,
                    new System.Numerics.Vector2(ImGui.GetColumnWidth() - GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET, ImGui.GetFontSize())))
            {
                ImGui.OpenPopup("popup");
            }
            if (ImGui.BeginPopup("popup"))
            {
                this.mGUI.HeaderRoleSelectables(this.mCurrValue);
                ImGui.EndPopup();
            }
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
