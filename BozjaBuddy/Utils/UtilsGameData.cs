using BozjaBuddy.Data;
using BozjaBuddy.GUI.Sections;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Utils
{
    public class UtilsGameData
    {
        public readonly static Dictionary<Role, List<Job>> kJobToRole = new Dictionary<Role, List<Job>>()
        {
            { Role.Tank, new List<Job> { Job.WAR, Job.PLD, Job.GNB, Job.DRK, Job.GLA, Job.MRD } },
            { Role.Healer, new List<Job> { Job.WHM, Job.SGE, Job.SCH, Job.AST, Job.CNJ } },
            { Role.Caster, new List<Job> { Job.BLM, Job.SMN, Job.RDM, Job.THM, Job.ACN } },
            { Role.Melee, new List<Job> { Job.MNK, Job.DRG, Job.SAM, Job.NIN, Job.PGL, Job.LNC, Job.ROG, Job.RPR } },
            { Role.Range, new List<Job> { Job.BRD, Job.DNC, Job.MCH, Job.ARC } }
        };
        public readonly static HashSet<Job> kValidJobs = new() { Job.WAR, Job.PLD, Job.GNB, Job.DRK,
                                                                Job.WHM, Job.SGE, Job.SCH, Job.AST,
                                                                Job.BLM, Job.SMN, Job.RDM,
                                                                Job.MNK, Job.DRG, Job.SAM, Job.NIN, Job.RPR,
                                                                Job.BRD, Job.DNC, Job.MCH,
                                                                Job.ALL};
        public static HashSet<Job> kRelicValidJobs = UtilsGameData.kValidJobs
                                                                .Where(j => j != Job.NONE && j != Job.ALL && j != Job.RPR && j != Job.SGE)
                                                                .Select(o => o)
                                                                .ToHashSet<Job>();
        private static readonly Dictionary<Job, int> kJobIconIds = new()
        {
            { Job.WAR, 62121 },
            { Job.PLD, 62119 },
            { Job.GNB, 62137 },
            { Job.DRK, 62132 },
            { Job.WHM, 62124 },
            { Job.SGE, 62140 },
            { Job.SCH, 62128 },
            { Job.AST, 62133 },
            { Job.BLM, 62125 },
            { Job.SMN, 62127 },
            { Job.RDM, 62135 },
            { Job.MNK, 62120 },
            { Job.DRG, 62122 },
            { Job.SAM, 62134 },
            { Job.NIN, 62130 },
            { Job.BRD, 62123 },
            { Job.DNC, 62138 },
            { Job.RPR, 62139 },
            { Job.MCH, 62131 }
        };
        private static readonly Dictionary<Role, int> kRoleIconIds = new()
        {
            { Role.Tank, 62581 },
            { Role.Healer, 62582 },
            { Role.Melee, 62584 },
            { Role.Range, 62586 },
            { Role.Caster, 62587 },
            { Role.None, 62574 }
        };
        public static TextureCollection? kTextureCollection = null;
        public static TextureCollection? kTexCol_LostAction = null;
        public static TextureCollection? kTexCol_FieldNote = null;

        public static ImFontPtr kFont_Yuruka = null;

        // dumpster
        /// <summary>
        /// 0, 1: DR/DRS   2, 3: Bozja/Zadnor/Dal/CLL
        /// </summary>
        private static Dictionary<Job, List<int>> kAutoPairingData = new()
        {
            { Job.PLD, new List<int> { 10001, 10002, 10050, 10050 } },
            { Job.WAR, new List<int> { 10001, 10002, 10050, 10050 } },
            { Job.GNB, new List<int> { 10001, 10002, 10050, 10050  } },
            { Job.DRK, new List<int> { 10001, 10002, 10050, 10050 } },
            { Job.WHM, new List<int> { 10003, 10004, 10052, 10052 } },
            { Job.SGE, new List<int> { 10003, 10004, 10052, 10052 } },
            { Job.SCH, new List<int> { 10003, 10004, 10052, 10052 } },
            { Job.AST, new List<int> { 10003, 10004, 10052, 10052  } },
            { Job.BLM, new List<int> { 10009, 10010, 10053, 10053 } },
            { Job.SMN, new List<int> { 10015, 10016, 10053, 10053 } },
            { Job.RDM, new List<int> { 10015, 10016, 10053, 10053 } },
            { Job.MNK, new List<int> { 10007, 10008, 10054, 10054 } },
            { Job.DRG, new List<int> { 10007, 10008, 10054, 10054 } },
            { Job.SAM, new List<int> { 10007, 10008, 10054, 10054 } },
            { Job.NIN, new List<int> { 10007, 10008, 10054, 10054 } },
            { Job.RPR, new List<int> { 10007, 10008, 10054, 10054 } },
            { Job.BRD, new List<int> { 10017, 10018, 10055, 10055 } },
            { Job.DNC, new List<int> { 10017, 10018, 10055, 10055 } },
            { Job.MCH, new List<int> { 10017, 10018, 10055, 10055 } },
            { Job.ALL, new List<int> { 10000, 10000, 10000, 10000 } }
        };
        private static Dictionary<int, string> kTerritoryIdsAndCode = new()
        {
            { 920, "n4b4" },        // CLL has the same Id as Bozja. Zadnor and Dalriada are in the same situation too.
            { 975, "n4b6" },
            { 936, "n4b5" },
            { 937, "n4b5_2" }
        };
        public static Dictionary<string, Location.Area> kAreaAndCode = new()
        {
            { "none", Location.Area.None },
            { "n4b4_z1", Location.Area.Bozja_Zone1 },
            { "n4b4_z2", Location.Area.Bozja_Zone2 },
            { "n4b4_z3", Location.Area.Bozja_Zone3 },
            { "n4b6_z1", Location.Area.Zadnor_Zone1 },
            { "n4b6_z2", Location.Area.Zadnor_Zone2 },
            { "n4b6_z3", Location.Area.Zadnor_Zone3 },
            { "n4b5", Location.Area.Delubrum },
            { "n4b5_2", Location.Area.DelubrumSavage },
            { "cll", Location.Area.Castrum },
            { "castrum", Location.Area.Dalriada }
        };

        private static Dictionary<RelicSection.RelicStep, Tuple<int, int>> kRelicIdRanges = new()
        {
            { RelicSection.RelicStep.Resistance, new(30228, 30245) },
            { RelicSection.RelicStep.ResistanceA, new(30767, 30784) },
            { RelicSection.RelicStep.Recollection, new(30785, 30802) },
            { RelicSection.RelicStep.LawsOrder, new(32651, 32668) },
            { RelicSection.RelicStep.LawsOrderA, new(32669, 32686) },
            { RelicSection.RelicStep.Blades, new(33462, 33479) }
        };

        public static Dictionary<RelicSection.RelicStep, Dictionary<Job, int>> kRelicsAndJobs = new();

        public static void Init(Plugin pPlugin)
        {
            UtilsGameData.kTextureCollection = new(pPlugin);
            UtilsGameData.kTextureCollection.AddTexture(
                    UtilsGameData.kJobIconIds.Values.ToList(),
                    TextureCollection.Sheet.Job
                );
            UtilsGameData.kTextureCollection.AddTexture(
                    UtilsGameData.kRoleIconIds.Values.ToList(),
                    TextureCollection.Sheet.Role
                );

            UtilsGameData.kTexCol_LostAction = new(pPlugin);
            UtilsGameData.kTexCol_LostAction.AddTextureFromItemId(pPlugin.mBBDataManager.mLostActions.Keys.ToList());
            UtilsGameData.kTexCol_FieldNote = new(pPlugin);
            UtilsGameData.kTexCol_FieldNote.AddTextureFromItemId(pPlugin.mBBDataManager.mFieldNotes.Keys.ToList(), pSheet: TextureCollection.Sheet.FieldNote);

            // Relic info initial mapping
            foreach (var idRange in UtilsGameData.kRelicIdRanges)
            {
                var step = idRange.Key;
                if (!UtilsGameData.kRelicsAndJobs.ContainsKey(step)) UtilsGameData.kRelicsAndJobs.TryAdd(step, new());
                for (int id = idRange.Value.Item1; id < idRange.Value.Item2; id++)
                {
                    Job? job = (Job?)pPlugin.mBBDataManager.mSheetItem?.GetRow(Convert.ToUInt32(id))?.ClassJobUse?.Value?.RowId;
                    if (job == null) continue;
                    UtilsGameData.kRelicsAndJobs[step].TryAdd(job.Value, id);
                }
            }
        }

        public static Job? GetUserJob(Plugin pPlugin)
        {
            if (pPlugin.ClientState.LocalPlayer == null) return null;
            var tJob = pPlugin.ClientState.LocalPlayer!.ClassJob;
            if (tJob == null || tJob.GameData == null || !Enum.IsDefined(typeof(Job), (int)tJob.Id)) return null;
            return (Job)tJob.Id switch
            {
                Job.GLA => Job.PLD,
                Job.MRD => Job.WAR,
                Job.CNJ => Job.WHM,
                Job.ROG => Job.NIN,
                Job.LNC => Job.DRG,
                Job.PGL => Job.MNK,
                Job.ARC => Job.BRD,
                Job.THM => Job.BLM,
                Job.ACN => tJob.GameData!.Role == 3 ? Job.SMN : Job.SCH,
                _ => (Job)tJob.Id
            };
        }
        public static Role? GetUserRole(Plugin pPlugin)
        {
            if (pPlugin.ClientState.LocalPlayer == null) return null;
            var tJob = pPlugin.ClientState.LocalPlayer!.ClassJob;
            if (tJob == null) return null;
            Role tRole = Role.None;
            tRole |= tJob.GameData!.Role switch
            {
                1 => Role.Tank,
                2 => Role.Melee,
                3 => tJob.GameData!.PrimaryStat == 2 ? Role.Range : Role.Caster,
                4 => Role.Healer,
                _ => ~tRole,
            };
            return tRole;
        }
        public static int? GetUserTerritoryAsId(Plugin pPlugin)
        {
            return (int)pPlugin.ClientState.TerritoryType;
        }
        public static string? GetUserTerritoryAsCode(Plugin pPlugin)
        {
            return UtilsGameData.kTerritoryIdsAndCode[(int)pPlugin.ClientState.TerritoryType];
        }
        public static (int, int)? GetRecPairingForCurrJob(Plugin pPlugin)
        {
            var tJob = UtilsGameData.GetUserJob(pPlugin);
            var tTerritoryId = UtilsGameData.GetUserTerritoryAsId(pPlugin);
            if (tJob == null || tTerritoryId == null) return null;
            int tIndex = tTerritoryId!.Value == 920 || tTerritoryId!.Value == 975 ? 2 : 0;
            return (UtilsGameData.kAutoPairingData[tJob!.Value][tIndex], UtilsGameData.kAutoPairingData[tJob!.Value][tIndex + 1]);
        }
        public static Role ConvertJobToRole(Job pJob)
        {
            foreach (Role iRole in UtilsGameData.kJobToRole.Keys)
            {
                if (UtilsGameData.kJobToRole.ContainsKey(iRole)) { return iRole; }
            }
            return Role.None;
        }
        public static TextureWrap? GetJobIcon(Job pJob)
        {
            if (!UtilsGameData.kJobIconIds.ContainsKey(pJob)) return null;
            if (UtilsGameData.kTextureCollection == null) return null;
            return UtilsGameData.kTextureCollection.GetTexture((uint)UtilsGameData.kJobIconIds[pJob], TextureCollection.Sheet.Job);
        }
        public static TextureWrap? GetRoleIcon(Role pRole)
        {
            if (!UtilsGameData.kRoleIconIds.ContainsKey(pRole)) return null;
            if (UtilsGameData.kTextureCollection == null) return null;
            return UtilsGameData.kTextureCollection.GetTexture((uint)UtilsGameData.kRoleIconIds[pRole], TextureCollection.Sheet.Role);
        }
        public static int ConvertGameIdToInternalId_LostAction(int pGameId)
        {
            switch (pGameId)
            {
                case <= 70: return pGameId + 20700;
                case <= 83: return pGameId + 22273;
                case <= 99: return pGameId + 23823;
                default: return pGameId;
            }
        }

        public static void Dispose()
        {
            UtilsGameData.kTextureCollection?.Dispose();
        }

        public enum LuminaItemId          // basically a map of item's name and lumina id, for ease of use. Only for the ones that we use a lot.
        {
            Memory_Tortured = 31573,
            Memory_Sorrowful = 31574,
            Memory_Harrowing = 31575,
            Memory_Bitter = 31576,

            Memory_Loathsome = 32956,
            Memory_Haunting = 32957,
            Memory_Vexatious = 32958,
            Memory_Bleak = 33763,
            Memory_Lurid = 33764,

            TimeWorn = 32959,
            CompactAxle = 33757,
            CompactSpring = 33758,
            RealmBook = 33759,
            RiftBook = 33760,
            RawEmotion = 33767,

            SaveThePrincess = 32855
        }
    }

    public enum Job
    {
        NONE = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
        RPR = 39,
        SGE = 40,
        ALL = 41
    }
}
