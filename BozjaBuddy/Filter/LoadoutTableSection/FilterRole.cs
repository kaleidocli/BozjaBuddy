using BozjaBuddy.Data;
using BozjaBuddy.GUI;
using ImGuiNET;

namespace BozjaBuddy.Filter.LoadoutTableSection
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

        public override bool CanPassFilter(Loadout pLoadout)
            => !mIsFilteringActive | pLoadout.mRole.mRoleFlagBit.HasFlag(mCurrValue.mRoleFlagBit);

        public override void DrawFilterGUI()
        {
            if (ImGui.Selectable(mFilterName.ToUpper(),
                    true,
                    ImGuiSelectableFlags.None,
                    new System.Numerics.Vector2(ImGui.GetColumnWidth() - GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET, ImGui.GetFontSize())))
            {
                ImGui.OpenPopup("popup");
            }
            mCurrValue.UpdateRoleFlagBit();
            if (ImGui.BeginPopup("popup"))
            {
                mGUI.HeaderSelectable("T##0", ref mCurrValue.mRoleFlagArray[0]);
                mGUI.HeaderSelectable("H##1", ref mCurrValue.mRoleFlagArray[1]);
                mGUI.HeaderSelectable("M##2", ref mCurrValue.mRoleFlagArray[2]);
                mGUI.HeaderSelectable("R##3", ref mCurrValue.mRoleFlagArray[3]);
                mGUI.HeaderSelectable("C##4", ref mCurrValue.mRoleFlagArray[4]);
                ImGui.EndPopup();
            }
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
