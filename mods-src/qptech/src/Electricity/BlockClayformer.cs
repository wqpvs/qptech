using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Vintagestory.API.MathTools;

namespace qptech.src
{
    /// <summary>
    /// BlockClayformer allows for swapping out the block with other clayformer configurations
    /// using objects in the players hands as specified in qptech/config/clayformerswaps.json
    /// </summary>
    class BlockClayformer:ElectricalBlock
    {
        static Dictionary<string, string> variantlist;
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            
            //must sneak click
            if (!byPlayer.Entity.Controls.Sneak) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            //must have a relevant item
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;

            BEEClayFormer clayformer = (BEEClayFormer)api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (clayformer == null) { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
            if (stack==null) {
                clayformer.HaltProduction();                
            }
            else
            {
                
                clayformer.SetCurrentItem(stack.Collectible.Code.ToString());
                
            }
            return true;
        }
        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            BEEClayFormer cf = world.BlockAccessor.GetBlockEntity(pos) as BEEClayFormer;
            if (cf == null)
            {
                return base.GetPlacedBlockName(world, pos);
            }
            if (cf.CurrentRecipe != null)
            {
                return "Clayformer (" + cf.CurrentRecipe.Output.ResolvedItemstack.GetName() + ")";
            }
            return base.GetPlacedBlockName(world, pos);
        }
        static void LoadVariantList(ICoreAPI api)
        {

            //TODO Need to either swap the hardcoded path name out or figure out correct reference for the path
            //string path = Path.Combine(GamePaths.Cache, @"assets\machines\config\clayformerswaps.json");
            //string ad = AppDomain.CurrentDomain.BaseDirectory;
            //string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //TODO - extend this to have multiple classes, so you could have a list of clayformerswaps, list of metal plate swaps etc
            
            
            
           variantlist = api.Assets.TryGet("qptech:config/clayformerswaps.json").ToObject<Dictionary<string,string>>();

            if (variantlist==null) 
            {
                variantlist = new Dictionary<string, string>();
                api.World.Logger.Error("clayformerswaps was not found sowee!");
            }
            

        }
        public static string GetModCacheFolder(string modArchiveName)
        {
            var modCacheDir = new DirectoryInfo(Path.Combine(GamePaths.DataPath, "Cache", "unpack"));
            var myModCacheDir = modCacheDir.EnumerateDirectories()
                .FirstOrDefault(p => p.Name.StartsWith(modArchiveName));
            return myModCacheDir?.FullName;
        }
    }
}
