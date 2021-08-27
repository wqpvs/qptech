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
        
        public GECCheckbox(ICoreClientAPI capi, ElementBounds bounds, string uncheckedtexture,string checkedtexture, Vintagestory.API.Common.Action handler, bool ischecked) : base(capi, bounds)
        {
            this.capi = capi;
            this.handler = handler;
            this.uncheckedtexture = uncheckedtexture;
            this.checkedtexture = checkedtexture;
            this.ischecked = ischecked;
        }
        public void OnDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {

            ctx.Rectangle(0, 0, currentBounds.InnerWidth, currentBounds.InnerHeight);
            //CompositeTexture tex = liquidSlot.Itemstack.Collectible.Attributes?["waterTightContainerProps"]?["texture"]?.AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:" + UseTexture));

            if (tex != null)
            {
                ctx.Save();
                Matrix m = ctx.Matrix;
                m.Scale(GuiElement.scaled(GEDrawTexture.scalefactor), GuiElement.scaled(GEDrawTexture.scalefactor));
                ctx.Matrix = m;
                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
                //GuiElement.fillWithPattern(api, ctx, loc.Path, true, false);
                GuiElement.fillWithPattern(capi, ctx, loc.Path, true, false);
                ctx.Restore();
            }
        }
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            handler?.Invoke();
            api.Gui.PlaySound("toggleswitch");
        }
        public override void OnFocusGained()
        {
            base.OnFocusGained();
        }
        public override void OnFocusLost()
        {
            base.OnFocusLost();
        }
    }
}
