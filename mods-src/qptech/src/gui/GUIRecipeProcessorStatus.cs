using System;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace qptech.src
{
    class GUIRecipeProcessorStatus:GuiDialogBlockEntity
    {
        ICoreClientAPI api;
        public GUIRecipeProcessorStatus(string dialogTitle, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle,blockEntityPos,capi)
        {
            api = capi;
        }
        public void SetupDialog(BEERecipeProcessor bea)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x100 pixel box with 40 pixels top spacing for the title bar
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, 600, 600);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);
            string guicomponame = bea.Pos.ToString()+"Assembler";
            string statustext = "";
            string alertred = "<font color=\"#ffbbaa\">";//<font <color=\"#ffdddd\">>";
                        
            statustext += "<font color=\"#ffffff\">";
            if (bea.MakingRecipe != "") { statustext += "<strong>CURRENT RECIPE: " + bea.MakingRecipe + "</strong></font><br><br>"; }
            if (bea.StatusMessage != "")
            {
                statustext += bea.StatusMessage+"<br>";
            }
            
            if (bea.Recipes!=null&& bea.Recipes.Count > 0)
            {
                statustext += "<strong>Available Recipes:</strong><br>";
                foreach (MachineRecipe mr in bea.Recipes)
                {
                    statustext += "<font color=\"#aaffaa\">";
                    statustext += "<strong>";// +mr.name ;
                    //statustext += " makes ";
                    foreach (MachineRecipeItems mri in mr.output)
                    {
                        statustext += mri.quantity + " ";
                        int vi = mri.validitems.Count();
                        if (vi > 1) { statustext += "("; }
                        int c = 1;
                        foreach (string subi in mri.validitems)
                        {

                            AssetLocation al = new AssetLocation(subi);
                            string usestring = Lang.Get(al.Path);


                            statustext += usestring;

                            if (vi > 1 && c == vi - 1) { statustext += " or "; }
                            else if (vi > 1 && c != vi) { statustext += ","; }
                            c++;
                        }
                        if (vi > 1) { statustext += ")"; }
                        
                    }
                    statustext += " from</strong></font><br><font color=\"#ffffff\">";
                    foreach (MachineRecipeItems mri in mr.ingredients)
                    {
                        statustext += "   "+mri.quantity + " ";
                        int vi = mri.validitems.Count();
                        if (vi > 1) { statustext += "("; }
                        int c = 1;
                        foreach (string subi in mri.validitems)
                        {
                            AssetLocation al = new AssetLocation(subi);
                            string usestring = Lang.Get(al.Path);
                            
                            
                            statustext += usestring;
                            if (vi > 1 && c == vi - 1) { statustext += " or "; }
                            else if (vi>1 && c!=vi) { statustext += ","; }
                            c++;
                        }
                        if (vi > 1) { statustext += ")"; }
                        statustext += "<br>";
                    }
                   if (mr.processingsteps.Count() > 0)
                    {
                        statustext += "  *Requires ";
                        int c = 1;
                        foreach (string key in mr.processingsteps.Keys)
                        {
                            statustext += key + "(" + mr.processingsteps[key] + ")";
                            if (c < mr.processingsteps.Count()) { statustext += ", "; }
                            c++;
                        }
                    }
                }
                statustext += "</font></strong>";

            }
            SingleComposer = capi.Gui.CreateCompo(guicomponame, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Machine Status", OnTitleBarCloseClicked)
                .AddRichtext(statustext, CairoFont.WhiteDetailText(), textBounds)
                
                .Compose()
            ;

        }
        public override bool TryOpen()
        {
            
            return base.TryOpen();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
     

    }
}
