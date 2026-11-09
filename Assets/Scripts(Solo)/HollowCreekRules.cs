using System;

namespace HollowCreek
{
    public enum Route { Undecided, Upper, Prison }
    public enum Stage { Arrival, Entrance, Lift, Rescue, Acrostic, WeakWall, Search, Radio, Chase, Sealed, Vent, Ending, Dead }
    public enum Interaction { Profile, Entrance, Lift, Acrostic, WeakWall, NextArea, Wounded, Note1, Note2, Note3, Note4, Digits, Cabinet, Sliding, Radio, SealDoor, Vent, Exit, Guide }

    // Pure progression rules: UI and scene triggers both use these guards.
    [Serializable]
    public sealed class HollowCreekRules
    {
        public Route route;
        public Stage stage = Stage.Arrival;
        public int notes;
        public bool profileRead, passageOpen, puzzleSolved, cabinetOpened;
        public bool HasNote(int index) => index >= 0 && index < 4 && (notes & (1 << index)) != 0;
        public void ReadNote(int index) { if (index >= 0 && index < 4) notes |= 1 << index; }
        public bool ReadProfile() { if (stage != Stage.Arrival) return false; profileRead = true; return true; }
        public bool UnlockEntrance(string answer)
        {
            if (stage != Stage.Arrival || !profileRead || Clean(answer) != "1953") return false;
            stage = Stage.Entrance; return true;
        }
        public bool StartLift() { if (stage != Stage.Lift) return false; stage = Stage.Rescue; return true; }
        public bool ResolveRescue(bool caught)
        {
            if (stage != Stage.Rescue) return false;
            route = caught ? Route.Upper : Route.Prison;
            stage = caught ? Stage.Acrostic : Stage.WeakWall; return true;
        }
        public bool OpenPassage(string answer)
        {
            if (stage == Stage.Acrostic && Clean(answer).ToUpperInvariant() == "OPEN" || stage == Stage.WeakWall && answer == "PUSH")
            { passageOpen = true; return true; }
            return false;
        }
        public bool SolveDigits(string answer)
        {
            if (stage != Stage.Search || route != Route.Upper || Clean(answer) != "4") return false;
            puzzleSolved = true; stage = Stage.Radio; return true;
        }
        public static int SegmentMask(int digit)
        {
            // This facility uses the four-stroke variant of 7 (upper-left segment illuminated).
            int[] masks = { 63, 6, 91, 79, 102, 109, 125, 39, 127, 111 };
            return digit >= 0 && digit < masks.Length ? masks[digit] : 0;
        }
        public static int SegmentCount(int digit)
        {
            int mask = SegmentMask(digit), count = 0;
            while (mask != 0) { count += mask & 1; mask >>= 1; }
            return count;
        }
        public bool SolveSliding(int[] tiles)
        {
            if (stage != Stage.Search || route != Route.Prison || !SlidingPuzzle.IsSolved(tiles)) return false;
            puzzleSolved = true; stage = Stage.Radio; return true;
        }
        public bool TuneRadio(string answer) => stage == Stage.Radio && puzzleSolved && HasNote(3) && Clean(answer) == "1224";
        public bool Seal() { if (stage != Stage.Chase) return false; stage = Stage.Sealed; return true; }
        public bool EnterVent() { if (stage != Stage.Sealed) return false; stage = Stage.Vent; return true; }
        public bool Escape() { if (stage != Stage.Vent) return false; stage = Stage.Ending; return true; }
        static string Clean(string value) => (value ?? "").Trim();
    }

    public sealed class SlidingPuzzle
    {
        public int[] Tiles { get; private set; }
        public int Moves { get; private set; }
        public SlidingPuzzle(int seed)
        {
            Tiles = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
            var random = new Random(seed); int previous = -1;
            // Legal moves from the solved board guarantee a solvable permutation.
            for (int i = 0; i < 72; i++)
            {
                int empty = Array.IndexOf(Tiles, 0), choice;
                do { choice = random.Next(9); } while (choice == previous || !Adjacent(choice, empty));
                Move(choice); previous = empty;
            }
            if (IsSolved(Tiles)) Move(7);
            Moves = 0;
        }
        public bool Move(int index)
        {
            int empty = Array.IndexOf(Tiles, 0);
            if (index < 0 || index >= 9 || !Adjacent(index, empty)) return false;
            Tiles[empty] = Tiles[index]; Tiles[index] = 0; Moves++; return true;
        }
        public static bool Adjacent(int a, int b) => Math.Abs(a / 3 - b / 3) + Math.Abs(a % 3 - b % 3) == 1;
        public static bool IsSolved(int[] tiles)
        {
            if (tiles == null || tiles.Length != 9) return false;
            for (int i = 0; i < 8; i++) if (tiles[i] != i + 1) return false;
            return tiles[8] == 0;
        }
    }
}
