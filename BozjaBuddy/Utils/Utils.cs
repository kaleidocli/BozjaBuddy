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
        public static string FormatThousand(int pNum)
        {
            return pNum > 999 ? $"{pNum / 1000}k" : pNum.ToString();
        }
    }
}
