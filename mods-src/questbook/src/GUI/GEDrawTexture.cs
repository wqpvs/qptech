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
    class GEDrawTexture : GuiElementTextBase
    {
        ICoreClientAPI api;
        string texturename;
        public static double scalefactor = 1.145;
        public GEDrawTexture(ICoreClientAPI capi, ElementBounds bounds, string texturename) : base(capi, "", CairoFont.WhiteDetailText(), bounds)
        {
            this.texturename = texturename;

            api = capi;
        }
        public void OnDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {

            ctx.Rectangle(0, 0, currentBounds.InnerWidth, currentBounds.InnerHeight);
            //CompositeTexture tex = liquidSlot.Itemstack.Collectible.Attributes?["waterTightContainerProps"]?["texture"]?.AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            CompositeTexture tex = new CompositeTexture(new AssetLocation("game:" + texturename));

            if (tex != null)
            {
                ctx.Save();
                Matrix m = ctx.Matrix;

                m.Scale(GuiElement.scaled(scalefactor), GuiElement.scaled(scalefactor));

                ctx.Matrix = m;


                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");

                //GuiElement.fillWithPattern(api, ctx, loc.Path, true, false);
                GuiElement.fillWithPattern(api, ctx, loc.Path, true, false);

                ctx.Restore();
            }
        }
    }
}
