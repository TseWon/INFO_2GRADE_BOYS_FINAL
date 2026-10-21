using UnityEngine;
using UnityEngine.UIElements;

namespace FacilityEscape
{
    // Local camera/input and HUD. The LAN session assigns this machine's role.
    [DefaultExecutionOrder(100), RequireComponent(typeof(UIDocument))]
    public sealed class MultiEscapeLocalSession : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private MultiEscapePlayer playerA;
        [SerializeField] private MultiEscapePlayer playerB;
        private VisualElement root, modal, crosshair;
        private Label roleLabel, timerLabel, heartsA, heartsB, statusLabel, hintLabel, noticeLabel, modalTitle, modalBody;
        private Button switchButton, restartButton, startButton;
        private Button hostButton, joinButton, leaveButton, lobbyLeaveButton;
        private TextField addressField;
        private VisualElement connectionControls;
        private Label connectionLabel;
        private MultiEscapeLanSession lan;
        private double noticeUntil;
        private string notice;
        public EscapeRole ActiveRole { get; private set; }
        public bool MouseCaptured => UnityEngine.Cursor.lockState == CursorLockMode.Locked;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            modal = root.Q("modal");
            crosshair = root.Q("crosshair");
            roleLabel = root.Q<Label>("roleLabel"); timerLabel = root.Q<Label>("timerLabel");
            heartsA = root.Q<Label>("heartsA"); heartsB = root.Q<Label>("heartsB");
            statusLabel = root.Q<Label>("statusLabel"); hintLabel = root.Q<Label>("hintLabel");
            noticeLabel = root.Q<Label>("noticeLabel"); modalTitle = root.Q<Label>("modalTitle");
            modalBody = root.Q<Label>("modalBody");
            switchButton = root.Q<Button>("switchButton"); restartButton = root.Q<Button>("restartButton");
            startButton = root.Q<Button>("startButton");
            lan = game.Lan;
            hostButton = root.Q<Button>("hostButton"); joinButton = root.Q<Button>("joinButton");
            leaveButton = root.Q<Button>("leaveButton"); lobbyLeaveButton = root.Q<Button>("lobbyLeaveButton");
            addressField = root.Q<TextField>("addressField");
            connectionControls = root.Q("connectionControls"); connectionLabel = root.Q<Label>("connectionLabel");
            if (lan != null)
            {
                hostButton.clicked += lan.Host; joinButton.clicked += Join;
                leaveButton.clicked += lan.Leave; lobbyLeaveButton.clicked += lan.Leave;
                lan.Changed += NetworkChanged;
                game.onRoundStarted.AddListener(RoundStarted);
            }
            switchButton.clicked += SwitchRole; restartButton.clicked += StartRound; startButton.clicked += StartRound;
            game.StateChanged += Refresh;
            game.onWrongAnswer.AddListener(Wrong);
            game.onElectricShock.AddListener(Shock);
            playerA.IsPointerBlocked = PointerBlocked;
            playerB.IsPointerBlocked = PointerBlocked;
        }

        private void Start()
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            SelectRole(EscapeRole.Blindfolded);
            Refresh();
        }

        private void OnDisable()
        {
            if (lan != null)
            {
                hostButton.clicked -= lan.Host; joinButton.clicked -= Join;
                leaveButton.clicked -= lan.Leave; lobbyLeaveButton.clicked -= lan.Leave;
                lan.Changed -= NetworkChanged;
                game.onRoundStarted.RemoveListener(RoundStarted);
            }
            if (switchButton != null) switchButton.clicked -= SwitchRole;
            if (restartButton != null) restartButton.clicked -= StartRound;
            if (startButton != null) startButton.clicked -= StartRound;
            if (game != null)
            {
                game.StateChanged -= Refresh;
                game.onWrongAnswer.RemoveListener(Wrong);
                game.onElectricShock.RemoveListener(Shock);
            }
            if (playerA != null) playerA.IsPointerBlocked = null;
            if (playerB != null) playerB.IsPointerBlocked = null;
            SetMouseCaptured(false);
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) SetMouseCaptured(false);
            else if (!MouseCaptured && CanLook() && Input.GetMouseButtonDown(0) && !PointerBlocked())
                SetMouseCaptured(true);
            if (Input.GetKeyDown(KeyCode.Tab)) SwitchRole();
            bool typing = addressField != null && addressField.Contains(root.focusController.focusedElement as VisualElement);
            if (!typing && (Input.GetKeyDown(KeyCode.R) ||
                (Input.GetKeyDown(KeyCode.Space) && game.Rules != null && !game.Rules.IsRunning))) StartRound();
