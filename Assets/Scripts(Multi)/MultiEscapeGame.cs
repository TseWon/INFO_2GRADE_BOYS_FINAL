using System;
using UnityEngine;
using UnityEngine.Events;

namespace FacilityEscape
{
    [DisallowMultipleComponent]
    public sealed class MultiEscapeGame : MonoBehaviour
    {
        [Header("Players (A / B)")]
        [SerializeField] private MultiEscapePlayer playerA;
        [SerializeField] private MultiEscapePlayer playerB;
        [SerializeField] private bool startAutomatically = true;

        [Header("Rules")]
        [SerializeField, Min(1)] private float timeLimit = 60;
        [SerializeField, Min(1)] private int maxHearts = 3;
        [Tooltip("기획 시간표 기준 20초. 40초 간격이 필요하면 이 값을 40으로 변경하세요.")]
        [SerializeField, Min(0.1f)] private float damageInterval = 20;
        [SerializeField, Min(0.1f)] private float wrongColorLockSeconds = 3;
        [Tooltip("매 라운드 색상 순서와 기호 정답을 새로 생성합니다. 끄면 아래 고정 정답을 사용합니다.")]
        [SerializeField] private bool randomizeAnswers = true;
        [SerializeField] private LockColor[] colorAnswer =
            { LockColor.Red, LockColor.Blue, LockColor.Yellow, LockColor.Green };
        [SerializeField] private DialSymbol[] symbolAnswer =
            { DialSymbol.Triangle, DialSymbol.Star, DialSymbol.Diamond };

