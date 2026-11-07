using System;

namespace FacilityEscape
{
    public enum EscapeChapter { Chair, Search, Chase, Won, Failed }

    [Serializable]
    public sealed class CampaignState
    {
        public EscapeChapter chapter;
        public double elapsed, chaseElapsed, holdAUntil, holdBUntil, braceUntil, gateProgress;
        public int heartsA = 3, heartsB = 3, evidence, lifted, exited, braceRole = -1;
        public int[] code = { 4, 7, 2 }, entry = { 0, 0, 0 };
        public bool swarmHit, gateOpen, ropeDown;
        public float killerDistance;
        public double liftAtA = -10, liftAtB = -10;
        public int feedbackSequence, feedbackRole;
        public string feedback = "";
        public string failure = "";
    }

    // All timers and cooperative decisions run on the host. The client receives a replica.
    public sealed class MultiEscapeCampaignRules
    {
        public const double SearchDuration = 180, ChaseDuration = 100;
        public CampaignState State { get; private set; } = new CampaignState();
        public bool Terminal => State.chapter == EscapeChapter.Won || State.chapter == EscapeChapter.Failed;
        public double Remaining => Math.Max(0, State.chapter == EscapeChapter.Search
            ? SearchDuration - State.elapsed : ChaseDuration - State.chaseElapsed);
        public bool Swarming => State.chapter == EscapeChapter.Search && State.elapsed >= 135 && State.elapsed < 139;
        public bool Silent => State.chapter == EscapeChapter.Search && State.elapsed >= 132 && State.elapsed < 135;

        public void Begin(int heartsA, int heartsB, Random random)
        {
            State = new CampaignState { chapter = EscapeChapter.Search, heartsA = heartsA, heartsB = heartsB,
                code = new[] { random.Next(1, 10), random.Next(1, 10), random.Next(1, 10) } };
        }
        public void Restore(CampaignState state) => State = state ?? new CampaignState();
        public void Advance(double dt)
        {
            if (dt < 0 || double.IsNaN(dt) || double.IsInfinity(dt)) throw new ArgumentOutOfRangeException(nameof(dt));
            if (State.chapter == EscapeChapter.Search)
            {
                State.elapsed = Math.Min(SearchDuration, State.elapsed + dt);
                if (State.elapsed >= 139 && !State.swarmHit)
                {
                    State.swarmHit = true;
                    State.heartsA = Math.Max(0, State.heartsA - 1);
                    State.heartsB = Math.Max(0, State.heartsB - 1);
                    if (State.heartsA == 0 || State.heartsB == 0) Fail("벌레 떼를 피하다 탁자에 부딪혔습니다. 팀의 체력이 소진되었습니다.");
                }
                if (State.elapsed >= SearchDuration && !Terminal) Fail("탐색 시간이 끝났습니다. 살인마가 방으로 돌아왔습니다.");
            }
            else if (State.chapter == EscapeChapter.Chase)
            {
                double before = State.chaseElapsed;
                State.chaseElapsed = Math.Min(ChaseDuration, before + dt);
                if (!State.gateOpen)
                {
                    double overlap = Math.Max(0, Math.Min(State.chaseElapsed, Math.Min(State.holdAUntil, State.holdBUntil)) - before);
                    State.gateProgress = overlap > 0 ? State.gateProgress + overlap : 0;
                    if (State.gateProgress >= 2 - 0.000001) State.gateOpen = true;
                }
                if (State.chaseElapsed >= ChaseDuration) Fail("출구가 봉쇄되었습니다. 두 사람이 함께 탈출해야 합니다.");
            }
        }
        public bool Act(EscapeRole role, int action)
        {
            if (role != EscapeRole.Blindfolded && role != EscapeRole.Observer) return false;
            int bit = 1 << (int)role;
            if (State.chapter == EscapeChapter.Search)
            {
                if (Swarming) return false;
                if (action >= 0 && action < 3) { State.evidence |= 1 << action; return true; }
                if (action >= 3 && action < 6) { int i = action - 3; State.entry[i] = (State.entry[i] + 1) % 10; return true; }
                if (action == 6 && State.evidence == 7 && State.entry[0] == State.code[0] &&
                    State.entry[1] == State.code[1] && State.entry[2] == State.code[2])
                { State.chapter = EscapeChapter.Chase; return true; }
            }
            else if (State.chapter == EscapeChapter.Chase)
            {
                if (action == 7 && role == EscapeRole.Blindfolded) { State.holdAUntil = State.chaseElapsed + .5; return true; }
                if (action == 8 && role == EscapeRole.Observer) { State.holdBUntil = State.chaseElapsed + .5; return true; }
                if (action == 9 && State.gateOpen && State.lifted == 0)
                { State.braceRole = (int)role; State.braceUntil = State.chaseElapsed + .5; return true; }
                if (action == 10 && State.gateOpen && (State.lifted & bit) == 0 &&
                    (State.ropeDown || (State.braceUntil > State.chaseElapsed && State.braceRole >= 0 && State.braceRole != (int)role)))
                {
                    State.lifted |= bit;
                    if (role == EscapeRole.Blindfolded) State.liftAtA = State.chaseElapsed; else State.liftAtB = State.chaseElapsed;
                    return true;
                }
                if ((State.lifted & bit) != 0 && State.chaseElapsed - (role == EscapeRole.Blindfolded ? State.liftAtA : State.liftAtB) < 1.4) return false;
                if (action == 11 && (State.lifted & bit) != 0) { State.ropeDown = true; return true; }
                if (action == 12 && State.lifted == 3)
                { State.exited |= bit; if (State.exited == 3) State.chapter = EscapeChapter.Won; return true; }
            }
            return false;
        }
        public void Fail(string reason)
        {
            if (Terminal) return;
            State.failure = reason;
            State.chapter = EscapeChapter.Failed;
        }
    }
}
