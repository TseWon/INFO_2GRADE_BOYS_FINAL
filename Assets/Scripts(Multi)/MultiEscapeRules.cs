using System;

namespace FacilityEscape
{
    public enum EscapeRole { Blindfolded, Observer }
    public enum EscapeStage { Ready, ColorLock, SymbolLock, Escaped, Failed }
    public enum LockColor { Red, Blue, Yellow, Green }
    public enum DialSymbol { Circle, Triangle, Square, Star, Diamond, Cross }
    public enum EscapeInputResult { Ignored, Accepted, Wrong, Unlocked, Escaped }

    /// <summary>Unity/네트워크와 독립적인 규칙. 한 게임당 하나만 생성합니다.</summary>
    public sealed class MultiEscapeRules
    {
        public const int DialCount = 3;
        public const int SymbolCount = 6;
        private readonly LockColor[] colorAnswer;
        private readonly DialSymbol[] symbolAnswer;
        private readonly int[] dials = new int[DialCount];
        private readonly double timeLimit, damageInterval, lockoutDuration;
        private readonly int maxHearts;
        private double elapsed, nextShock, lockedUntil;

        public EscapeStage Stage { get; private set; } = EscapeStage.Ready;
        public int HeartsA { get; private set; }
        public int HeartsB { get; private set; }
        public int ColorProgress { get; private set; }
        public int ColorCount => colorAnswer.Length;
        public bool IsRunning => Stage == EscapeStage.ColorLock || Stage == EscapeStage.SymbolLock;
        public double RemainingSeconds => Math.Max(0, timeLimit - elapsed);
        public double ColorLockoutSeconds => Math.Max(0, lockedUntil - elapsed);

        public MultiEscapeRules(LockColor[] colors, DialSymbol[] symbols,
            double duration = 60, int hearts = 3, double shockInterval = 20, double lockout = 3)
        {
            if (colors == null || colors.Length == 0)
                throw new ArgumentException("색상 정답을 하나 이상 지정하세요.", nameof(colors));
            if (symbols == null || symbols.Length != DialCount)
                throw new ArgumentException("기호 정답은 정확히 3개여야 합니다.", nameof(symbols));
            if (!PositiveFinite(duration) || !PositiveFinite(shockInterval) ||
                !PositiveFinite(lockout) || hearts < 1)
                throw new ArgumentException("시간, 피해 간격, 잠금 시간과 HP는 양수여야 합니다.");
            foreach (var color in colors)
                if (!Enum.IsDefined(typeof(LockColor), color)) throw new ArgumentException("잘못된 색상 값입니다.");
            foreach (var symbol in symbols)
                if (!Enum.IsDefined(typeof(DialSymbol), symbol)) throw new ArgumentException("잘못된 기호 값입니다.");
            colorAnswer = (LockColor[])colors.Clone();
            symbolAnswer = (DialSymbol[])symbols.Clone();
            timeLimit = duration;
            maxHearts = hearts;
            damageInterval = shockInterval;
            lockoutDuration = lockout;
            HeartsA = HeartsB = hearts;
        }

        private static bool PositiveFinite(double value) => value > 0 && !double.IsInfinity(value) && !double.IsNaN(value);

        public void StartRound()
        {
            elapsed = lockedUntil = 0;
            nextShock = damageInterval;
            HeartsA = HeartsB = maxHearts;
            ColorProgress = 0;
            Array.Clear(dials, 0, dials.Length);
            Stage = EscapeStage.ColorLock;
        }

        /// <returns>이번 갱신에서 두 플레이어에게 동시에 적용한 전기 충격 횟수.</returns>
        public int Advance(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsRunning) return 0;
            elapsed = Math.Min(timeLimit, elapsed + seconds);
            int shocks = 0;
            // 프레임이 지연되어도 20/40/60초 피해를 빠뜨리지 않습니다.
            while (nextShock <= elapsed && HeartsA > 0 && HeartsB > 0)
            {
                HeartsA = Math.Max(0, HeartsA - 1);
                HeartsB = Math.Max(0, HeartsB - 1);
                nextShock += damageInterval;
                shocks++;
            }
            if (HeartsA == 0 || HeartsB == 0 || elapsed >= timeLimit)
                Stage = EscapeStage.Failed;
            return shocks;
        }

        public EscapeInputResult PressColor(EscapeRole role, int color)
        {
            if (role != EscapeRole.Blindfolded || Stage != EscapeStage.ColorLock ||
                ColorLockoutSeconds > 0 || color < 0 || color > 3) return EscapeInputResult.Ignored;
            if ((LockColor)color != colorAnswer[ColorProgress])
            {
                ColorProgress = 0;
                lockedUntil = elapsed + lockoutDuration;
                return EscapeInputResult.Wrong;
            }
            ColorProgress++;
            if (ColorProgress < colorAnswer.Length) return EscapeInputResult.Accepted;
            Stage = EscapeStage.SymbolLock;
            return EscapeInputResult.Unlocked;
        }

        public EscapeInputResult RotateDial(EscapeRole role, int index, int direction)
        {
            if (role != EscapeRole.Blindfolded || Stage != EscapeStage.SymbolLock ||
                index < 0 || index >= DialCount || (direction != -1 && direction != 1))
                return EscapeInputResult.Ignored;
            dials[index] = (dials[index] + direction + SymbolCount) % SymbolCount;
            return EscapeInputResult.Accepted;
        }

        public EscapeInputResult PullLever(EscapeRole role)
        {
            if (role != EscapeRole.Blindfolded || Stage != EscapeStage.SymbolLock)
                return EscapeInputResult.Ignored;
            for (int i = 0; i < DialCount; i++)
                if (dials[i] != (int)symbolAnswer[i])
                {
                    Array.Clear(dials, 0, dials.Length);
                    return EscapeInputResult.Wrong;
                }
            Stage = EscapeStage.Escaped;
            return EscapeInputResult.Escaped;
        }

        public LockColor GetColorAnswer(int index) => colorAnswer[index];

        // Only the host advances the simulation. Clients display this authoritative replica.
        public MultiEscapeSnapshot Capture()
        {
            return new MultiEscapeSnapshot {
                colors = (LockColor[])colorAnswer.Clone(), symbols = (DialSymbol[])symbolAnswer.Clone(),
                dials = (int[])dials.Clone(), stage = Stage, heartsA = HeartsA, heartsB = HeartsB,
                progress = ColorProgress, elapsed = elapsed, nextShock = nextShock, lockedUntil = lockedUntil,
                duration = timeLimit, damageInterval = damageInterval, lockout = lockoutDuration, maxHearts = maxHearts
            };
        }

        public static MultiEscapeRules FromSnapshot(MultiEscapeSnapshot state)
        {
            var replica = new MultiEscapeRules(state.colors, state.symbols, state.duration,
                state.maxHearts, state.damageInterval, state.lockout);
            replica.Stage = state.stage;
            replica.HeartsA = state.heartsA; replica.HeartsB = state.heartsB;
            replica.ColorProgress = state.progress;
            replica.elapsed = state.elapsed; replica.nextShock = state.nextShock; replica.lockedUntil = state.lockedUntil;
            Array.Copy(state.dials, replica.dials, DialCount);
            return replica;
        }
        public DialSymbol GetSymbolAnswer(int index) => symbolAnswer[index];
        public DialSymbol GetDial(int index) => (DialSymbol)dials[index];
    }
}
