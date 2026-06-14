namespace Cherry.Spawning
{
    public enum RespawnReason
    {
        Default = 0,
        Death = 1,
        Load = 2,      // loading from save
        RestartDay = 3 // optional
    }

    public static class RespawnRequest
    {
        private static bool _pending;
        private static RespawnReason _reason;
        private static string _overrideSpawnId;

        public static void Set(RespawnReason reason, string overrideSpawnId = null)
        {
            _pending = true;
            _reason = reason;
            _overrideSpawnId = overrideSpawnId;
        }

        public static bool TryConsume(out RespawnReason reason, out string overrideSpawnId)
        {
            if (!_pending)
            {
                reason = RespawnReason.Default;
                overrideSpawnId = null;
                return false;
            }

            _pending = false;
            reason = _reason;
            overrideSpawnId = _overrideSpawnId;
            _overrideSpawnId = null;
            return true;
        }
    }
}
