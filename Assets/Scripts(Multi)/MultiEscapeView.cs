using UnityEngine;

namespace FacilityEscape
{
    /// <summary>기존 씬의 구속장치, 기호 다이얼, 하트 및 텍스트를 게임 상태에 연결합니다.</summary>
    public sealed class MultiEscapeView : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private GameObject[] wristRestraints = new GameObject[0];
        [SerializeField] private GameObject[] remainingRestraints = new GameObject[0];
        [SerializeField] private GameObject symbolPanel;
        [SerializeField] private GameObject panelLockedIndicator;
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject failurePanel;
        [SerializeField] private GameObject[] playerAHearts = new GameObject[3];
        [SerializeField] private GameObject[] playerBHearts = new GameObject[3];
        [SerializeField] private Transform[] dialVisuals = new Transform[3];
        [SerializeField] private Vector3 dialRotationAxis = Vector3.forward;
        [Tooltip("월드 공간 TextMesh. UI 패키지 없이 동작합니다.")]
        [SerializeField] private TextMesh timerText;
        [SerializeField] private TextMesh statusText;
        [SerializeField] private TextMesh[] dialTexts = new TextMesh[3];
        private Quaternion[] initialDialRotations;

        private void Awake()
        {
            initialDialRotations = new Quaternion[dialVisuals.Length];
            for (int i = 0; i < dialVisuals.Length; i++)
                initialDialRotations[i] = dialVisuals[i] != null ? dialVisuals[i].localRotation : Quaternion.identity;
        }

        private void OnEnable()
        {
            if (game != null) game.StateChanged += Refresh;
            Refresh();
        }

        private void Start() => Refresh();
        private void OnDisable() { if (game != null) game.StateChanged -= Refresh; }

        private void Refresh()
        {
            if (game == null || game.Rules == null) return;
            var rules = game.Rules;
            bool escaped = rules.Stage == EscapeStage.Escaped;
            // 시간 종료 후에도 이미 풀린 손목은 다시 잠그지 않습니다.
            bool wristReleased = rules.ColorProgress == rules.ColorCount;
            SetAll(wristRestraints, !wristReleased);
            SetAll(remainingRestraints, !escaped);
            SetActive(symbolPanel, wristReleased);
            SetActive(panelLockedIndicator, rules.Stage == EscapeStage.ColorLock && rules.ColorLockoutSeconds > 0);
            SetActive(successPanel, escaped);
            SetActive(failurePanel, rules.Stage == EscapeStage.Failed);
            SetHearts(playerAHearts, rules.HeartsA);
            SetHearts(playerBHearts, rules.HeartsB);
            if (timerText != null)
            {
                SetActive(timerText.gameObject, !escaped);
                timerText.text = Mathf.CeilToInt((float)rules.RemainingSeconds).ToString("00");
            }
            if (statusText != null) statusText.text = Status(rules);
            for (int i = 0; i < MultiEscapeRules.DialCount; i++)
            {
                int value = (int)rules.GetDial(i);
                if (i < dialVisuals.Length && dialVisuals[i] != null)
                    dialVisuals[i].localRotation = initialDialRotations[i] * Quaternion.AngleAxis(-60 * value, dialRotationAxis);
                if (i < dialTexts.Length && dialTexts[i] != null)
                    dialTexts[i].text = MultiEscapeClueBoard.SymbolText((DialSymbol)value);
            }
        }

        private static string Status(MultiEscapeRules rules)
        {
            switch (rules.Stage)
            {
                case EscapeStage.ColorLock:
                    return rules.ColorLockoutSeconds > 0
                        ? $"LOCKED {rules.ColorLockoutSeconds:0.0}s" : $"LOCK 1: {rules.ColorProgress}/{rules.ColorCount}";
                case EscapeStage.SymbolLock: return "LOCK 2: SET DIALS / PULL LEVER";
                case EscapeStage.Escaped: return "ESCAPED";
                case EscapeStage.Failed: return "FAILED";
                default: return "READY";
            }
        }

        private static void SetHearts(GameObject[] hearts, int count)
        {
            for (int i = 0; i < hearts.Length; i++) SetActive(hearts[i], i < count);
        }
        private static void SetAll(GameObject[] objects, bool active)
        {
            foreach (var item in objects) SetActive(item, active);
        }
        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
