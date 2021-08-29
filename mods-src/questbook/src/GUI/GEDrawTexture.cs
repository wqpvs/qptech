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
    class GEDrawTexture : GuiElement
    {
        
        string texturename;
        public static double scalefactor = 1.145;
        ICoreClientAPI capi;
        ElementBounds bounds;
        public GEDrawTexture(ICoreClientAPI capi, ElementBounds bounds, string texturename) : base(capi, bounds)
        {
            this.texturename = texturename;

            this.capi = capi;
            this.bounds = bounds;
        }

        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();
            ctx.Rectangle(Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight);
            //CompositeTexture tex = liquidSlot.Itemstack.Collectible.Attributes?["waterTightContainerProps"]?["texture"]?.AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:" + texturename));

            if (tex != null)
            {
                ctx.Save();
                Matrix m = ctx.Matrix;

                //m.Scale(GuiElement.scaled(scalefactor), GuiElement.scaled(scalefactor));

                ctx.Matrix = m;


                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");

                //GuiElement.fillWithPattern(api, ctx, loc.Path, true, false);
                GuiElement.fillWithPattern(capi, ctx, loc.Path, true, false);

                ctx.Restore();
            }
        }

        
    }
}