        [Header("Sound (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip wrongAnswerSound;
        [SerializeField] private AudioClip electricShockSound;
        [SerializeField] private AudioClip unlockSound;

        [Header("Scene / HUD events")]
        public UnityEvent onRoundStarted = new UnityEvent();
        public UnityEvent onWristReleased = new UnityEvent();
        public UnityEvent onWrongAnswer = new UnityEvent();
        public UnityEvent onElectricShock = new UnityEvent();
        public UnityEvent onEscaped = new UnityEvent();
        public UnityEvent onFailed = new UnityEvent();
        public UnityEvent<float> onTimeChanged = new UnityEvent<float>();
        public UnityEvent<int> onPlayerAHeartsChanged = new UnityEvent<int>();
        public UnityEvent<int> onPlayerBHeartsChanged = new UnityEvent<int>();

        public MultiEscapeRules Rules { get; private set; }
        public event Action StateChanged;
        public MultiEscapeLanSession Lan { get; set; }
        public int RoundNumber { get; private set; }
        private int wrongCount, shockCount;
        public bool HasAuthority => Lan == null || Lan.IsHost;
        private double lastTick;
        private bool terminalEventSent;
        private readonly System.Random puzzleRandom = new System.Random();

        private void Awake() => CreateRules();
        private void Start() { if (startAutomatically) StartRound(); }

        private bool CreateRules(bool newRound = false)
        {
            try
            {
                var colors = colorAnswer;
                var symbols = symbolAnswer;
                if (newRound && randomizeAnswers)
                    MultiEscapePuzzleGenerator.Generate(puzzleRandom, Rules, out colors, out symbols);
                Rules = new MultiEscapeRules(colors, symbols, timeLimit,
                    maxHearts, damageInterval, wrongColorLockSeconds);
                return true;
            }
            catch (ArgumentException exception)
            {
                Rules = null;
                Debug.LogError($"MultiEscapeGame 설정 오류: {exception.Message}", this);
                return false;
            }
        }

        /// <summary>재시작도 이 메서드를 사용합니다. 온라인 연결 시 두 참가자 준비 후 호스트에서 호출하세요.</summary>
        public void StartRound()
        {
            if (!Application.isPlaying || !HasAuthority || (Lan != null && !Lan.CanStartRound)) return;
            if (playerA == null || playerB == null || playerA == playerB ||
                playerA.Role != EscapeRole.Blindfolded || playerB.Role != EscapeRole.Observer ||
                playerA.Game != this || playerB.Game != this)
            {
                Debug.LogError("서로 다른 A(Blindfolded), B(Observer)와 각 플레이어의 Game 참조를 지정하세요.", this);
                return;
            }
            if (!CreateRules(true)) return;
            Rules.StartRound();
            RoundNumber++;
            wrongCount = shockCount = 0;
            terminalEventSent = false;
            lastTick = Time.realtimeSinceStartupAsDouble;
            PublishState();
            onRoundStarted.Invoke();
        }

        private void Update() => AdvanceClock();

        private void AdvanceClock()
        {
            if (!HasAuthority || Rules == null || !Rules.IsRunning) return;
            double now = Time.realtimeSinceStartupAsDouble;
            int shocks = Rules.Advance(Math.Max(0, now - lastTick));
            shockCount += shocks;
            lastTick = now;
            PublishState();
            for (int i = 0; i < shocks; i++)
            {
                PlaySound(electricShockSound);
                onElectricShock.Invoke();
            }
            PublishTerminalEvent();
        }

        public bool IsOperator(MultiEscapePlayer player) =>
            HasAuthority && player != null && player == playerA && player.Role == EscapeRole.Blindfolded && player.Game == this;

        public EscapeInputResult PressColor(MultiEscapePlayer player, LockColor color)
        {
            AdvanceClock(); // Update 순서와 상관없이 제한 시간 이후 입력은 거부합니다.
            if (!IsOperator(player) || Rules == null) return EscapeInputResult.Ignored;
            return HandleResult(Rules.PressColor(player.Role, (int)color));
        }

        public EscapeInputResult RotateDial(MultiEscapePlayer player, int index, int direction)
        {
            AdvanceClock();
            if (!IsOperator(player) || Rules == null) return EscapeInputResult.Ignored;
            return HandleResult(Rules.RotateDial(player.Role, index, direction));
        }

        public EscapeInputResult PullLever(MultiEscapePlayer player)
        {
            AdvanceClock();
            if (!IsOperator(player) || Rules == null) return EscapeInputResult.Ignored;
            return HandleResult(Rules.PullLever(player.Role));
        }

        private EscapeInputResult HandleResult(EscapeInputResult result)
        {
            if (result == EscapeInputResult.Ignored) return result;
            PublishState();
            if (result == EscapeInputResult.Wrong)
            {
                wrongCount++;
                PlaySound(wrongAnswerSound);
                onWrongAnswer.Invoke();
            }
            if (result == EscapeInputResult.Unlocked)
            {
                PlaySound(unlockSound);
                onWristReleased.Invoke();
            }
            PublishTerminalEvent();
            return result;
        }

        private void PublishState()
        {
            StateChanged?.Invoke();
            onTimeChanged.Invoke((float)Rules.RemainingSeconds);
            onPlayerAHeartsChanged.Invoke(Rules.HeartsA);
            onPlayerBHeartsChanged.Invoke(Rules.HeartsB);
        }

        public MultiEscapeSnapshot CaptureSnapshot()
        {
            var state = Rules.Capture();
            state.round = RoundNumber; state.wrong = wrongCount; state.shocks = shockCount;
            return state;
        }

        public void ApplyRemoteSnapshot(MultiEscapeSnapshot state)
        {
            if (HasAuthority) return;
            bool newRound = state.round != RoundNumber;
            var oldStage = Rules.Stage;
            Rules = MultiEscapeRules.FromSnapshot(state);
            if (newRound)
            {
                RoundNumber = state.round;
                wrongCount = shockCount = 0;
                terminalEventSent = false;
                onRoundStarted.Invoke();
            }
            PublishState();
            if (state.wrong > wrongCount) { PlaySound(wrongAnswerSound); onWrongAnswer.Invoke(); }
            for (int i = shockCount; i < state.shocks; i++) { PlaySound(electricShockSound); onElectricShock.Invoke(); }
            if (oldStage == EscapeStage.ColorLock && Rules.Stage == EscapeStage.SymbolLock)
            { PlaySound(unlockSound); onWristReleased.Invoke(); }
            wrongCount = state.wrong; shockCount = state.shocks;
            PublishTerminalEvent();
        }

        public void ReturnToLobby()
        {
            CreateRules();
            RoundNumber = wrongCount = shockCount = 0;
            terminalEventSent = false;
            onRoundStarted.Invoke();
            PublishState();
        }

        private void PublishTerminalEvent()
        {
            if (terminalEventSent || Rules.IsRunning || Rules.Stage == EscapeStage.Ready) return;
            terminalEventSent = true;
            if (Rules.Stage == EscapeStage.Escaped)
            {
                PlaySound(unlockSound);
                onEscaped.Invoke();
            }
            else onFailed.Invoke();
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
