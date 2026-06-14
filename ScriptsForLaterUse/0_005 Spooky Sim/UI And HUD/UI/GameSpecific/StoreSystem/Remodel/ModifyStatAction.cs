using Remodeling.Runtime;
using UnityEngine;

namespace Remodeling.Data.Actions
{
    [CreateAssetMenu(menuName = "Remodel/Actions/Modify Stat")]
    public class ModifyStatAction : RemodelAction
    {
        public PlayerStatType stat;
        public int delta = 5;

        public override IRemodelUndo Apply(RemodelContext ctx)
        {
            if (!ctx.stats) return new RemodelUndo(null);

            ctx.stats.Add(stat, delta);

            return new RemodelUndo(() =>
            {
                if (ctx.stats) ctx.stats.Add(stat, -delta);
            });
        }


        public override IRemodelUndo Apply(RemodelContext ctx, Transform deliveryPoint)
        {
            if (!ctx.stats) return new RemodelUndo(null);

            ctx.stats.Add(stat, delta);

            return new RemodelUndo(() =>
            {
                if (ctx.stats) ctx.stats.Add(stat, -delta);
            });
        }
    }
}
