using UnityEngine;

namespace Cherry.Anomalies
{
    public class PuzzleAnomaly : AnomalyBase
    {
        [SerializeField] private PuzzleController puzzle;

        protected override void Activate_Internal()
        {
            if (!puzzle) return;
            puzzle.gameObject.SetActive(true);
            puzzle.OnSolved += HandleSolved;
        }

        protected override void Deactivate_Internal()
        {
            if (!puzzle) return;
            puzzle.OnSolved -= HandleSolved;
            puzzle.gameObject.SetActive(false);
        }

        private void HandleSolved()
        {
            Resolve();
        }
    }

    public class PuzzleController : MonoBehaviour
    {
        public System.Action OnSolved;
        public void Solve() => OnSolved?.Invoke();
    }
}
