using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HollowCreek
{
    [DefaultExecutionOrder(-50)]
    public sealed class HollowCreekCampaign : MonoBehaviour
    {
        public int sceneNumber = 1;
        public SnowFirstPerson player;
        public HollowCreekHud hud;
        public SoloCinematicSlot openingCinematic, finalEscapeCinematic;
        public SoloCinematicSlot chaseCinematic;
        public Transform start, upperSpawn, prisonSpawn, ventSpawn, endingSpawn;
        public GameObject[] labLeaves;
        public GameObject upperProps, prisonProps, passageBarrier, prisonBarrier, lowerLabDoor, sealDoor, collapsedExit;
        public Transform lift, cable, monster;
        public Transform[] teammates, chaseRoute, ventRoute, collapsePieces;
        public float rescueWindow = 2.5f;
        public float chaseSpeed = 3.7f;
        public HollowCreekRules Rules => HollowCreekSession.Rules;
        public bool Busy { get; private set; }
        public float PursuerDistance { get; private set; } = 30;
        public float ChaseProgress => pursuit;
        public float RescueRemaining => rescueRemaining;
        HollowCreekAtmosphere atmosphere;
        HollowCreekSpeech speech;
        SlidingPuzzle sliding;
        float rescueRemaining, pursuit, subtitleUntil, trauma, nextBang;
        bool loading, deathShown, ventReady;
        Vector3 monsterRestScale;
        string lastObjective;
        public const string Profile = "연구자 프로필 / 보안 사본\n\n이름: ELIAS WARD\n소속: PROJECT NULL · 약리 및 행동 연구\n시설 창설년도: 1953년\n최종 출근: 12월 24일\n\n[보안 메모] 정문 잠금은 '시설 창설년도' 네 자리.\n이 프로필은 J 키 수색 기록에서 다시 확인할 수 있다.";
        public static readonly string[] Notes = {
            "실험 기록 01 / 피험자 N\n\n신종 약물 투약 후 극심한 폭력성이 나타났다. 여러 명이 붙잡아 겨우 진정시켰으나, 의식을 잃은 뒤에도 근육 경련은 계속됐다.\n\n피험자는 입소 당시 실종자로 분류된 인물과 일치한다.",
            "실험 기록 02 / 광과민 반응\n\n새로운 조합을 투약하자 N은 빛을 피하기 시작했다. 항상 어두운 구석에 웅크리고 음식과 물을 거부한다.\n\n강한 빛은 짧은 지연만 유발한다. 격리 장벽을 대신할 수 없다.",
            "실험 기록 03 / 철수 전날\n\n정부 조사단이 접근 중이다. 기록과 연구는 모두 폐쇄한다.\n남은 약물을 한꺼번에 투약해 마지막 반응을 관찰할 것이다.\n\n'아무 일도 일어나지 않을 것이다.'\n그 문장 아래에는 찢긴 손자국만 남아 있다.",
            "시설 충성 서약 / 통신 규정\n\n우리는 조국의 이름 아래 침묵한다. 별과 줄무늬가 우리를 지켜볼 것이다.\n\n그림을 복원하면 통신실로 가는 문이 열린다.\n철수일: 12월 24일.\n비상 무전 채널은 철수일의 월일 네 자리로 고정한다."
        };
        const string Acrostic = "O — Our nation watches over us.\nP — Preserve the silence below.\nE — Every subject serves the cause.\nN — Nothing leaves this place.\n\n벽에 새겨진 문구: '각 행의 첫 글자가 길을 연다.'";

        void Awake()
        {
            HollowCreekSession.Enter(sceneNumber);
            atmosphere = GetComponent<HollowCreekAtmosphere>();
            speech = GetComponent<HollowCreekSpeech>();
            hud.campaign = this;
            if (monster != null) { monsterRestScale = monster.localScale; monster.gameObject.SetActive(false); }
        }
        void Start()
        {
            sliding = new SlidingPuzzle(19531224);
            player.minimumY = -15; player.boundary = 230;
            ApplyWorld();
            if (sceneNumber == 1 && !HollowCreekSession.Resumed)
            {
                HollowCreekSession.NewGame(); ApplyWorld(); HollowCreekSession.Save();
                StartCoroutine(Opening());
            }
            else if (sceneNumber == 2 && Rules.stage == Stage.Lift)
            {
                HollowCreekSession.Save();
                Say("밀러: 이 승강기로 위쪽 보안층에 갈 수 있어. 조작반을 확인해.", 8);
            }
            else if (sceneNumber == 3)
            {
                HollowCreekSession.Save();
                Say(Rules.route == Route.Upper ? "리드: 다른 둘을 찾아야 해. 연구실의 단서부터 살펴보자." : "희미한 비명이 끊겼다. 철창 너머에는 아무도 없다.", 8);
            }
            else if (sceneNumber == 4)
            {
                if (Rules.stage == Stage.Vent) { ventReady = true; BeginPursuit(true); }
                else { HollowCreekSession.Save(); StartCoroutine(FinalArrival()); }
            }
            HollowCreekSession.Resumed = false;
            RefreshObjective();
        }
        void ApplyWorld()
        {
            bool upper = Rules.route != Route.Prison;
            if (upperProps != null) upperProps.SetActive(upper);
            if (prisonProps != null) prisonProps.SetActive(!upper);
            if (passageBarrier != null) passageBarrier.SetActive(!Rules.passageOpen);
            if (prisonBarrier != null) prisonBarrier.SetActive(!Rules.passageOpen);
            if (lowerLabDoor != null) lowerLabDoor.SetActive(!Rules.puzzleSolved);
            SetLabDoor();
            if (sealDoor != null) sealDoor.SetActive(Rules.stage == Stage.Vent || Rules.stage == Stage.Sealed);
            if (collapsedExit != null) collapsedExit.SetActive(Rules.stage == Stage.Vent || Rules.stage == Stage.Sealed);
            Transform spawn = start;
            if (sceneNumber == 2 && Rules.route != Route.Undecided && Rules.stage != Stage.Lift) spawn = upper ? upperSpawn : prisonSpawn;
            if (sceneNumber == 3) spawn = upper ? upperSpawn : prisonSpawn;
            if (Rules.stage == Stage.Vent && ventSpawn != null) spawn = ventSpawn;
            if (spawn != null) player.Teleport(spawn.position, spawn.rotation);
            player.SetCrawling(Rules.stage == Stage.Vent);
            if (sceneNumber == 2 && Rules.stage == Stage.WeakWall) PoseLowerTeam();
            if (sceneNumber == 2 && Rules.stage == Stage.Acrostic && teammates != null)
                for (int i = 2; i < teammates.Length; i++) if (teammates[i] != null) teammates[i].gameObject.SetActive(false);
        }
        void Update()
        {
            if (loading) return;
            if (Input.GetKeyDown(KeyCode.Escape) && !Busy && Rules.stage != Stage.Ending && Rules.stage != Stage.Dead) hud.Pause();
            if (Input.GetKeyDown(KeyCode.J) && !hud.IsOpen && !Busy && !InPursuit) hud.Journal(Rules);
            player.InputBlocked = Busy || hud.IsOpen || Rules.stage == Stage.Dead || Rules.stage == Stage.Ending || Rules.stage == Stage.Rescue;
            if (hud.Paused) return;
            trauma = Mathf.MoveTowards(trauma, 0, Time.deltaTime * .65f); hud.Flash(trauma);
            if (Time.time > subtitleUntil) hud.Subtitle("");
            if (Rules.stage == Stage.Rescue && !Busy && rescueRemaining > 0)
            {
                rescueRemaining = Mathf.Max(0, rescueRemaining - Time.deltaTime);
                hud.Rescue(rescueRemaining, rescueWindow);
                if (Input.GetKeyDown(KeyCode.X)) ResolveRescue(true);
                else if (rescueRemaining <= 0) ResolveRescue(false);
            }
            if (!Busy && !hud.IsOpen && InPursuit) UpdatePursuit();
            RefreshObjective();
            UpdateInteraction();
        }
        bool InPursuit => Rules.stage == Stage.Chase || Rules.stage == Stage.Vent;
        void UpdateInteraction()
        {
            hud.SetPrompt("");
            if (Busy || hud.IsOpen || Rules.stage == Stage.Rescue || Rules.stage == Stage.Dead || Rules.stage == Stage.Ending) return;
            var cam = player.viewCamera;
            if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, 3.5f, ~0, QueryTriggerInteraction.Ignore)) return;
            var target = hit.collider.GetComponentInParent<HollowCreekTarget>();
            if (target == null || !target.Available) return;
            hud.SetPrompt("E  " + target.label);
            if (Input.GetKeyDown(KeyCode.E)) target.Use();
        }
        public bool IsAvailable(HollowCreekTarget target)
        {
            if (loading || Rules.stage == Stage.Dead || Rules.stage == Stage.Ending) return false;
            if (target.route != Route.Undecided && target.route != Rules.route) return false;
            switch (target.interaction)
            {
                case Interaction.Profile: return Rules.stage == Stage.Arrival;
                case Interaction.Entrance: return Rules.stage == Stage.Arrival || Rules.stage == Stage.Entrance;
                case Interaction.Lift: return Rules.stage == Stage.Lift;
                case Interaction.Acrostic: return Rules.stage == Stage.Acrostic && !Rules.passageOpen;
                case Interaction.WeakWall: return Rules.stage == Stage.WeakWall && !Rules.passageOpen;
                case Interaction.NextArea: return Rules.passageOpen && sceneNumber == 2;
                case Interaction.Wounded: return Rules.stage == Stage.WeakWall && !Rules.passageOpen;
                case Interaction.Digits: return Rules.stage == Stage.Search && Rules.route == Route.Upper;
                case Interaction.Sliding: return Rules.stage == Stage.Search && Rules.route == Route.Prison;
                case Interaction.Radio: return Rules.stage == Stage.Search || Rules.stage == Stage.Radio;
                case Interaction.SealDoor: return Rules.stage == Stage.Chase;
                case Interaction.Vent: return Rules.stage == Stage.Sealed && ventReady;
                case Interaction.Exit: return Rules.stage == Stage.Vent;
                case Interaction.Cabinet: return sceneNumber == 3 && !Rules.cabinetOpened;
                default: return sceneNumber == 3 || target.interaction == Interaction.Guide;
            }
        }
        public void Interact(HollowCreekTarget target)
        {
            if (Busy || hud.IsOpen || !IsAvailable(target)) return;
            switch (target.interaction)
            {
                case Interaction.Profile: Rules.ReadProfile(); hud.Document("밀러: 이 프로필부터 읽어봐", Profile); break;
                case Interaction.Entrance:
                    if (Rules.stage == Stage.Entrance) LoadLaboratory();
                    else if (!Rules.profileRead) Say("밀러에게 연구자 프로필을 먼저 받으세요.");
                    else hud.Keypad("연구소 출입 인증", "힌트: 연구자 프로필에 기록된 시설 창설년도.\n프로필은 J 키로 다시 읽을 수 있습니다.", true, Rules.UnlockEntrance);
                    break;
                case Interaction.Lift: StartCoroutine(LiftSequence()); break;
                case Interaction.Acrostic: hud.Keypad("잠긴 정비실 · 벽면 서약", Acrostic, false, Rules.OpenPassage); break;
                case Interaction.WeakWall: StartCoroutine(BreakWall()); break;
                case Interaction.Wounded: hud.Document("움직일 수 없는 동료", "콜: 갈비뼈가… 숨을 쉴 수가 없어. 먼저 나갈 길을 찾아.\n\n옆의 동료는 이미 숨을 멈췄다. 철창 옆 벽에는 깊은 균열이 나 있다."); break;
                case Interaction.NextArea: Load(3); break;
                case Interaction.Digits:
                    hud.Keypad("연구실 전원 복구", "켜진 막대의 개수를 다음 숫자로 적으세요.\n시설 메모: 이 표시기의 7은 왼쪽 위 막대도 켜집니다.\n화면에 실제로 켜진 막대를 기준으로 세세요.", true, Rules.SolveDigits, true); break;
                case Interaction.Sliding: hud.Sliding(sliding, () => { Rules.SolveSliding(sliding.Tiles); AfterPuzzle(); }); break;
                case Interaction.Cabinet: StartCoroutine(CabinetScare()); break;
                case Interaction.Radio:
                    if (!Rules.puzzleSolved) { Say("통신 전원이 끊겼습니다. 연구실 잠금장치를 먼저 해결하세요."); return; }
                    if (!Rules.HasNote(3)) { Say("채널 규정이 필요합니다. 시설 충성 서약 문서를 찾아보세요."); return; }
                    hud.Keypad("노트북 옆 비상 무전기", "채널 네 자리를 입력하세요.\n힌트: 시설 충성 서약의 '통신 규정'.", true, answer => { radioEntered = Rules.TuneRadio(answer); return radioEntered; }); break;
                case Interaction.SealDoor:
                    if (Rules.Seal()) StartCoroutine(SealSequence()); break;
                case Interaction.Vent:
                    if (Rules.EnterVent()) { player.Teleport(ventSpawn.position, ventSpawn.rotation); player.SetCrawling(true); BeginPursuit(true); HollowCreekSession.Save(); Say("환풍구 안에서는 자동으로 몸을 낮춥니다. 빛이 보이는 출구로 이동하세요.", 7); } break;
                case Interaction.Exit: if (Rules.Escape()) StartCoroutine(Ending()); break;
                case Interaction.Guide: Say(Rules.route == Route.Upper ? "리드: 문서부터 읽어. 숫자의 값이 아니라 표시기에 켜진 막대를 세는 거야." : "밀러: 통신 규정은 벽에 붙은 서약서에 있을 거야."); break;
                default:
                    int index = (int)target.interaction - (int)Interaction.Note1;
                    if (index >= 0 && index < 4) { Rules.ReadNote(index); hud.Document("연구일지 " + (index + 1), Notes[index]); HollowCreekSession.Save(); }
                    break;
            }
        }
        public void AfterPuzzle()
        {
            AudioCue(true);
            if (Rules.stage == Stage.Entrance) { HollowCreekSession.Save(); Say("밀러: 잘했어. 이제 정신 꽉 붙들어 매. 인증기를 다시 눌러 들어가자.", 7); }
            else if (sceneNumber == 2 && Rules.passageOpen) { if (passageBarrier != null) passageBarrier.SetActive(false); HollowCreekSession.Save(); Say("잠금 해제. 열린 통로 끝으로 이동하세요."); }
            else if (sceneNumber == 3 && Rules.stage == Stage.Radio)
            {
                if (lowerLabDoor != null) lowerLabDoor.SetActive(false);
                SetLabDoor();
                // The radio validator is read-only; keypad completion is distinguished by the active objective.
                if (radioEntered) { radioEntered = false; StartCoroutine(RadioSequence()); }
                else { HollowCreekSession.Save(); Say("전원 복구. 연구일지를 확인하고 무전 채널을 맞추세요."); }
            }
        }
        bool radioEntered;
        void SetLabDoor() { if (labLeaves != null) foreach (var leaf in labLeaves) if (leaf != null) leaf.SetActive(!Rules.puzzleSolved); }
        IEnumerator LiftSequence()
        {
            if (!Rules.StartLift()) yield break;
            Busy = true; player.InputBlocked = true;
            Say("콜: 왜 올라가는 거지? …잠깐, 위에서 소리가 나.", 5);
            Vector3 liftStart = lift != null ? lift.position : Vector3.zero;
            Vector3 playerStart = player.transform.position;
            for (float t = 0; t < 3; t += Time.deltaTime)
            {
                if (lift != null) lift.position = liftStart + Vector3.up * Mathf.Min(t, 1.2f);
                player.Teleport(playerStart + Vector3.up * Mathf.Min(t, 1.2f), player.transform.rotation);
                yield return null;
            }
            if (monster != null) { monster.position = player.transform.position + player.transform.forward * 2.2f + Vector3.up * 2; monster.gameObject.SetActive(true); }
            if (monster != null) player.viewCamera.transform.LookAt(monster.position + Vector3.up * 1.5f);
            Say("리드: 위를 봐! 케이블을 끊고 있어!", 3); Impact();
            yield return new WaitForSeconds(1);
            if (cable != null) cable.gameObject.SetActive(false);
            if (lift != null) lift.position = liftStart;
            if (teammates != null && teammates.Length > 0 && teammates[0] != null)
            {
                player.viewCamera.transform.LookAt(teammates[0].position + Vector3.up * 1.4f);
                foreach (var bone in teammates[0].GetComponentsInChildren<Transform>())
                    if (bone.name == "UpperArm_R") bone.localRotation *= Quaternion.Euler(-75, 0, 0);
            }
            // X is only sampled inside the visible timed window, never before it.
            rescueRemaining = rescueWindow; Busy = false;
        }
        public bool ResolveRescue(bool success)
        {
            if (Busy || !Rules.ResolveRescue(success)) return false;
            rescueRemaining = 0; hud.Rescue(0, rescueWindow);
            if (monster != null) monster.gameObject.SetActive(false);
            ApplyWorld(); HollowCreekSession.Save(); Impact();
            Say(success ? "밀러: 잡았다! 다른 둘은 아래로 떨어졌어… 이 문부터 열자." : "충격과 함께 승강기가 멈췄다. 옆에서 동료의 신음이 들린다.", 8);
            return true;
        }
        void PoseLowerTeam()
        {
            if (teammates == null) return;
            for (int i = 0; i < teammates.Length; i++) {
                if (teammates[i] == null) continue;
                if (i < 2) { teammates[i].gameObject.SetActive(false); continue; }
                teammates[i].position = prisonSpawn.position + new Vector3(i == 2 ? 1.5f : -1.5f, .35f, 1.2f);
                teammates[i].rotation = Quaternion.Euler(0, 0, 80);
                if (i == 3 && Rules.passageOpen) teammates[i].gameObject.SetActive(false);
            }
        }
        IEnumerator BreakWall()
        {
            Busy = true; Say("균열이 난 벽을 밀고 있습니다…", 2); Impact();
            yield return new WaitForSeconds(1.2f);
            Rules.OpenPassage("PUSH"); if (passageBarrier != null) passageBarrier.SetActive(false);
            if (prisonBarrier != null) prisonBarrier.SetActive(false);
            if (teammates != null && teammates.Length > 3 && teammates[3] != null) teammates[3].gameObject.SetActive(false);
            Impact(); Say("콜: 뒤에… 뒤에 누가…!\n신음이 끊겼다. 연구실 쪽으로 나가야 한다.", 7);
            HollowCreekSession.Save(); Busy = false;
        }
        IEnumerator CabinetScare()
        {
            Rules.cabinetOpened = true; Busy = true;
            if (monster != null) { monster.position = player.transform.position + player.transform.forward * 1.4f; monster.LookAt(player.transform.position); monster.gameObject.SetActive(true); }
            Impact(); yield return new WaitForSeconds(.55f);
            if (monster != null) monster.gameObject.SetActive(false);
            Busy = false; Rules.ReadNote(0); hud.Document("넘어진 벽장 안의 기록", Notes[0]); HollowCreekSession.Save();
        }
        IEnumerator RadioSequence()
        {
            Busy = true;
            Say("무전기: …여기는 콜… 찾았어. N을 찾았는데… 사람의 모습이…", 5);
            AudioCue(false); yield return new WaitForSeconds(Mathf.Max(4, speech != null ? speech.Duration : 0));
            Impact(); Say("무전기: 도망쳐! 뒤를 보지… [비명과 잡음]", 4);
            yield return new WaitForSeconds(Mathf.Max(3, speech != null ? speech.Duration : 0));
            Busy = false; Load(4);
        }
        IEnumerator FinalArrival()
        {
            Busy = true;
            if (teammates != null) for (int i = 2; i < teammates.Length; i++) if (teammates[i] != null) teammates[i].rotation = Quaternion.Euler(0, i * 45, 90);
            hud.Dialogue(Rules.route == Route.Upper ? "수색 종료" : "다시 만난 동료들", new[] {
                Rules.route == Route.Upper ? "밀러: 여기가 마지막 신호가 잡힌 곳이야.\n\n찾아낸 두 동료는 이미 숨을 멈췄다. N의 실험 기록만 바닥에 흩어져 있다." :
                "밀러: 살아 있었군! 아래에서 무슨 일이 있었던 거야?\n\n설명을 마치기도 전에 천장에서 둔탁한 소리가 울렸다.",
                "리드: 저건… N이야.\n\n[쿵!]\n\nSHIFT로 달리고 SPACE로 장애물을 넘으세요.\n복도 끝의 문을 E로 잠그세요."
            }, () => StartCoroutine(Ambush()));
            yield return null;
        }
        IEnumerator Ambush()
        {
            // Both CASE routes converge here before the shared pursuit begins.
            player.InputBlocked = true;
            hud.SetCinematicHidden(true);
            if (chaseCinematic != null) yield return chaseCinematic.Play();
            hud.SetCinematicHidden(false);
            if (monster != null) { monster.position = start.position + Vector3.forward * 4; monster.gameObject.SetActive(true); }
            Impact(); Say("밀러: 뛰어! 우리 신경 쓰지 말고…!", 4);
            if (teammates != null) for (int i = 0; i < teammates.Length; i++) {
                if (teammates[i] == null) continue;
                teammates[i].rotation = Quaternion.Euler(0, i * 45, 90);
                teammates[i].position += Vector3.up * .3f;
            }
            yield return new WaitForSeconds(1.5f);
            Busy = false; BeginPursuit(false);
        }
        void BeginPursuit(bool vent)
        {
            pursuit = 0;
            if (monster != null) { monster.localScale = monsterRestScale * (vent ? .4f : 1); monster.position = Point(vent ? ventRoute : chaseRoute, 0); monster.gameObject.SetActive(true); }
        }
        void UpdatePursuit()
        {
            var route = Rules.stage == Stage.Vent ? ventRoute : chaseRoute;
            if (monster == null || route == null || route.Length < 2) return;
            pursuit += Time.deltaTime * (Rules.stage == Stage.Vent ? 1.95f : chaseSpeed);
            var p = Point(route, pursuit); var next = Point(route, pursuit + .4f);
            monster.position = p;
            if ((next - p).sqrMagnitude > .001f) monster.rotation = Quaternion.LookRotation(next - p);
            PursuerDistance = Vector3.Distance(player.transform.position, p);
            if (PursuerDistance < 1.2f) Die();
            else if (PursuerDistance < 6 && Time.time >= nextBang) { nextBang = Time.time + 2; Impact(.4f); Say("바로 뒤에서 금속을 긁는 소리가 들린다!", 1.5f); }
        }
        public static Vector3 Point(Transform[] route, float distance)
        {
            if (route == null || route.Length == 0) return Vector3.zero;
            for (int i = 1; i < route.Length; i++) {
                float length = Vector3.Distance(route[i - 1].position, route[i].position);
                if (distance <= length) return Vector3.Lerp(route[i - 1].position, route[i].position, length > 0 ? distance / length : 1);
                distance -= length;
            }
            return route[route.Length - 1].position;
        }
        IEnumerator SealSequence()
        {
            Busy = true;
            if (sealDoor != null) sealDoor.SetActive(true);
            if (collapsedExit != null) collapsedExit.SetActive(true);
            Say("문은 잠겼다. 하지만 비상구는 잔해로 막혀 있다. 환풍구를 찾아라.", 7);
            for (int i = 0; i < 3; i++) { Impact(); yield return new WaitForSeconds(1); }
            if (monster != null) monster.gameObject.SetActive(false);
            ventReady = true; Busy = false;
        }
        IEnumerator Ending()
        {
            Busy = true; player.InputBlocked = true;
            hud.SetCinematicHidden(true);
            if (finalEscapeCinematic != null) yield return finalEscapeCinematic.Play();
            if (endingSpawn != null) player.Teleport(endingSpawn.position, endingSpawn.rotation);
            player.SetCrawling(false); Impact();
            Say("N이 출구를 뜯어낸다. 그 순간, 연구소 아래의 지반이 무너지기 시작했다.", 6);
            for (float t = 0; t < 4; t += Time.deltaTime) {
                if (collapsePieces != null) foreach (var piece in collapsePieces) if (piece != null) { piece.position += Vector3.down * Time.deltaTime * (1 + t); piece.Rotate(0, 0, Time.deltaTime * 7); }
                if (monster != null) monster.position += Vector3.down * Time.deltaTime * 2;
                yield return null;
            }
            if (monster != null) monster.gameObject.SetActive(false);
            hud.SetCinematicHidden(false);
            Busy = false; hud.EndScreen(true, Retry, ReturnToStart);
        }
        IEnumerator Opening()
        {
            Busy = true; player.InputBlocked = true;
            hud.SetCinematicHidden(true);
            if (openingCinematic != null) yield return openingCinematic.Play();
            hud.SetCinematicHidden(false);
            Busy = false; hud.Capture(true);
        }
        public void Die()
        {
            if (!InPursuit || deathShown) return;
            deathShown = true; Rules.stage = Stage.Dead; Impact(); player.InputBlocked = true;
            hud.EndScreen(false, Retry, ReturnToStart);
        }
        public void Retry() { if (HollowCreekSession.Restore()) Load(HollowCreekSession.Scene, false); else ReturnToStart(); }
        public void Resume() { if (HollowCreekSession.Restore()) Load(HollowCreekSession.Scene, false); }
        public void ReturnToStart() { HollowCreekSession.NewGame(); Load(1, false); }
        void LoadLaboratory()
        {
            if (loading) return;
            if (SoloSceneRoutes.BuildIndex(SoloSceneRoutes.Laboratory) < 0)
            {
                Say("씬이 빌드 목록에 없습니다: " + SoloSceneRoutes.Laboratory, 8);
                Debug.LogError("Missing solo scene: " + SoloSceneRoutes.Laboratory, this);
                return;
            }
            loading = true; Time.timeScale = 1; player.InputBlocked = true;
            StartCoroutine(LoadScene(SoloSceneRoutes.Laboratory));
        }
        void Load(int scene, bool advance = true)
        {
            if (loading) return;
            string name = SoloSceneRoutes.CampaignScene(scene);
            if (SoloSceneRoutes.BuildIndex(name) < 0) { Say("씬이 빌드 목록에 없습니다: " + name, 8); Debug.LogError("Missing solo scene: " + name, this); return; }
            if (advance) HollowCreekSession.Enter(scene);
            loading = true; Time.timeScale = 1; player.InputBlocked = true;
            StartCoroutine(LoadScene(name));
        }
        IEnumerator LoadScene(string name) { var operation = SceneManager.LoadSceneAsync(SoloSceneRoutes.BuildIndex(name)); while (!operation.isDone) yield return null; }
        public float Speak(string text) => speech != null ? speech.Speak(text) : 0;
        public void StopSpeech() { if (speech != null) speech.Stop(); }
        public void Say(string text, float seconds = 5) { hud.Subtitle(text); subtitleUntil = Time.time + Mathf.Max(seconds, Speak(text)); }
        public void AudioCue(bool success) { if (atmosphere != null) atmosphere.Cue(success); }
        void Impact(float amount = 1) { trauma = amount; if (atmosphere != null) atmosphere.Impact(amount); }
        void RefreshObjective()
        {
            string text;
            switch (Rules.stage) {
                case Stage.Arrival: text = Rules.profileRead ? "정문 인증기 · 연구자 프로필로 출입 암호 찾기" : "트럭에서 내려 밀러에게 연구자 프로필 받기"; break;
                case Stage.Entrance: text = "인증 완료 · 출입 인증기를 E로 눌러 연구소 진입"; break;
                case Stage.Lift: text = "승강기 조작반 작동"; break;
                case Stage.Rescue: text = "케이블 절단 · X로 동료의 손을 잡기"; break;
                case Stage.Acrostic: text = Rules.passageOpen ? "열린 정비 통로로 이동" : "CASE 1 · 벽면 서약으로 정비실 잠금 해제"; break;
                case Stage.WeakWall: text = Rules.passageOpen ? "연구실 통로로 탈출" : "CASE 2 · 부상당한 동료 확인 / 균열 난 벽 밀기"; break;
                case Stage.Search: text = Rules.route == Route.Upper ? "실종자 수색 · 디지털 표시기 규칙으로 전원 복구" : "지하 연구실 · 성조기 조각을 복원해 문 열기"; break;
                case Stage.Radio: text = "연구일지 수집 / 통신 규정을 찾아 무전 채널 입력"; break;
                case Stage.Chase: text = "도망쳐! · SHIFT 달리기 / SPACE 장애물 넘기 / 끝의 문 잠그기"; break;
                case Stage.Sealed: text = "비상구 붕괴 · 환풍구를 찾아 E로 진입"; break;
                case Stage.Vent: text = "환풍구 탈출 · 출구의 빛을 따라 이동"; break;
                case Stage.Ending: text = "HOLLOW CREEK · 지반 붕괴"; break;
                default: text = "수색 신호 소실"; break;
            }
            if (text == lastObjective) return;
            lastObjective = text;
            hud.SetMission("PROJECT NULL / " + (sceneNumber == 1 ? "PROLOGUE" : sceneNumber == 4 ? "FINAL" : "CHAPTER " + (sceneNumber - 1)) + " / 12.24", text);
        }
        void OnDestroy() { Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}
