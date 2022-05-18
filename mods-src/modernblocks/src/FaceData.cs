using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace modernblocks.src
{
    public class FaceData
    {

        //these are just helper constants going to get crazy otherwise
        const int n_ = 1; const int e_ = 2; const int s_ = 4; const int w_ = 8;


        public BlockFacing facing;
        public float cellsize = 0.25f; //how many
        public int ucell = 3;
        public int vcell = 0;
        public byte[] rgba = { 255, 255, 255, 255 };
        public FaceData(BlockFacing facing)
        {
            this.facing = facing;
        }
        const int csz = 4; //size of cells in graphic must be 4 for connected faces to work
        //This wretched looking dictionary is just a cross reference tool for UV cells depending on what areas are open
        //  the "u" and "v" are packed into a single value based on a cell size of 4
        //  Note while this uses cardinal directions, it is not equal to world directions, and could be
        //  considered to be up/down/left/right instead
        static Dictionary<int, int> cftocell4 = new Dictionary<int, int>()
        {
            {0,1+csz },{n_,1 },{e_,2+csz},{n_+e_,2},
            {s_,1+csz*2},{s_+n_,3+csz*2},{s_+e_,2+csz*2},{s_+n_+e_,1+3*csz},
            {w_,csz*1},{w_+n_,0 },{w_+e_,3+csz*1},{w_+n_+e_,3+3*csz},
            {w_+s_,csz*2},{w_+s_+n_,csz*3 },{e_+s_+w_,2+2*csz},{n_+e_+s_+w_,1+csz }

        };
        /// <summary>
        /// Accepts list of neighbors and selects the appropriate cell
        /// Cell Layout:
        ///  NW| N | NE| C 
        ///  W | O | E | EW 
        ///  SW| S | SE| NS
        /// NSW|NES|ESW|NEW
        /// 
        /// This does not update the mesh, that must be called separately!
        /// 
        /// </summary>
        /// <param name="connectedneighbors"></param>
        public void SetConnectedTextures(BlockFacing[] connectedneighbors)
        {
            if (cellsize != 0.25f || connectedneighbors == null || connectedneighbors.Length == 0) //Closed Cell
            {
                ucell = 3;
                vcell = 0;
                return;
            }
            int nhash = 0;
            if (facing == BlockFacing.UP)
            {
                if (!connectedneighbors.Contains(BlockFacing.NORTH)) { nhash += (int)n_; }
                if (!connectedneighbors.Contains(BlockFacing.EAST)) { nhash += (int)e_; }
                if (!connectedneighbors.Contains(BlockFacing.SOUTH)) { nhash += (int)s_; }
                if (!connectedneighbors.Contains(BlockFacing.WEST)) { nhash += (int)w_; }
            }
            ucell = cftocell4[nhash] % csz;
            vcell = cftocell4[nhash] / csz;


        }
    }
}
