using UnityEngine;

namespace Remodeling.Data.Actions
{
    [CreateAssetMenu(menuName = "Remodel/Actions/Spawn Prefab")]
    public class SpawnPrefabAction : RemodelAction
    {
        public GameObject prefab;
        public Transform anchor; // optional; if null, spawns under shipRoot
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 localScale = Vector3.one;
        public override bool ApplyPerPurchase => false;

        public override IRemodelUndo Apply(RemodelContext ctx)
        {
            if (!prefab) return new RemodelUndo(null);

            Transform parent = anchor ? anchor : ctx.shipRoot;
            var go = Object.Instantiate(prefab, parent);
            go.transform.localPosition = localPosition;
            go.transform.localEulerAngles = localEuler;
            go.transform.localScale = localScale;

            return new RemodelUndo(() =>
            {
                if (go) Object.Destroy(go);
            });
        }
        public override IRemodelUndo Apply(RemodelContext ctx, Transform newParent)
        {
            if (!prefab) return new RemodelUndo(null);
            DeliveryDropper dropper = ctx.shipRoot.GetComponent<DeliveryDropper>();

            Transform parent = newParent ? newParent : ctx.shipRoot;
            if(dropper != null)
            {
                dropper.Deliver(prefab);
                return new RemodelUndo(() => { });
            }
            else
            {
                var go = Object.Instantiate(prefab, parent);
                go.transform.localPosition = localPosition;
                go.transform.localEulerAngles = localEuler;
                go.transform.localScale = localScale;
                return new RemodelUndo(() =>
                {
                    if (go) Object.Destroy(go);
                });
            }

        }
    }
}
