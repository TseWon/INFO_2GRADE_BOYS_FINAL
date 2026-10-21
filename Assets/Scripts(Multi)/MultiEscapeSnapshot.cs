using System;

namespace FacilityEscape
{
    [Serializable]
    public sealed class MultiEscapeSnapshot
    {
        public LockColor[] colors;
        public DialSymbol[] symbols;
        public int[] dials;
        public EscapeStage stage;
        public int heartsA, heartsB, progress, maxHearts;
        public double elapsed, nextShock, lockedUntil, duration, damageInterval, lockout;
        public int round, wrong, shocks;
    }
}
