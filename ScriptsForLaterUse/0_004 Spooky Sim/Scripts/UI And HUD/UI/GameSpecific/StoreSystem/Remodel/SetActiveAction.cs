using System.Collections.Generic;
using UnityEngine;

namespace Remodeling.Data.Actions
{
    [CreateAssetMenu(menuName = "Remodel/Actions/Set Active")]
    public class SetActiveAction : RemodelAction
    {
        public GameObject[] enable;
        public GameObject[] disable;

        public override IRemodelUndo Apply(RemodelContext ctx)
        {
            var previous = new List<(GameObject go, bool wasActive)>();

            void TrackAndSet(GameObject go, bool target)
            {
                if (!go) return;
                previous.Add((go, go.activeSelf));
                go.SetActive(target);
            }

            if (disable != null)
                foreach (var go in disable) TrackAndSet(go, false);

            if (enable != null)
                foreach (var go in enable) TrackAndSet(go, true);

            return new RemodelUndo(() =>
            {
                for (int i = previous.Count - 1; i >= 0; i--)
                {
                    var (go, wasActive) = previous[i];
                    if (go) go.SetActive(wasActive);
                }
            });
        }
        public override IRemodelUndo Apply(RemodelContext ctx, Transform deliveryPoint)
        {
            var previous = new List<(GameObject go, bool wasActive)>();

            void TrackAndSet(GameObject go, bool target)
            {
                if (!go) return;
                previous.Add((go, go.activeSelf));
                go.SetActive(target);
            }

            if (disable != null)
                foreach (var go in disable) TrackAndSet(go, false);

            if (enable != null)
                foreach (var go in enable) TrackAndSet(go, true);

            return new RemodelUndo(() =>
            {
                for (int i = previous.Count - 1; i >= 0; i--)
                {
                    var (go, wasActive) = previous[i];
                    if (go) go.SetActive(wasActive);
                }
            });
        }
    }
}