#endif
            if (noticeLabel != null) noticeLabel.text = Time.realtimeSinceStartupAsDouble < noticeUntil ? notice : "";
            crosshair?.EnableInClassList("hidden", !MouseCaptured || !CanLook());
        }

        private bool CanLook() => game != null && game.Rules != null &&
            (lan == null || lan.Connected) &&
            (game.Rules.IsRunning || game.Rules.Stage == EscapeStage.Escaped);

        public void SetMouseCaptured(bool captured)
        {
            UnityEngine.Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            UnityEngine.Cursor.visible = !captured;
            if (playerA != null) playerA.SuppressMouseForTwoFrames();
            if (playerB != null) playerB.SuppressMouseForTwoFrames();
            crosshair?.EnableInClassList("hidden", !captured || !CanLook());
        }

        public void StartRound()
        {
            if (!Application.isPlaying || (lan != null && !lan.CanStartRound)) return;
            noticeUntil = 0;
            SelectRole(EscapeRole.Blindfolded);
            playerA.ResetView(); playerB.ResetView();
            game.StartRound();
            if (game.Rules != null && game.Rules.IsRunning) SetMouseCaptured(true);
        }
        private void Join() => lan.Join(addressField.value);
        private void NetworkChanged() { SelectRole(lan.LocalRole); Refresh(); }
        private void RoundStarted()
        {
            noticeUntil = 0;
            playerA.ResetView(); playerB.ResetView();
            SetMouseCaptured(game.Rules.IsRunning);
        }
        public void SwitchRole()
        {
            if (lan == null) SelectRole(ActiveRole == EscapeRole.Blindfolded ? EscapeRole.Observer : EscapeRole.Blindfolded);
        }
        public void SelectRole(EscapeRole role)
        {
            if (lan != null) role = lan.LocalRole;
            ActiveRole = role;
            SetActivePlayer(playerA, role == EscapeRole.Blindfolded);
            SetActivePlayer(playerB, role == EscapeRole.Observer);
            playerA.SuppressMouseForTwoFrames(); playerB.SuppressMouseForTwoFrames();
            Refresh();
        }
        private static void SetActivePlayer(MultiEscapePlayer player, bool active)
        {
            player.LocalInput = active;
            player.ViewCamera.enabled = active;
            var listener = player.ViewCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = active;
        }

        private bool PointerBlocked()
        {
            if (root?.panel == null) return false;
            Vector2 position = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            var element = root.panel.Pick(RuntimePanelUtils.ScreenToPanel(root.panel, position));
            while (element != null)
            {
                if (element.ClassListContains("blocks-world")) return true;
                element = element.parent;
            }
            return false;
        }

        private void Wrong() { notice = "WRONG INPUT / TRY AGAIN"; noticeUntil = Time.realtimeSinceStartupAsDouble + 2; }
        private void Shock() { notice = "ELECTRIC SHOCK / BOTH PLAYERS -1 HP"; noticeUntil = Time.realtimeSinceStartupAsDouble + 1; }
        private static string Hearts(int count) => string.Join(" ", new[]
            { count > 0 ? "♥" : "♡", count > 1 ? "♥" : "♡", count > 2 ? "♥" : "♡" });

        private void Refresh()
        {
            if (root == null || game == null || game.Rules == null) return;
            var rules = game.Rules;
            bool observer = ActiveRole == EscapeRole.Observer;
            roleLabel.text = observer ? "B / OBSERVER / COLOR" : "A / OPERATOR / GRAYSCALE";
            timerLabel.text = Mathf.CeilToInt((float)rules.RemainingSeconds).ToString("00") + "s";
            timerLabel.EnableInClassList("hidden", rules.Stage == EscapeStage.Escaped);
            heartsA.text = "A  [ " + Hearts(rules.HeartsA) + " ]  " + rules.HeartsA + " HP";
            heartsB.text = "B  [ " + Hearts(rules.HeartsB) + " ]  " + rules.HeartsB + " HP";
            hintLabel.text = "Mouse: look  |  Aim + left-click: operate  |  Esc: free cursor  |  Tab: role  |  R: restart";
            modal.EnableInClassList("hidden", rules.IsRunning || rules.Stage == EscapeStage.Escaped);
            switch (rules.Stage)
            {
                case EscapeStage.Ready:
                    if (MouseCaptured) SetMouseCaptured(false);
                    statusLabel.text = "WAITING / TWO LOCKS / 60 SECONDS";
                    modalTitle.text = "RELEASE THE RESTRAINTS";
                    modalBody.text = "A operates the controls in grayscale. B reads the wall instructions. Answers change every round.\n\nMove the mouse to look. Aim at a control and left-click. Esc releases the cursor; click the room to resume. Tab switches roles.";
                    startButton.text = "SPACE / START";
                    break;
                case EscapeStage.ColorLock:
                    statusLabel.text = rules.ColorLockoutSeconds > 0 ? $"PANEL LOCKED / {rules.ColorLockoutSeconds:0.0}s"
                        : observer ? "READ THE WALL / DESCRIBE BUTTON POSITIONS TO A" : $"LOCK 01 / COLOR SEQUENCE / {rules.ColorProgress} OF {rules.ColorCount}";
                    break;
                case EscapeStage.SymbolLock:
                    statusLabel.text = observer ? "READ THE SYMBOL ORDER / A MUST PULL THE LEVER" : "LOCK 02 / SET THREE DIALS, THEN PULL THE LEVER";
                    break;
                case EscapeStage.Escaped:
                    statusLabel.text = "ESCAPED / ALL RESTRAINTS RELEASED";
                    hintLabel.text = "WASD: walk through the exit  |  Mouse: look  |  Esc: free cursor  |  Tab: role  |  R: restart";
                    break;
                case EscapeStage.Failed:
                    if (MouseCaptured) SetMouseCaptured(false);
                    statusLabel.text = "TEAM FAILED / CONTROLS DISABLED";
                    modalTitle.text = "TIME RAN OUT";
                    modalBody.text = "The team did not release both locks in time.\nUse the wall instructions and communicate the button positions.";
                    startButton.text = "SPACE / TRY AGAIN";
                    break;
            }
            if (lan != null)
            {
                switchButton.AddToClassList("hidden");
                connectionLabel.text = lan.Status;
                connectionControls.EnableInClassList("hidden", rules.Stage != EscapeStage.Ready);
                connectionLabel.EnableInClassList("hidden", rules.Stage != EscapeStage.Ready);
                hostButton.SetEnabled(!lan.IsBusy); joinButton.SetEnabled(!lan.IsBusy); addressField.SetEnabled(!lan.IsBusy);
                leaveButton.EnableInClassList("hidden", !lan.IsBusy);
                lobbyLeaveButton.EnableInClassList("hidden", !lan.IsBusy);
                startButton.SetEnabled(lan.CanStartRound); restartButton.SetEnabled(lan.CanStartRound);
                startButton.EnableInClassList("hidden", rules.Stage == EscapeStage.Ready && !lan.CanStartRound);
                timerLabel.EnableInClassList("hidden", rules.Stage == EscapeStage.Escaped || rules.Stage == EscapeStage.Ready);
                hintLabel.text = (rules.Stage == EscapeStage.Escaped ? "WASD: walk  |  " : "Aim + left-click: operate  |  ")
                    + "Mouse: look  |  Esc: free cursor" + (lan.IsHost ? "  |  R: restart" : "  |  Host controls restart");
                if (rules.Stage == EscapeStage.Ready)
                {
                    modalTitle.text = "TWO-PLAYER LAN";
                    modalBody.text = "Same Wi-Fi: create a room as A, then join from the other laptop as B.\nHost gives B the Wi-Fi IPv4 address shown below. Both players must connect before starting.";
                    statusLabel.text = lan.Connected ? "TWO PLAYERS CONNECTED / READY" : "WAITING FOR TWO PLAYERS";
                    if (MouseCaptured) SetMouseCaptured(false);
                }
                else if (rules.Stage == EscapeStage.Failed && !lan.IsHost)
                    modalBody.text = "Team failed. Waiting for the host to start a new round.";
            }
        }
    }
}
