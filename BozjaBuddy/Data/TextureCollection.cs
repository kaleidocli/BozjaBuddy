using Dalamud.Utility;
using Dalamud.Logging;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Textures;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentDragDrop.Delegates;

namespace BozjaBuddy.Data
{
    /// <summary>
    /// <para>Represents a collection of IDalamudTextureWrapp icon. Call Dispose() when no longer needed.</para>
    /// <para>Enforcing dimension on texture if given, otherwise use texture's own dimension.</para>
    /// </summary>
    public class TextureCollection : IDisposable
    {
        private Plugin mPlugin;
        private Dictionary<Sheet, Dictionary<uint, ISharedImmediateTexture?>> mIcons;
        private Dictionary<uint, ISharedImmediateTexture?> mStandardIcons = new Dictionary<uint, ISharedImmediateTexture?>();

        public TextureCollection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.mIcons = (new List<Sheet>((Sheet[])Enum.GetValues(typeof(Sheet))))
                                    .ToDictionary(o => o, o => new Dictionary<uint, ISharedImmediateTexture?>());
            this.LoadStandardIcons();
        }

        public void AddTexture(uint pIconId, Sheet pSheet = Sheet.Action)
        {
            if (this.mIcons[pSheet].ContainsKey(pIconId)) return;
            this.mIcons[pSheet][pIconId] = LoadTexture(pIconId);
        }
        public void AddTexture(List<int> pIconIds, Sheet pSheet = Sheet.Action)
        {
            foreach (int pIconId in pIconIds)
            {
                this.AddTexture(Convert.ToUInt16(pIconId), pSheet);
            }
        }

        public void AddTextureFromItemId(uint pItemId, Sheet pSheet = Sheet.Action)
        {
            uint? tIconId = this.GetIconId(pItemId, pSheet); 
            if (tIconId == null)
            {
                //PluginLog.LogDebug($"AddTextureFromItemId(): Unable to load texture with given item id {pItemId} from sheet {pSheet.ToString()}");
                return;
            }
            this.AddTexture(tIconId.Value, pSheet);
        }
        public void AddTextureFromItemId(uint pItemId, GeneralObject.GeneralObjectSalt pSalt)
        {
            this.AddTextureFromItemId(pItemId, TextureCollection.GenObjSaltToSheet(pSalt));
        }
        public void AddTextureFromItemId(List<int> pItemIds, Sheet pSheet = Sheet.Action)
        {
            foreach (int pItemId in pItemIds)
            {
                this.AddTextureFromItemId(Convert.ToUInt16(pItemId), pSheet);
            }
        }

        private ISharedImmediateTexture? LoadTexture(uint pIconId)
        {
            try
            {
                //PluginLog.LogInformation($"Loading texture iconId {pIconId}");
                TexFile? tTexFile = this.mPlugin.DataManager.GameData.GetIcon(pIconId);
                if (tTexFile == null) {
                    this.mPlugin.PLog.Warning($"Unable to load texture for {pIconId}");
                    return null; 
                }
                RawImageSpecification spec = new();
                spec.Width = tTexFile.Header.Width;
                spec.Height = tTexFile.Header.Height;
                return this.mPlugin.TextureProvider.GetFromGameIcon(new GameIconLookup() { IconId = pIconId });
            }
            catch(Exception e)
            {
                this.mPlugin.PLog.Warning($"Unable to load texture for {pIconId} - {e.Message}");
            }
            return null;
        }

        public IDalamudTextureWrap? GetTexture(uint pIconId, Sheet pSheet = Sheet.Action)
        {
            return this.mIcons[pSheet][pIconId]?.GetWrapOrEmpty();
        }
        public IDalamudTextureWrap? GetTextureFromItemId(uint pItemId, Sheet pSheet = Sheet.Action, bool pTryLoadTexIfFailed = false)
        {
            uint? tIconId = this.GetIconId(pItemId, pSheet);
            //PluginLog.LogDebug($"GetTextureFromItemId(): Loading iconId from itemId {pItemId} of from sheet {pSheet.ToString()}: {(tIconId == null ? false : tIconId)}");
            //return (tIconId != null && this.mIcons.ContainsKey(tIconId!.Value)) ? this.mIcons[tIconId!.Value] : null;
            if (tIconId != null && this.mIcons[pSheet].ContainsKey(tIconId!.Value))
            {
                //PluginLog.LogDebug($"GetTextureFromItemId(): Texture found for iconId {tIconId} in sheet {pSheet.ToString()}");
                return this.mIcons[pSheet][tIconId!.Value]?.GetWrapOrEmpty();
            }
            else if (pTryLoadTexIfFailed)
            {
                // loading
                this.AddTextureFromItemId(pItemId, pSheet);
                // Try getting the texture again
                if (tIconId != null && this.mIcons[pSheet].ContainsKey(tIconId!.Value))
                {
                    return this.mIcons[pSheet][tIconId!.Value]?.GetWrapOrEmpty();
                }
            }
            return null;
        }

