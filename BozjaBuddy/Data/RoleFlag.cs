using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Data
{
    public class RoleFlag
    {
        public static int ROLE_COUNT = 5;

        public Role mRoleFlagBit { get; set; } = Role.None;
        public bool[] mRoleFlagArray = {false, false, false, false, false};

        /// <summary>
        /// Only used once, during DB importing
        /// </summary>
        public RoleFlag() { }

        public RoleFlag(int pValue) {
            this.mRoleFlagBit = IntToFlag(pValue);
        }

        public override string ToString()
        {
            return FlagToString(mRoleFlagBit);
        }

        public void UpdateRoleFlagArray()
        {
            if (this.mRoleFlagArray[0]) this.mRoleFlagBit |= Role.Tank;
            else this.mRoleFlagBit &= ~Role.Tank;

            if (this.mRoleFlagArray[1]) this.mRoleFlagBit |= Role.Healer;
            else this.mRoleFlagBit &= ~Role.Healer;

            if (this.mRoleFlagArray[2]) this.mRoleFlagBit |= Role.Melee;
            else this.mRoleFlagBit &= ~Role.Melee;

            if (this.mRoleFlagArray[3]) this.mRoleFlagBit |= Role.Range;
            else this.mRoleFlagBit &= ~Role.Range;

            if (this.mRoleFlagArray[4]) this.mRoleFlagBit |= Role.Caster;
            else this.mRoleFlagBit &= ~Role.Caster;
        }

        public static Role IntToFlag(int pRole)
        {
            Role tRes = Role.None;

            // THMRC
            if (pRole % 100000 / 10000 == 1) tRes |= Role.Tank;
            if (pRole % 10000 / 1000 == 1) tRes |= Role.Healer;
            if (pRole % 1000 / 100 == 1) tRes |= Role.Melee;
            if (pRole % 100 / 10 == 1) tRes |= Role.Range;
            if (pRole % 10 / 1 == 1) tRes |= Role.Caster;

            return tRes;
        }

        public static string FlagToString(Role pRole)
        {
            string tRes = String.Empty;

            // THMRC
            tRes += pRole.HasFlag(Role.Tank) ? "T" : "_";
            tRes += pRole.HasFlag(Role.Healer) ? "H" : "_";
            tRes += pRole.HasFlag(Role.Melee) ? "M" : "_";
            tRes += pRole.HasFlag(Role.Range) ? "R" : "_";
            tRes += pRole.HasFlag(Role.Caster) ? "C" : "_";

            return tRes;
        }
    }

    [Flags]
    public enum Role : short
    {
        None = 0,
        Tank = 1,
        Healer = 2,
        Melee = 4,
        Range = 16,
        Caster = 256
    }
}
