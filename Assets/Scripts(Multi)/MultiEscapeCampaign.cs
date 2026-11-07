using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FacilityEscape
{
    [DefaultExecutionOrder(200)]
    public sealed class MultiEscapeCampaign : MonoBehaviour
    {
        public MultiEscapeGame game;
        public MultiEscapePlayer playerA, playerB;
        public Transform searchDoor, teamGate, killer, landingA, landingB;
        public Transform[] pursuitRoute;
        public MultiEscapeCampaignTarget[] targets;
        public Light[] alarmLights;
        public UIDocument hud;
        public MultiEscapeCampaignRules Rules { get; private set; } = new MultiEscapeCampaignRules();
        public CampaignState State => Rules.State;
        public MultiEscapePlayer LocalPlayer => playerA.LocalInput ? playerA : playerB;
        public string Message { get; private set; }
        private Label journal, prompt, stamina;
        private VisualElement overlay;
        private Vector3 doorClosed, gateClosed;
        private int appliedLifted;
        private double lastTick, nextHold, messageUntil;
        private readonly System.Random random = new System.Random();
        private MultiEscapeAtmosphere atmosphere;
        private Transform rope;
        private int seenFeedback;
        private Transform killerTorso, killerMask;
        private Transform[] killerLegs;
        private Vector3 torsoRest, maskRest;

        private void Awake()
        {
            game.Campaign = this;
            doorClosed = searchDoor.localPosition; gateClosed = teamGate.localPosition;
            atmosphere = gameObject.AddComponent<MultiEscapeAtmosphere>();
            atmosphere.campaign = this;
            rope = transform.Find("03_Pursuit/RescueRope");
            killerTorso = killer.Find("Torso"); killerMask = killer.Find("Mask");
            if (killerTorso != null) torsoRest = killerTorso.localPosition;
            if (killerMask != null) maskRest = killerMask.localPosition;
            var legs = new System.Collections.Generic.List<Transform>();
            foreach (Transform part in killer) if (part.name == "Boot") legs.Add(part);
            killerLegs = legs.ToArray();
        }
        private void OnEnable()
        {
            game.onRoundStarted.AddListener(ResetCampaign);
            game.onEscaped.AddListener(BeginSearch);
        }
        private void OnDisable()
        {
            game.onRoundStarted.RemoveListener(ResetCampaign);
            game.onEscaped.RemoveListener(BeginSearch);
            if (game.Campaign == this) game.Campaign = null;
        }
        private void Start()
        {
            var root = hud.rootVisualElement;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 20);
            root.style.unityFontDefinition = FontDefinition.FromFont(font);
            journal = new Label { name = "campaignJournal", pickingMode = PickingMode.Ignore };
            journal.AddToClassList("campaign-journal"); root.Add(journal);
            prompt = new Label { name = "campaignPrompt", pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("campaign-prompt"); root.Add(prompt);
            stamina = new Label { pickingMode = PickingMode.Ignore };
            stamina.AddToClassList("campaign-stamina"); root.Add(stamina);
            overlay = new VisualElement { pickingMode = PickingMode.Ignore };
            overlay.AddToClassList("campaign-overlay"); root.Add(overlay);
            ResetCampaign();
        }
        private void ResetCampaign()
        {
            Rules = new MultiEscapeCampaignRules();
            appliedLifted = 0; nextHold = messageUntil = 0; Message = "";
            seenFeedback = 0;
            lastTick = Time.realtimeSinceStartupAsDouble;
            searchDoor.localPosition = doorClosed; teamGate.localPosition = gateClosed;
        }
        private void BeginSearch()
        {
            if (!game.HasAuthority) return;
            Rules.Begin(game.Rules.HeartsA, game.Rules.HeartsB, random);
            lastTick = Time.realtimeSinceStartupAsDouble;
            Tell("구속이 풀렸습니다. 열린 문 너머의 증거실로 이동하세요.");
        }
        public void ApplyRemote(CampaignState state)
        {
            Rules.Restore(state);
            if (State.feedbackSequence > seenFeedback)
            {
                if (State.feedbackRole == (int)LocalPlayer.Role) Tell(State.feedback);
                seenFeedback = State.feedbackSequence;
            }
            ApplyLifts();
        }
        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double dt = Math.Max(0, now - lastTick); lastTick = now;
            if (!game.HasAuthority || State.chapter == EscapeChapter.Chair || Rules.Terminal) return;
            Rules.Advance(dt);
            if (State.chapter == EscapeChapter.Chase)
            {
                // Eight seconds of head start before the killer leaves the evidence room.
                if (State.chaseElapsed > 8 && (State.gateOpen || RoutePosition(State.killerDistance).z < 42.4f))
                    State.killerDistance += (float)Math.Min(dt, State.chaseElapsed - 8) * 2.35f;
                Vector3 position = RoutePosition(State.killerDistance);
                CheckCaught(playerA, position); CheckCaught(playerB, position);
            }
            ApplyLifts();
        }
        private void CheckCaught(MultiEscapePlayer player, Vector3 position)
        {
            if ((State.exited & (1 << (int)player.Role)) != 0 || State.chaseElapsed <= 8) return;
            Vector3 target = player.transform.position + Vector3.up;
            Vector3 source = position + Vector3.up;
            if (Vector3.Distance(source, target) < 1.2f &&
                (!Physics.Linecast(source, target, out var hit, ~0, QueryTriggerInteraction.Ignore) ||
                 hit.collider.GetComponentInParent<MultiEscapePlayer>() == player))
                Rules.Fail((player.Role == EscapeRole.Blindfolded ? "A" : "B") + "가 살인마에게 붙잡혔습니다. 동료와 보조를 맞춰 다시 도전하세요.");
        }
        public Vector3 RoutePosition(float distance)
        {
            if (pursuitRoute == null || pursuitRoute.Length == 0) return Vector3.zero;
            for (int i = 1; i < pursuitRoute.Length; i++)
            {
                float length = Vector3.Distance(pursuitRoute[i - 1].position, pursuitRoute[i].position);
                if (distance <= length) return Vector3.Lerp(pursuitRoute[i - 1].position, pursuitRoute[i].position, length > 0 ? distance / length : 0);
                distance -= length;
            }
            return pursuitRoute[pursuitRoute.Length - 1].position;
        }
        private void ApplyLifts()
        {
            foreach (var player in new[] { playerA, playerB })
            {
                int bit = 1 << (int)player.Role;
                if ((State.lifted & bit) == 0 || (appliedLifted & bit) != 0) continue;
                player.GetComponent<MultiEscapeWalk>().BeginClimb(player.Role == EscapeRole.Blindfolded ? landingA.position : landingB.position);
            }
            appliedLifted = State.lifted;
        }
        public void CheckInteraction(MultiEscapePlayer player, bool held, bool pressed)
        {
            if (Rules.Terminal || !game.CanExplore || !player.LocalInput) return;
            if (!Physics.Raycast(player.InteractionRay, out var hit, 3.4f, ~0, QueryTriggerInteraction.Ignore)) return;
            var target = hit.collider.GetComponentInParent<MultiEscapeCampaignTarget>();
            if (target == null || !((target.hold && held && Time.unscaledTimeAsDouble >= nextHold) || pressed)) return;
            nextHold = Time.unscaledTimeAsDouble + .12;
            if (game.HasAuthority) Act(player.Role, target.action);
            else game.Lan.SendCampaignAction(target.action);
        }
        public bool Act(EscapeRole role, int action)
        {
            if (!game.HasAuthority || !game.CanExplore || action < 0 || action >= targets.Length ||
                (role != EscapeRole.Blindfolded && role != EscapeRole.Observer)) return false;
            var player = role == EscapeRole.Blindfolded ? playerA : playerB;
            if (player.GetComponent<MultiEscapeWalk>().IsClimbing) return false;
            var target = targets[action];
            if (target == null || Vector3.Distance(player.ViewCamera.transform.position, target.transform.position) > 4.2f) return false;
            if (action == 10 && Vector3.ProjectOnPlane(player.transform.position - target.transform.position, Vector3.up).magnitude > 1.7f)
            { Reply(role, "담장 가까이 다가온 뒤 E를 누르세요."); return false; }
            Vector3 origin = player.ViewCamera.transform.position;
            if (Physics.Linecast(origin, target.transform.position, out var obstruction, ~0, QueryTriggerInteraction.Ignore) &&
                obstruction.collider.GetComponentInParent<MultiEscapeCampaignTarget>() != target) return false;
            if (action == 6 && (Vector3.Distance(playerA.transform.position, target.transform.position) > 5 ||
                Vector3.Distance(playerB.transform.position, target.transform.position) > 5))
            { Reply(role, "두 사람이 문 앞에 모여야 합니다."); return false; }
            bool accepted = Rules.Act(role, action);
            if (action == 6 && !accepted) Reply(role, "증거 3개를 확인하고, 심장 → 눈 → 손 순서로 암호를 맞추세요. 동료도 문 앞으로 와야 합니다.");
            if (action == 10 && !accepted) Reply(role, "한 사람은 발판에서 E를 계속 누르고, 다른 사람은 담장의 E를 누르세요.");
            if (accepted && action < 3) Reply(role, "증거를 기록했습니다. 왼쪽 수첩을 확인하세요.");
            ApplyLifts();
            return accepted;
        }
        public void Tell(string message) { Message = message; messageUntil = Time.realtimeSinceStartupAsDouble + 4; }
        private void Reply(EscapeRole role, string message)
        {
            if (LocalPlayer.Role == role) Tell(message);
            else { State.feedbackRole = (int)role; State.feedback = message; State.feedbackSequence++; }
        }
        private void LateUpdate()
        {
            bool afterSearch = State.chapter == EscapeChapter.Chase || State.chapter == EscapeChapter.Won || State.chapter == EscapeChapter.Failed && State.chaseElapsed > 0;
            searchDoor.localPosition = Vector3.MoveTowards(searchDoor.localPosition, doorClosed + (afterSearch ? Vector3.up * 3.8f : Vector3.zero), Time.unscaledDeltaTime * 3);
            teamGate.localPosition = Vector3.MoveTowards(teamGate.localPosition, gateClosed + (State.gateOpen ? Vector3.up * 3.8f : Vector3.zero), Time.unscaledDeltaTime * 4);
            killer.gameObject.SetActive(State.chapter == EscapeChapter.Chase);
            if (rope != null) rope.gameObject.SetActive(State.ropeDown);
            if (killer.gameObject.activeSelf)
            {
                killer.position = RoutePosition(State.killerDistance);
                Vector3 direction = RoutePosition(State.killerDistance + .2f) - killer.position;
                if (direction.sqrMagnitude > .001f) killer.rotation = Quaternion.LookRotation(direction);
                float gait = Mathf.Sin(State.killerDistance * 5.5f);
                if (killerTorso != null) killerTorso.localPosition = torsoRest + Vector3.up * Mathf.Abs(gait) * .045f;
                if (killerMask != null) killerMask.localPosition = maskRest + Vector3.up * Mathf.Abs(gait) * .035f;
                for (int i = 0; i < killerLegs.Length; i++)
                    killerLegs[i].localRotation = Quaternion.Euler(gait * 18 * (i % 2 == 0 ? 1 : -1), 0, 0);
            }
            foreach (var light in alarmLights)
                light.intensity = afterSearch ? 1.5f + Mathf.Sin(Time.unscaledTime * 5) * 1.1f :
                    State.chapter == EscapeChapter.Search && Rules.Remaining < 60 ? .25f + Mathf.PerlinNoise(Time.unscaledTime * 17, light.transform.position.z) * .7f : .7f;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].display == null) continue;
                if (i < 3) targets[i].display.text = new[] { "HEART", "EYE", "HAND" }[i] + "\n" + State.code[i];
                else if (i < 6) targets[i].display.text = State.entry[i - 3].ToString();
            }
            UpdateHud();
        }
        private void UpdateHud()
        {
            if (journal == null) return;
            var root = hud.rootVisualElement;
            bool chair = State.chapter == EscapeChapter.Chair;
            // Keep microphone settings available with Esc, but give the room back to the player during movement.
            root.Q("voicePanel").style.display = Rules.Terminal || UnityEngine.Cursor.lockState == CursorLockMode.Locked ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q("crosshair").EnableInClassList("hidden", Rules.Terminal);
            journal.style.display = chair || Rules.Terminal ? DisplayStyle.None : DisplayStyle.Flex;
            stamina.style.display = chair || Rules.Terminal ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q("connectionControls").EnableInClassList("hidden", !chair || game.Rules.Stage != EscapeStage.Ready);
            root.Q("connectionLabel").EnableInClassList("hidden", !chair || game.Rules.Stage != EscapeStage.Ready);
            prompt.text = "";
            overlay.style.backgroundColor = State.swarmHit && State.elapsed < 140.2 && State.chapter == EscapeChapter.Search
                ? new Color(.5f, .025f, .015f, (float)(140.2 - State.elapsed) * .3f) : Color.clear;
            if (chair)
            {
                var chairTimer = root.Q<Label>("timerLabel");
                chairTimer.style.translate = new Translate(0, 0); chairTimer.style.color = Color.white;
                if (game.Rules.Stage == EscapeStage.Ready)
                {
                    root.Q<Label>("modalTitle").text = "연쇄살인마의 집";
                    root.Q<Label>("modalBody").text = "01 전기의자: B가 벽의 단서를 읽고 A가 색상·기호를 조작합니다. 오답은 둘 다 -1 HP, 세 번 틀리면 감전사합니다.\n02 증거실: 3분 안에 증거 3개로 암호를 푸세요.\n03 추격전: 함께 철문을 열고 담장을 넘어 둘 다 탈출하세요.\n\nE / 클릭: 상호작용 · 마우스: 시점\nWASD: 이동 · Shift: 달리기 · Esc: 커서 해제\n같은 Wi-Fi에서 방 만들기 / 호스트 IPv4로 참가";
                    root.Q<Label>("statusLabel").text = "게임 시작 전 역할과 조작법을 확인하세요";
                }
                else if (game.Rules.Stage == EscapeStage.Failed)
                {
                    root.Q<Label>("modalTitle").text = "전기의자에서 사망했습니다";
                    root.Q<Label>("modalBody").text = "오답 세 번 또는 제한 시간 초과입니다. B가 단서를 전달하고 A가 입력하세요.";
                }
                root.Q<Label>("hintLabel").text = "E / 왼쪽 클릭: 조작  |  마우스: 시점  |  Esc: 커서  |  R: 재시작";
                return;
            }
            var walk = LocalPlayer.GetComponent<MultiEscapeWalk>();
            stamina.text = walk.IsClimbing ? "담장을 넘는 중…" : walk.Exhausted ? "숨 고르는 중…  " + Mathf.RoundToInt(walk.Stamina * 100) + "%"
                : "달리기 " + Mathf.RoundToInt(walk.Stamina * 100) + "%  ·  Shift";
            journal.text = "증거 수첩\n" + Evidence(0, "심장") + "\n" + Evidence(1, "눈") + "\n" + Evidence(2, "손") +
                "\n문 옆 메모: 심장 → 눈 → 손";
            if (State.chapter == EscapeChapter.Chase)
                journal.text = "협동 탈출\n" + (State.gateOpen ? "✓ 철문 개방" : "A / B 스위치에서 E를 함께 2초 유지") +
                    "\n" + (State.lifted == 3 ? "✓ 두 사람 모두 담장 통과" : State.ropeDown ? "밧줄이 내려왔습니다. 남은 사람은 담장에서 E" : State.lifted != 0 ? "올라간 사람: E로 밧줄 내리기" : "한 명은 발판 E 유지 · 다른 한 명은 담장 E") +
                    "\n마지막 출구는 두 사람이 모두 사용하세요.";
            var timer = root.Q<Label>("timerLabel");
            timer.RemoveFromClassList("hidden");
            int seconds = Mathf.CeilToInt((float)Rules.Remaining);
            timer.text = Rules.Terminal ? "—" : $"{seconds / 60:00}:{seconds % 60:00}";
            float fear = State.chapter == EscapeChapter.Search ? Mathf.Clamp01((60 - seconds) / 60f) : 0;
            timer.style.translate = new Translate(Mathf.Sin(Time.unscaledTime * 43) * fear * 6, Mathf.Cos(Time.unscaledTime * 31) * fear * 4);
            timer.style.color = fear > 0 ? new Color(1, .4f, .28f) : Color.white;
            root.Q<Label>("heartsA").text = "A  " + new string('♥', State.heartsA) + "  " + State.heartsA + " HP";
            root.Q<Label>("heartsB").text = "B  " + new string('♥', State.heartsB) + "  " + State.heartsB + " HP";
            root.Q<Label>("roleLabel").text = (LocalPlayer.Role == EscapeRole.Blindfolded ? "A" : "B") + " / " +
                (State.chapter == EscapeChapter.Search ? "02  증거실" : "03  추격전");
            root.Q<Label>("hintLabel").text = "WASD 이동  ·  Shift 달리기  ·  E 상호작용 / 스위치 유지  ·  Esc 커서";
            root.Q<Label>("statusLabel").text = State.chapter == EscapeChapter.Search
                ? Rules.Swarming ? "벌레 떼가 덮쳐옵니다!" : Rules.Silent ? "…숨소리가 멈췄다." : State.swarmHit ? "탁자에 부딪혀 둘 다 -1 HP. 서둘러 문을 여세요!" : "단서를 찾아 암호를 입력하고, 동료와 문 앞에 모이세요."
                : "살인마가 쫓아옵니다. 노란 유도선을 따라 함께 탈출하세요!";
            if (State.chapter == EscapeChapter.Chase && !State.gateOpen && State.gateProgress > 0)
                root.Q<Label>("statusLabel").text = $"철문 개방 중… {Mathf.Clamp01((float)State.gateProgress / 2) * 100:0}% · 두 사람 모두 E 유지";
            if (Time.realtimeSinceStartupAsDouble < messageUntil) prompt.text = Message;
            else if (!Rules.Terminal && Physics.Raycast(LocalPlayer.InteractionRay, out var hit, 3.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<MultiEscapeCampaignTarget>();
                if (target != null) prompt.text = "[E" + (target.hold ? " 유지" : "") + "] " + target.prompt;
            }
            root.Q("modal").EnableInClassList("hidden", !Rules.Terminal);
            if (Rules.Terminal)
            {
                prompt.text = "";
                UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true;
                root.Q<Label>("statusLabel").text = State.chapter == EscapeChapter.Won ? "탈출 성공 · 동료와 함께 살아남았습니다." : "팀 탈출 실패 · 다시 도전할 수 있습니다.";
                root.Q<Label>("hintLabel").text = "호스트가 재시작하거나 방을 나갈 수 있습니다.";
                root.Q<Label>("modalTitle").text = State.chapter == EscapeChapter.Won ? "두 사람 모두 탈출했습니다" : "탈출 실패";
                root.Q<Label>("modalBody").text = State.chapter == EscapeChapter.Won ? "서로의 목소리를 따라 지하실을 벗어났습니다.\n전기의자 → 증거실 → 협동 추격전 완료" : State.failure;
                root.Q<Button>("startButton").text = "처음부터 다시 도전";
                root.Q<Button>("startButton").RemoveFromClassList("hidden");
                root.Q<Button>("startButton").SetEnabled(game.Lan == null || game.Lan.CanStartRound);
            }
        }
        private string Evidence(int index, string title) => title + ": " + ((State.evidence & 1 << index) != 0 ? State.code[index].ToString() : "미발견");
    }
}
