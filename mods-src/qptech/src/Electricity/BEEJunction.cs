using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace qptech.src
{
    class BEEJunction : BEElectric
    {
        bool isInsulated = false;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Block == null) { return; }
            if (Block.Attributes == null) { return; }
            isInsulated= Block.Attributes["isInsulated"].AsBool(isInsulated);

        }
        public override void OnTick(float par)
        {
            base.OnTick(par);
            
        }

        public virtual void EntityCollide(Entity entity)
        {
            //if (!isInsulated && IsPowered ) { entity.IsOnFire = true; }
        }

    }
}
