using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using questbook.src.SampleQuest;

namespace questbook.src.GUI
{
    class QuestBookPageRenderer
    {

        ICoreClientAPI api;
        IQuestBookPage myPage;
        double questIconSize = 32;
        double questSpacing = 6;
        double xtrack = 0;
        double ytrack = 0;
        public QuestBookPageRenderer(ICoreClientAPI capi, ElementBounds bounds, IQuestBookPage qbpage,GuiComposer SingleComposer) 
        { 
            myPage = qbpage;
            api = capi;
            xtrack = 10;
            ytrack = 10;
            if (myPage != null)
            {
                foreach (IQuest quest in qbpage.Quests())
                {
                    //create GEQuest Renderer
                    if (quest is SimpleQuest)
                    {
                        SimpleQuest squest = quest as SimpleQuest;
                        ElementBounds qbounds = ElementBounds.Fixed(bounds.drawX + xtrack, bounds.drawY + ytrack, questIconSize, questIconSize);
                        GEDrawTexture gdt = new GEDrawTexture(capi, qbounds, squest.texturename);
                        SingleComposer.AddDynamicCustomDraw(qbounds, gdt.OnDraw);
                        xtrack += questIconSize + questSpacing;
                    }
                }
            }
        }
    }
}
