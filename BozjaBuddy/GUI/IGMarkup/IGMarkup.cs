using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace BozjaBuddy.GUI.IGMarkup
{
    public class IGMarkup
    {
        Queue<IGMarkupBlock> mBlocks = new Queue<IGMarkupBlock>();

        public IGMarkup(string pBlockGroup)
        {
            mBlocks = IGMarkup.ProcessBlockGroup(pBlockGroup);
        }

        public void DrawGUI()
        {
            foreach (IGMarkupBlock tBlock in this.mBlocks)
            {
                tBlock.DrawGUI();
            }
        }

        public static string[] BlockSplit(string pRawText, int pCallerScope = 1)
        {
            return pRawText.Split("<" + new string('!', pCallerScope) + ">", StringSplitOptions.RemoveEmptyEntries);
        }
        public static IGMarkupBlock BlockTranslate(string pRawBlock, int pCallerScope = 1)
        {
            string[] tRes = pRawBlock.Split("<~" + new string('!', pCallerScope) + ">", StringSplitOptions.RemoveEmptyEntries);
            if (tRes.Length > 1)
            {
                switch (tRes[0].ToLower())
                {
                    case "table":
                        return new IGMarkupBlock_Table(tRes[1], pCallerScope);
                    default:
                        return new IGMarkupBlock_Text(tRes[1], pCallerScope);
                }
            }
            else return new IGMarkupBlock_Text(tRes[0], pCallerScope);
        }
        public static Queue<IGMarkupBlock> ProcessBlockGroup(string pBlockGroup, int pScope = 1)
        {
            Queue<IGMarkupBlock> tQueue = new Queue<IGMarkupBlock>();
            string[] tBlocks = BlockSplit(pBlockGroup, pScope);
            foreach (string iBlock in tBlocks)
            {
                tQueue.Enqueue(IGMarkup.BlockTranslate(iBlock, pScope));
            }
            return tQueue;
        }
    }
}
