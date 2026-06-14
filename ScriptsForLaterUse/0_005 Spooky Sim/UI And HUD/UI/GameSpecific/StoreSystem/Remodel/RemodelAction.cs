using UnityEngine;

namespace Remodeling.Data
{
    public interface IRemodelUndo
    {
        void Undo();
    }

    public sealed class RemodelUndo : IRemodelUndo
    {
        private System.Action _undo;
        public RemodelUndo(System.Action undo) => _undo = undo;
        public void Undo() { _undo?.Invoke(); _undo = null; }
    }

    public abstract class RemodelAction : ScriptableObject
    {
        // Default: actions are applied once per purchase (adds +5 each time, etc.)
        public virtual bool ApplyPerPurchase => true;
        public abstract IRemodelUndo Apply(RemodelContext ctx);
        public abstract IRemodelUndo Apply(RemodelContext ctx, Transform deliveryPoint);
    }
}
