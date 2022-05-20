using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Util;

namespace modernblocks.src
{
    class ModernBlockLoader : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("BEConnectedTextures", typeof(BEConnectedTextures));
            api.RegisterBlockClass("BlockConnectedTexture", typeof(BlockConnectedTexture));
            api.RegisterBlockEntityClass("BEAnimatedTextures", typeof(BEAnimatedTextures));
        }
    }
}
