using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace qptech.src
{
    class BEERecipeProcessor:BEEBaseDevice
    {
        protected static List<MachineRecipe> recipes;
        public static List<MachineRecipe> Recipes => recipes;
        
        public static void LoadRecipes(ICoreAPI api)
        {
            if (recipes == null)
            {
                recipes = api.Assets.TryGet("qptech:config/machinerecipes.json").ToObject<List<MachineRecipe>>();
            }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            LoadRecipes(api);
            if (Block.Attributes != null)
            {

            }
        }
    }
    class MachineRecipe
    {
        public string name;
        public int temperature;
        public double processingticks;
        public MachineRecipeItems[] ingredients;
        public MachineRecipeItems[] output;
        public MachineRecipe() { }
    }
    class MachineRecipeItems
    {
        string[] validitems;
        int quantity;
        public MachineRecipeItems() { }
    }
    
}
