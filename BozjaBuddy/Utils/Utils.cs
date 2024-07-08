using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;

namespace BozjaBuddy.Utils
{
    public class Utils
    {
        public static float GetIconResizeRatio(Plugin pPlugin, int pValue) => pPlugin.Configuration.STYLE_ICON_SIZE / pValue;
        public static Vector2 ResizeToIcon(Plugin pPlugin, float pWidth, float pHeight)
        {
            return new Vector2(pWidth * (pPlugin.Configuration.STYLE_ICON_SIZE / pWidth), 
                                pHeight * (pPlugin.Configuration.STYLE_ICON_SIZE / pHeight));
        }
        public static Vector2 ResizeToIcon(Plugin pPlugin, IDalamudTextureWrap tTexture)
        {
            return Utils.ResizeToIcon(pPlugin, tTexture.Width, tTexture.Height);
        }
        public static string FormatNum(int pNum, int pDivisor = 1000, int pThreshold = 999, bool pShorter = false)
        {
            return pNum > (pThreshold * pDivisor + pThreshold)
                   ? ((float)pNum / (pDivisor * pDivisor)).ToString(pShorter ? "N0" : "N2") + "M"
                   : pNum > pThreshold 
                     ? ((float)pNum / pDivisor).ToString(pShorter ? "N0" : "N2") + "K"
                     : pNum.ToString();
        }
        public static DateTime ProcessToLocalTime(DateTime pDateTime)
        {
            return DateTime.UtcNow.AddSeconds(Math.Round((pDateTime - DateTime.UtcNow).TotalSeconds, MidpointRounding.ToNegativeInfinity)).ToLocalTime();
        }
        public static Vector4 RGBAtoVec4(int R, int G, int B, int A)
        {
            return new Vector4((float)R / 255, (float)G / 255, (float)B / 255, (float)A / 255);
        }

        // https://github.com/Ottermandias/GatherBuddy/blob/d2965b24c1693aae53fcfe859d3afaa8ebd16836/GatherBuddy/SeFunctions/Teleporter.cs
        public static unsafe bool IsAttuned(Plugin pPlugin, uint aetheryteId)
        {
            var teleport = Telepo.Instance();
            if (teleport == null)
            {
                pPlugin.PLog.Warning("Could not check attunement: Telepo is missing.");
                return false;
            }

            if (pPlugin.ClientState.LocalPlayer == null)
                return true;
            teleport->UpdateAetheryteList();

            var endPtr = teleport->TeleportList.Last;
            for (var it = teleport->TeleportList.First; it != endPtr; ++it)
            {
                if (it->AetheryteId == aetheryteId)
                    return true;
            }

            return false;
        }
        // https://github.com/Ottermandias/GatherBuddy/blob/d2965b24c1693aae53fcfe859d3afaa8ebd16836/GatherBuddy/SeFunctions/Teleporter.cs
        public static unsafe bool Teleport(Plugin pPlugin, uint aetheryteId)
        {
            if (IsAttuned(pPlugin, aetheryteId))
            {
                Telepo.Instance()->Teleport(aetheryteId, 0);
                return true;
            }

            pPlugin.PLog.Warning($"Teleport failed. (aetheryteId={aetheryteId})");
            return false;
        }

        public enum NodeTagPrefix
        {
            SYS = 1000,
            USER = 1001
        }
    }
}
