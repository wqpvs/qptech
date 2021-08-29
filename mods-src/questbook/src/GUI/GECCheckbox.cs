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

namespace questbook.src.GUI
{
    class GECCheckbox:GuiElementControl
    {
        ICoreClientAPI capi;
        Vintagestory.API.Common.Action handler;
        string uncheckedtexture;
        string checkedtexture;
        bool ischecked=false;
        string UseTexture => ischecked ? checkedtexture : uncheckedtexture;
        public override bool Focusable => true;
        ElementBounds bounds;
        
        public GECCheckbox(ICoreClientAPI capi, ElementBounds bounds, string uncheckedtexture,string checkedtexture, Vintagestory.API.Common.Action handler, bool ischecked) : base(capi, bounds)
        {
            this.capi = capi;
            this.handler = handler;
            this.uncheckedtexture = uncheckedtexture;
            this.checkedtexture = checkedtexture;
            this.ischecked = ischecked;
            this.bounds = bounds;
        }
        

        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            
            //CompositeTexture tex = liquidSlot.Itemstack.Collectible.Attributes?["waterTightContainerProps"]?["texture"]?.AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:" + UseTexture));

            if (tex != null)
            {
                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
                ImageSurface mysurface = new ImageSurface("C:\\Users\\quent\\source\\repos\\wqpvs\\qptech\\mods\\questbook\\assets\\game\\textures\\gui\\bubblewindow.png");
                
                Bounds.CalcWorldBounds();
                ctx.Save();
                
                ctx.SetSourceSurface(mysurface, 0,0);
                ctx.Rectangle(Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight);
                ctx.Fill();
                ctx.Restore();
                mysurface.Dispose();
            }
        }

        public override void RenderInteractiveElements(float deltaTime)
        {

        }
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            handler?.Invoke();
            api.Gui.PlaySound("toggleswitch");
        }

    }
}