        /// <summary>
        /// Slower than GetTexture(), but will still get disposed the same way GetTexture()'s return value does.
        /// </summary>
        /// <param name="pIconId"></param>
        /// <returns></returns>
        public IDalamudTextureWrap? GetTextureDirect(uint pIconId, Sheet pSheet)
        {
            if (this.mIcons[pSheet].ContainsKey(pIconId)) return this.mIcons[pSheet][pIconId]?.GetWrapOrEmpty();
            AddTexture(pIconId);
            return this.mIcons[pSheet][pIconId]?.GetWrapOrEmpty();
        }
        public IDalamudTextureWrap? GetStandardTexture(uint pIconId)
            => this.mStandardIcons.ContainsKey(pIconId) ? this.mStandardIcons[pIconId]?.GetWrapOrEmpty() : null;
        public IDalamudTextureWrap? GetStandardTexture(StandardIcon pIcon)
        {
            this.mStandardIcons.TryGetValue((uint)pIcon, out var pTex);
            if (pTex == null)
            {
                this.mStandardIcons.TryAdd((uint)pIcon, this.LoadTexture((uint)pIcon));
                this.mStandardIcons.TryGetValue((uint)pIcon, out var pTex2);
                return pTex2?.GetWrapOrEmpty();
            }
            return pTex?.GetWrapOrEmpty();
        }

        public void RemoveTextureFromItemId(uint pItemId, Sheet pSheet = Sheet.Action)
        {
            uint? tIconId = this.GetIconId(pItemId, pSheet);
            if (tIconId == null) { this.mPlugin.PLog.Warning($"Unable to find Texture for itemId {pItemId} (maybe a standardTexuture?). Aborting disposing..."); return; }
            //PluginLog.LogInformation($"Disposing itemId {pItemId}");
            if (this.mIcons[pSheet].ContainsKey(tIconId.Value))
            {
                //this.mIcons[pSheet][tIconId.Value]?.Dispose();
                this.mIcons[pSheet].Remove(tIconId.Value);
            }
        }
        public void RemoveTextureFromItemId(uint pItemId, GeneralObject.GeneralObjectSalt pSalt)
        {
            this.RemoveTextureFromItemId(pItemId, TextureCollection.GenObjSaltToSheet(pSalt));
        }
        public void RemoveTextureFromItemId(List<int> pItemIds, GeneralObject.GeneralObjectSalt pSalt)
        {
            foreach (int iItemId in pItemIds)
            {
                this.RemoveTextureFromItemId(Convert.ToUInt32(iItemId), pSalt);
            }
        }
        public void RemoveTextureFromItemId(Dictionary<int,int>.KeyCollection pItemIds, GeneralObject.GeneralObjectSalt pSalt)
        {
            foreach (int iItemId in pItemIds)
            {
                this.RemoveTextureFromItemId(Convert.ToUInt32(iItemId), pSalt);
            }
        }

        private uint? GetIconId(uint pItemId, Sheet pSheet)
        {
            switch (pSheet)
            {
                case Sheet.Action: 
                    return Convert.ToUInt32(this.mPlugin.mBBDataManager.mSheetAction?.GetRow(pItemId)?.Icon);
                case Sheet.Item:
                    return Convert.ToUInt32(this.mPlugin.mBBDataManager.mSheetItem?.GetRow(pItemId)?.Icon);
                case Sheet.FieldNote:
                    return Convert.ToUInt32(this.mPlugin.mBBDataManager.mSheetMycWarResultNotebook?.GetRow(pItemId)?.Icon);
                default:
                    //PluginLog.LogDebug($"GetIconId(): Unable to load iconId of {pItemId} from sheet {pSheet.ToString()}");
                    return null;
            }
        }

        public void LoadStandardIcons()
        {
            int[] tTemp = new int[] { 
                (int)Fate.FateType.Fate, (int)Fate.FateType.CriticalEngagement, (int)Fate.FateType.Duel, (int)Fate.FateType.Raid,
                (int)Mob.MobType.Normal, (int)Mob.MobType.Legion, (int)Mob.MobType.Sprite, (int)Mob.MobType.Ashkin, (int)Mob.MobType.Boss
            };
            foreach (uint iconId in tTemp)
                this.mStandardIcons[Convert.ToUInt32(iconId)] = this.LoadTexture(Convert.ToUInt32(iconId));
        }

        public void Dispose()
        {
            foreach (uint iId in this.mIcons.Keys)
            {
                this.RemoveTextureFromItemId(iId);
            }
            this.mIcons.Clear();
            // ISharedImmediateTexture cannot be disposed.
            //foreach (ISharedImmediateTexture? iIconTexture in this.mStandardIcons.Values)
            //{
            //    iIconTexture?.Dispose();
            //}
            this.mStandardIcons.Clear();
        }

        public static Sheet GenObjSaltToSheet(GeneralObject.GeneralObjectSalt pSalt)
        {
            switch (pSalt)
            {
                case GeneralObject.GeneralObjectSalt.LostAction: return Sheet.Action;
                case GeneralObject.GeneralObjectSalt.Fragment: return Sheet.Item;
                default: return Sheet.None;
            }
        }

        public enum Sheet
        {
            None = 0,
            Action = 1,
            Item = 2,
            Job = 3,
            Role = 4,
            FieldNote = 5
        }
        public enum StandardIcon
        {
            None = 0,
            Uses = 1,
            Weight = 2,
            Cast = 3,
            Recast = 4,
            Rarity = 5,
            Gil = 65002,
            Exp = 65001,
            Mettle = 65081,
            Poetic = 65023,
            Cluster = 65082,
            Fate = 63914,
            BozjaCe = 63909,
            BozjaDuel = 63910,
            BozjaDungeon = 63912,
            QuestMSQ = 61432,
            QuestSide = 61431,
            QuestRepeatable = 61433,
            QuestKey = 61439
        }
    }
}
