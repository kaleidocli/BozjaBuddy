using ImGuiNET;
using BozjaBuddy.Data;
using BozjaBuddy.GUI;

namespace BozjaBuddy.Filter.LostActionTableSection
{
    internal class FilterRole : Filter
    {
        public override string mFilterName { get; set; } = "role";
        private new RoleFlag mCurrValue = default!;
        public bool mIsCompact = true;

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
        }
        public override void ClearInputValue()
        {
            this.mCurrValue.SetRoleFlagBit(Role.None);
        }

        public override bool CanPassFilter(LostAction pLostAction)
            => !mIsFilteringActive | pLostAction.mRole.mRoleFlagBit.HasFlag(mCurrValue.mRoleFlagBit);
        public override bool CanPassFilter(Fragment pFragment) => true;
        public override bool IsFiltering() => this.mCurrValue.mRoleFlagBit != Role.None;
        public override void ResetCurrValue() { this.mCurrValue.SetRoleFlagBit(Role.None); }

        public override void DrawFilterGUI()
        {
            if (!this.mIsCompact)
            {
                this.mGUI.HeaderRoleIconButtons(this.mCurrValue, this);
                return;
            }

            if (ImGui.Selectable(mFilterName.ToUpper(),
                    true,
                    ImGuiSelectableFlags.None,
                    new System.Numerics.Vector2(ImGui.GetColumnWidth() - GUIFilter.HEADER_TEXT_FIELD_SIZE_OFFSET, ImGui.GetFontSize())))
            {
                ImGui.OpenPopup("popup");
            }
            if (ImGui.BeginPopup("popup"))
            {
                this.mGUI.HeaderRoleIconButtons(this.mCurrValue, this);
                ImGui.EndPopup();
            }
        }
        public void SetCurrValue(RoleFlag pRoleFlag)
        {
            this.mCurrValue = pRoleFlag;
        }

        public override string GetCurrValue()
        {
            return mCurrValue.ToString();
        }
    }
}
