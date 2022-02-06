using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace qptech.src.itemtransport
{
    class TabletTool:Item
    {
        ICoreClientAPI capi;
        public static ItemFilter itemfilter;
        GuiDialog gui;

        public enum enTabletMode
        {
            
            ClearFilter = 99990001,
            SetFilter = 99990002,
            CopyFilter = 99990003
        }

        public static enTabletMode tabletMode = enTabletMode.CopyFilter;
        public override void OnLoaded(ICoreAPI api)
        {
            capi = api as ICoreClientAPI;
            base.OnLoaded(api);
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            //if (capi == null ) { base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling); return; }
            if (blockSel == null)
            {
                //this is where we should open the UI?
                OpenGUIItemFilter();
                return;
            }
            
            ItemPipe conveyor = capi.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ItemPipe;
            if (conveyor == null) {
                OpenGUIItemFilter();
                return;
            }
            
            if (tabletMode == enTabletMode.ClearFilter)
            {
                ItemFilter tempfilter = new ItemFilter();
                conveyor.OnNewFilter(tempfilter);
                capi.World.PlaySoundAt(new AssetLocation("sounds/clearfilter"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null, false, 8, 1);
            }
            else if (tabletMode == enTabletMode.CopyFilter)
            {
                if (conveyor.itemfilter != null)
                {
                    
                    itemfilter = conveyor.itemfilter.Copy();
                    capi.World.PlaySoundAt(new AssetLocation("sounds/filtercopy"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null, false, 8, 1);
                }
                
            }
            else if (tabletMode == enTabletMode.SetFilter)
            {
                if (itemfilter != null)
                {
                    conveyor.OnNewFilter(itemfilter);
                    capi.World.PlaySoundAt(new AssetLocation("sounds/filterset"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null, false, 8, 1);
                }
            }
            
            
        }
        
        public virtual bool OpenGUIItemFilter()
        {
            if (capi == null) { return false; }


           
            if (gui == null)
            {
                gui = new GUITabletItemFilter(capi);
                
                gui.TryOpen();
                
                
            }
            else
            {
                gui.TryClose();
                gui = new GUITabletItemFilter(capi);

                gui.TryOpen();

            }
            return false;
            

        }
    }
}
