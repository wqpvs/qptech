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
    class QuestRenderer
    {
        ICoreClientAPI api;
        IQuest myQuest;
        public QuestRenderer(ICoreClientAPI capi, ElementBounds bounds, GuiComposer SingleComposer, IQuest quest)
        {
            myQuest = quest;
            if (myQuest is SimpleQuest)
            {
                SimpleQuest sq = myQuest as SimpleQuest;
                //GEDrawTexture gdt = new GEDrawTexture(capi, bounds, sq.texturename);
                ElementBounds iconbounds = ElementBounds.Fixed(bounds.fixedX, bounds.fixedY, 32, 32);
                GECCheckbox gdt = new GECCheckbox(capi, iconbounds, sq.texturename, sq.completedtexturename, ()=>OnQuestClick(sq),sq.IsComplete());
                SingleComposer.AddDynamicCustomDraw(iconbounds, gdt.OnDraw);
            }
        }
        public void OnQuestClick(IQuest quest)
        {
            quest.CheckComplete();

        }
    }
}
