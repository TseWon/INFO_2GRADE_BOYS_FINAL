using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HollowCreek
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class HollowCreekHud : MonoBehaviour
    {
        public StyleSheet stylesheet;
        public Texture2D flagPreview;
        public Texture2D[] flagTiles;
        public HollowCreekCampaign campaign;
        VisualElement root, modal, card, actions, content, qteBox, flash;
        Label chapter, objective, prompt, subtitle, title, body, feedback, qte;
        ProgressBar qteTimer;
        Action dismiss;
        bool canClose = true, paused;
        Font font;
        public bool IsOpen => modal != null && modal.style.display == DisplayStyle.Flex;
        public bool Paused => paused;
        public void SetCinematicHidden(bool hidden)
        {
            if (root != null) root.style.display = hidden ? DisplayStyle.None : DisplayStyle.Flex;
        }
        void Awake()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear(); root.pickingMode = PickingMode.Ignore;
            if (stylesheet != null) root.styleSheets.Add(stylesheet);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 22);
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            var top = Element(root, "mission");
            chapter = Label(top, "HOLLOW CREEK / PROJECT NULL", "eyebrow");
            objective = Label(top, "", "objective");
            Label(root, "+", "reticle");
            prompt = Label(root, "", "prompt");
            subtitle = Label(root, "", "subtitle");
            Label(root, "WASD 이동   SHIFT 달리기   SPACE 점프   E 상호작용   J 기록   ESC 일시정지", "controls");
            qteBox = Element(root, "qte-box"); qte = Label(qteBox, "X  손을 잡으세요!", "qte");
            qteTimer = new ProgressBar { lowValue = 0, highValue = 1 }; qteBox.Add(qteTimer); qteBox.style.display = DisplayStyle.None;
            flash = Element(root, "flash"); flash.pickingMode = PickingMode.Ignore;
            modal = Element(root, "modal");
            card = Element(modal, "card");
            Label(card, "HOLLOW CREEK RESEARCH COMPLEX", "eyebrow");
            title = Label(card, "", "title");
            body = Label(card, "", "body");
            content = Element(card, "content");
            feedback = Label(card, "", "feedback");
            actions = Element(card, "actions");
            modal.style.display = DisplayStyle.None;
        }
        static VisualElement Element(VisualElement parent, string cls)
        { var e = new VisualElement(); e.AddToClassList(cls); parent.Add(e); return e; }
        static Label Label(VisualElement parent, string text, string cls)
        { var e = new Label(text) { pickingMode = PickingMode.Ignore }; e.AddToClassList(cls); parent.Add(e); return e; }
        Button Button(VisualElement parent, string text, Action clicked)
        { var b = new Button(clicked) { text = text }; parent.Add(b); return b; }
        void Open(string heading, string text, bool closeable = true)
        {
            dismiss = null; canClose = closeable;
            title.text = heading; body.text = text; feedback.text = "";
            content.Clear(); actions.Clear(); modal.style.display = DisplayStyle.Flex;
            Capture(false);
        }
        public void Close()
        {
            if (!canClose) return;
            var callback = dismiss; dismiss = null;
            modal.style.display = DisplayStyle.None;
            if (!paused) campaign.StopSpeech();
            if (paused) { paused = false; Time.timeScale = 1; }
            Capture(true); callback?.Invoke();
        }
        public void Capture(bool locked)
        {
            UnityEngine.Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; UnityEngine.Cursor.visible = !locked;
            if (campaign != null && campaign.player != null) campaign.player.InputBlocked = !locked || campaign.Busy;
        }
        public void Document(string heading, string text)
        { Open(heading, text); campaign.Speak(text); Button(actions, "닫기 · ESC", Close); }
        public void Dialogue(string heading, string[] lines, Action finished = null)
        {
            int index = 0;
            Open(heading, lines[0], false);
            campaign.Speak(lines[0]);
            var next = Button(actions, "계속", null);
            next.clicked += () => {
                if (++index < lines.Length) { body.text = lines[index]; campaign.Speak(lines[index]); return; }
                canClose = true; Close(); finished?.Invoke();
            };
        }
        public void Welcome(Action newGame, Action resume)
        {
            Open("12월 24일 · 실종자 수색", "폐쇄된 연구시설, 다섯 명의 수색대.\n우리가 찾는 실종자 N은 아직 이곳에 있다.\n\n헤드폰을 착용하고 팀원에게 연구자 프로필을 받으세요.", false);
            Button(actions, "수색 시작", () => { canClose = true; Close(); newGame(); });
            if (HollowCreekSession.HasSave) Button(actions, "체크포인트 계속", resume);
        }
        public void Keypad(string heading, string text, bool numeric, Func<string, bool> submit, bool showSegments = false)
        {
            Open(heading, text);
            if (showSegments) SegmentDisplay();
            var input = new TextField { maxLength = numeric ? 4 : 8 }; input.AddToClassList("code-input"); content.Add(input);
            Action send = () => {
                if (submit(input.value)) { Close(); campaign.AfterPuzzle(); }
                else { feedback.text = "잠금이 풀리지 않습니다. 단서와 순서를 다시 확인하세요."; campaign.AudioCue(false); }
            };
            input.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { send(); e.StopPropagation(); } });
            Button(actions, "입력 확인", send); Button(actions, "돌아가기", Close);
            input.schedule.Execute(input.Focus).StartingIn(80);
        }
        void SegmentDisplay()
        {
            var row = Element(content, "segment-row");
            int[] x = { 8, 44, 44, 8, 2, 2, 8 }, y = { 0, 7, 43, 78, 43, 7, 39 };
            foreach (int digit in new[] { 8, 7, 4, 4 }) {
                var display = Element(row, "seven-segment"); int mask = HollowCreekRules.SegmentMask(digit);
                for (int s = 0; s < 7; s++) {
                    var bar = Element(display, "segment"); bool horizontal = s == 0 || s == 3 || s == 6;
                    bar.style.left = x[s]; bar.style.top = y[s]; bar.style.width = horizontal ? 34 : 6; bar.style.height = horizontal ? 6 : 32;
                    bar.EnableInClassList("segment-on", (mask & (1 << s)) != 0);
                }
                Label(row, "→", "segment-arrow");
            }
            Label(row, "?", "segment-question");
        }
        public void Sliding(SlidingPuzzle puzzle, Action solved)
        {
            Open("연구실 문 · 성조기 복원", "빈칸과 맞닿은 조각을 눌러 그림을 완성하세요.\n마지막 빈칸은 오른쪽 아래입니다. 숫자는 원본 조각의 위치를 뜻합니다.");
            var row = Element(content, "puzzle-row");
            var reference = new UnityEngine.UIElements.Image { image = flagPreview }; reference.AddToClassList("flag-reference"); row.Add(reference);
            var board = Element(row, "tile-board");
            var tileRows = new VisualElement[3];
            for (int r = 0; r < tileRows.Length; r++) tileRows[r] = Element(board, "tile-board-row");
            var buttons = new Button[9];
            Action refresh = () => {
                for (int j = 0; j < 9; j++) {
                    int value = puzzle.Tiles[j]; var b = buttons[j]; b.text = value == 0 ? "" : value.ToString();
                    b.style.backgroundImage = value == 0 || flagTiles == null || flagTiles.Length < value ? StyleKeyword.None : new StyleBackground(flagTiles[value - 1]);
                    b.EnableInClassList("empty", value == 0);
                }
                feedback.text = "이동 횟수: " + puzzle.Moves;
            };
            for (int i = 0; i < 9; i++) {
                int slot = i;
                buttons[i] = Button(tileRows[i / 3], "", () => {
                    if (!puzzle.Move(slot)) return;
                    refresh(); campaign.AudioCue(true);
                    if (SlidingPuzzle.IsSolved(puzzle.Tiles)) { Close(); solved(); }
                }); buttons[i].AddToClassList("tile");
            }
            refresh(); Button(actions, "나중에 풀기", Close);
        }
        public void Journal(HollowCreekRules rules)
        {
            Open("수색 기록", "확보한 문서는 언제든 다시 읽을 수 있습니다.");
            if (rules.profileRead) Button(content, "연구자 프로필", () => Document("연구자 프로필", HollowCreekCampaign.Profile));
            for (int i = 0; i < 4; i++) {
                int note = i;
                if (rules.HasNote(i)) Button(content, "연구일지 " + (i + 1), () => Document("연구일지 " + (note + 1), HollowCreekCampaign.Notes[note]));
            }
            if (rules.notes == 0 && !rules.profileRead) feedback.text = "아직 확보한 문서가 없습니다.";
            Button(actions, "닫기", Close);
        }
        public void Pause()
        {
            if (IsOpen) { Close(); return; }
            paused = true; Time.timeScale = 0;
            Open("수색 일시정지", "체크포인트는 구역 진입과 주요 사건 이후 자동 저장됩니다.");
            Button(actions, "계속", Close);
            Button(actions, "체크포인트 재시작", () => { canClose = true; Close(); campaign.Retry(); });
            Button(actions, "첫 화면", () => { canClose = true; Close(); campaign.ReturnToStart(); });
        }
        public void EndScreen(bool escaped, Action retry, Action home)
        {
            Open(escaped ? "단 한 명의 생존자" : "수색 신호 소실", escaped ?
                "지반이 내려앉고 HOLLOW CREEK가 눈 아래로 사라졌다.\n\n수색대 5명. 생존자 1명.\n실종자 N은 구조되지 못했다.\n\n무전기 너머로, 뒤틀린 캐럴이 한 번 더 흘렀다.\n— PROJECT NULL / END —" :
                "N에게 붙잡혔습니다.\nSHIFT로 달리고 SPACE로 장애물을 넘으세요.\n닫힌 방에서는 환풍구를 찾아야 합니다.", false);
            if (!escaped) Button(actions, "체크포인트 재시작", retry);
            Button(actions, "연구소 앞으로", home);
        }
        public void SetMission(string heading, string text) { chapter.text = heading; objective.text = text; }
        public void SetPrompt(string text) { prompt.text = text; prompt.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex; }
        public void Subtitle(string text) { subtitle.text = text; subtitle.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex; }
        public void Rescue(float remaining, float total)
        { qteBox.style.display = remaining > 0 ? DisplayStyle.Flex : DisplayStyle.None; qteTimer.value = remaining / total; qte.text = "X  손을 잡으세요!"; }
        public void Flash(float amount) { flash.style.backgroundColor = new Color(.5f, .02f, .015f, Mathf.Clamp01(amount) * .5f); }
        void OnDestroy() { if (paused) Time.timeScale = 1; if (font != null) Destroy(font); }
    }
}
