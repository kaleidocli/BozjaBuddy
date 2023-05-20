using Dalamud.Utility;
using Dalamud.Logging;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BozjaBuddy.Data
{
    /// <summary>
    /// <para>Represents a collection of Texturewrapp icon. Call Dispose() when no longer needed.</para>
    /// <para>Enforcing dimension on texture if given, otherwise use texture's own dimension.</para>
    /// </summary>
    public class TextureCollection : IDisposable
    {
        private Plugin mPlugin;
        public Dictionary<Sheet, Dictionary<uint, TextureWrap?>> mIcons;
        private Dictionary<uint, TextureWrap?> mStandardIcons = new Dictionary<uint, TextureWrap?>();

        public TextureCollection(Plugin pPlugin)
        {
            this.mPlugin = pPlugin;
            this.mIcons = (new List<Sheet>((Sheet[])Enum.GetValues(typeof(Sheet))))
                                    .ToDictionary(o => o, o => new Dictionary<uint, TextureWrap?>());
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

        private TextureWrap? LoadTexture(uint pIconId)
        {
            try
            {
                //PluginLog.LogInformation($"Loading texture iconId {pIconId}");
                TexFile? tTexFile = this.mPlugin.DataManager.GameData.GetIcon(pIconId);
                return tTexFile == null
                    ? null
                    : this.mPlugin.PluginInterface.UiBuilder.LoadImageRaw(tTexFile.GetRgbaImageData(),
                                                                                            Convert.ToInt32(tTexFile.Header.Width),
                                                                                            Convert.ToInt32(tTexFile.Header.Height),
                                                                                            4);
            }
            catch(Exception e)
            {
                PluginLog.LogWarning($"Unable to load texture for {pIconId} - {e.Message}");
            }
            return null;
        }

        public TextureWrap? GetTexture(uint pIconId, Sheet pSheet = Sheet.Action)
        {
            return this.mIcons[pSheet][pIconId];
        }
        public TextureWrap? GetTextureFromItemId(uint pItemId, Sheet pSheet = Sheet.Action)
        {
            uint? tIconId = this.GetIconId(pItemId, pSheet);
            //PluginLog.LogDebug($"GetTextureFromItemId(): Loading iconId from itemId {pItemId} of from sheet {pSheet.ToString()}: {(tIconId == null ? false : tIconId)}");
            //return (tIconId != null && this.mIcons.ContainsKey(tIconId!.Value)) ? this.mIcons[tIconId!.Value] : null;
            if (tIconId != null && this.mIcons[pSheet].ContainsKey(tIconId!.Value))
            {
                //PluginLog.LogDebug($"GetTextureFromItemId(): Texture found for iconId {tIconId} in sheet {pSheet.ToString()}");
                return this.mIcons[pSheet][tIconId!.Value];
            }
            return null;
        }

        /// <summary>
        /// Slower than GetTexture(), but will still get disposed the same way GetTexture()'s return value does.
        /// </summary>
        /// <param name="pIconId"></param>
        /// <returns></returns>
        public TextureWrap? GetTextureDirect(uint pIconId, Sheet pSheet)
        {
            if (this.mIcons[pSheet].ContainsKey(pIconId)) return this.mIcons[pSheet][pIconId];
            AddTexture(pIconId);
            return this.mIcons[pSheet][pIconId];
        }
        public TextureWrap? GetStandardTexture(uint pIconId)
            => this.mStandardIcons.ContainsKey(pIconId) ? this.mStandardIcons[pIconId] : null;

        public void RemoveTextureFromItemId(uint pItemId, Sheet pSheet = Sheet.Action)
        {
            uint? tIconId = this.GetIconId(pItemId, pSheet);
            if (tIconId == null) { PluginLog.Warning($"Unable to find Texture for itemId {pItemId} (maybe a standardTexuture?). Aborting disposing..."); return; }
            //PluginLog.LogInformation($"Disposing itemId {pItemId}");
            if (this.mIcons[pSheet].ContainsKey(tIconId.Value))
            {
                this.mIcons[pSheet][tIconId.Value]?.Dispose();
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
            foreach (TextureWrap? iIconTexture in this.mStandardIcons.Values)
            {
                iIconTexture?.Dispose();
            }
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
    }
}
