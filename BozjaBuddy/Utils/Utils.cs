using ImGuiScene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Utils
{
    public class Utils
    {
        public static Vector2 ResizeToIcon(Plugin pPlugin, float pWidth, float pHeight)
        {
            return new Vector2(pWidth * (pPlugin.Configuration.STYLE_ICON_SIZE / pWidth), 
                                pHeight * (pPlugin.Configuration.STYLE_ICON_SIZE / pHeight));
        }
        public static Vector2 ResizeToIcon(Plugin pPlugin, TextureWrap tTexture)
        {
            return Utils.ResizeToIcon(pPlugin, tTexture.Width, tTexture.Height);
        }
        public static string FormatThousand(int pNum, int pDivisor = 1000, int pThreshold = 999)
        {
            return pNum > pThreshold ? $"{pNum / pDivisor}k" : pNum.ToString();
        }
        public static DateTime ProcessToLocalTime(DateTime pDateTime)
        {
            return DateTime.UtcNow.AddSeconds(Math.Round((pDateTime - DateTime.UtcNow).TotalSeconds, MidpointRounding.ToNegativeInfinity)).ToLocalTime();
        }
        public static Vector4 RGBAtoVec4(int R, int G, int B, int A)
        {
            return new Vector4((float)R / 255, (float)G / 255, (float)B / 255, (float)A / 255);
        }
    }
}
