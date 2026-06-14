using Remodeling.Runtime;
using UnityEngine;

namespace Remodeling.Data.Actions
{
    [CreateAssetMenu(menuName = "Remodel/Actions/Set Size Tier")]
    public class SetSizeTierAction : RemodelAction
    {
        public int newTier = 1;

        public override IRemodelUndo Apply(RemodelContext ctx)
        {
            int prev = ctx.playerState.SizeTier;

            ctx.playerState.SetSizeTier(Mathf.Max(prev, newTier));
            ctx.sizeApplier.Apply(ctx.playerState.SizeTier);

            return new RemodelUndo(() =>
            {
                ctx.playerState.SetSizeTier(prev);
                ctx.sizeApplier.Apply(prev);
            });
        }
        public override IRemodelUndo Apply(RemodelContext ctx, Transform deliveryPoint)
        {
            int prev = ctx.playerState.SizeTier;

            ctx.playerState.SetSizeTier(Mathf.Max(prev, newTier));
            ctx.sizeApplier.Apply(ctx.playerState.SizeTier);

            return new RemodelUndo(() =>
            {
                ctx.playerState.SetSizeTier(prev);
                ctx.sizeApplier.Apply(prev);
            });
        }
    }
}
