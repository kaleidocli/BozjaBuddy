using BozjaBuddy.GUI.IGMarkup;
using Dalamud.Game.Text.SeStringHandling;
using System.Collections.Generic;

namespace BozjaBuddy.Data
{
    /// <summary>
    /// GeneralObject is object that can be displayed in Auxiliary Viewer
    /// </summary>
    public abstract class GeneralObject
    {
        protected abstract Plugin mPlugin { get; set; }
        public abstract int mId { get; set; }
        protected abstract GeneralObjectSalt mIdSalt { get; set; }
        public virtual string mName { get; set; } = string.Empty;
        public virtual string mDetail { get; set; } = string.Empty;
        public virtual string mDescription { get; set; } = string.Empty;
        protected virtual string mUiTooltip { get; set; } = string.Empty;
        public virtual IGMarkup? mIGMarkup { get; set; } = null;
        public virtual List<int> mLinkActions { get; set; } = new List<int>();
        public virtual List<int> mLinkMobs { get; set; } = new List<int>();
        public virtual List<int> mLinkFates { get; set; } = new List<int>();
        public virtual List<int> mLinkFragments { get; set; } = new List<int>();
        public virtual List<int> mLinkVendors { get; set; } = new List<int>();
        public virtual Location? mLocation { get; set; } = null;
        public virtual System.Numerics.Vector4? mTabColor { get; set; } = null;
        public virtual bool mIsExist { get; set; } = true;

        public virtual int GetGenId() => GeneralObject.IdToGenId(this.mId, (int)this.mIdSalt);
        public virtual string GetReprName() => this.mName;
        public virtual string GetReprClipboardTooltip() => this.mDescription;
        protected virtual string GenReprUiTooltip() => " ";
        public virtual string GetReprUiTooltip() 
            => this.mUiTooltip == string.Empty ? this.GenReprUiTooltip() : this.mUiTooltip;
        public virtual string ResetReprUiTooltip() => this.mUiTooltip = string.Empty;
        public virtual Location? GetReprLocation() => this.mLocation;
        public virtual SeString? GetReprItemLink() => null;

        protected abstract void SetUpAuxiliary();

        public virtual GeneralObjectSalt GetSalt() => mIdSalt;

        public static int SaltMultiplier = 100000;
        public static int IdToGenId(int pId, int pIdSalt) => pId + (int)pIdSalt * GeneralObject.SaltMultiplier;
        public static int[] GenIdToIdAndSalt(int pGenId)
        {
            foreach (GeneralObjectSalt tSalt in new GeneralObjectSalt[] {
                                        GeneralObjectSalt.Fate,
                                        GeneralObjectSalt.Mob,
                                        GeneralObjectSalt.Fragment,
                                        GeneralObjectSalt.LostAction
                                                                        })
            {
                if (pGenId > (int)tSalt * GeneralObject.SaltMultiplier) 
                    return new int[] { pGenId - (int)tSalt * GeneralObject.SaltMultiplier, (int)tSalt };
            }
            return new int[] { pGenId - (int)GeneralObjectSalt.None, (int)GeneralObjectSalt.None };
        }

        public enum GeneralObjectSalt
        {
            None = 0,
            LostAction = 1,
            Fragment = 2,
            Mob = 3,
            Fate = 4,
            Vendor = 5,
            Loadout = 6
        }
    }
}
