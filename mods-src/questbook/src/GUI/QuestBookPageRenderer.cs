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
        double ytextshift = 5;
        
        public QuestBookPageRenderer(ICoreClientAPI capi, ElementBounds bounds, IQuestBookPage qbpage,GuiComposer SingleComposer) 
        { 
            myPage = qbpage;
            api = capi;
            xtrack = 20;
            ytrack = 20;
            
            if (myPage != null)
            {
                foreach (IQuest quest in qbpage.Quests())
                {
                    //create GEQuest Renderer
                                                           
                    ElementBounds qbounds = ElementBounds.Fixed(bounds.drawX + xtrack, bounds.drawY + ytrack, bounds.fixedWidth, questIconSize);
                    QuestRenderer qr = new QuestRenderer(capi, qbounds, SingleComposer, quest);
                    ytrack += questIconSize + questSpacing;
                    /*GEDrawTexture gdt = new GEDrawTexture(capi, qbounds, texturename);
                    SingleComposer.AddDynamicCustomDraw(qbounds, gdt.OnDraw);
                    qbounds = ElementBounds.Fixed(xtrack + bounds.drawX + questIconSize + questSpacing, ytrack+ytextshift, questIconSize * 10, questIconSize);
                    string text = "<font color=#000000>" + quest.Name + "</font>";
                    SingleComposer.AddRichtext(text, CairoFont.WhiteDetailText(), qbounds);
                    ytrack += questIconSize + questSpacing;*/
                }
            }
        }
        
    }
}
