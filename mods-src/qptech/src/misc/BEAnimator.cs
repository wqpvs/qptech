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
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Client;
using qptech.src.networks;

namespace qptech.src.misc
{
    class AniTestLoader : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockEntityClass("BEAnimator", typeof(BEAnimator));
            
        }
    }
    class BEAnimator : BlockEntity
    {
        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>().animUtil; }
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.World.Side == EnumAppSide.Client)
            {
                float rotY = Block.Shape.rotateY;
                animUtil.InitializeAnimator("anitest", new Vec3f(0, rotY, 0));
                RegisterGameTickListener(OnClientGameTick, 50);

            }

        }
        bool startani = true;
        double nextaniswitchat = 0;
        string anicode = "animation1";
        void OnClientGameTick(float df)
        {

            //bool active = animUtil.activeAnimationsByAnimCode.ContainsKey("teleport");
            
            if (startani&&Api.World.ElapsedMilliseconds>nextaniswitchat)
            {
                if (anicode == "animation1") { anicode = "run"; }
                else { anicode = "animation1"; }
                var meta = new AnimationMetaData() { Animation = "Animation 1", Code = anicode, AnimationSpeed = 1, EaseInSpeed = 1, EaseOutSpeed = 2, Weight = 1, BlendMode = EnumAnimationBlendMode.Add };
                animUtil.StartAnimation(meta);
                animUtil.StartAnimation(new AnimationMetaData() { Animation = "Animation 1", Code = anicode, AnimationSpeed = 1, EaseInSpeed = 1, EaseOutSpeed = 1, Weight = 1, BlendMode = EnumAnimationBlendMode.Average });
                startani = false;
                nextaniswitchat = Api.World.ElapsedMilliseconds + 3000;
            }
            else if (Api.World.ElapsedMilliseconds > nextaniswitchat) {
                startani = true;
                nextaniswitchat = Api.World.ElapsedMilliseconds + 3000;
                animUtil.StopAnimation(anicode);
            }

        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (animUtil.activeAnimationsByAnimCode.Count > 0 || (animUtil.animator != null && animUtil.animator.ActiveAnimationCount > 0))
            {
                return true;
            }
            return false;
        }
    }
}
