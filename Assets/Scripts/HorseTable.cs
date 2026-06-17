using System.Collections.Generic;

namespace CardGame
{
    public enum ActionType { Fold, Check, Call, Raise }
    public enum TableState { Running, WaitingHuman, HandOver }

    // Legal options presented to the human when it is their turn.
    public struct ActionView
    {
        public bool CanCheck;
        public bool CanCall;
        public int CallAmount;
        public bool CanRaise;
        public int RaiseAmount;   // total chips the raise/bet costs the player now
        public bool RaiseIsBet;   // true when there is no outstanding bet (label "Bet" vs "Raise")
    }

    // The HORSE engine: deals all five variants, runs fixed-limit betting, resolves side pots and hi-lo splits.
    public class HorseTable
    {
        public const int StartChips = 1000;
        public const int SmallBet = 20, BigBet = 40;
        public const int SmallBlind = 10, BigBlind = 20;
        public const int Ante = 5, BringIn = 10;
        public const int MaxBetsPerStreet = 4;

        public readonly Player[] Players;
        public int Button = -1;
        public Variant Variant;
        public int HandNo;
        public readonly List<Card> Board = new List<Card>();
        public TableState State = TableState.HandOver;
        public string Message = "";
        public readonly List<string> ShowdownLines = new List<string>();

        private readonly Deck _deck;
        private readonly int _n;
        private int _street;
        private int _toAct;
        private int _currentBet;
        private int _betSize;
        private int _betsMade;
        private int _toRespond;
        private bool _bettingOpen;
        public int HumanActor = -1;

        public HorseTable(int players, int seed = 0)
        {
            _n = players;
            Players = new Player[players];
            Players[0] = new Player("You", StartChips, true);
            string[] bots = { "Ada", "Boyd", "Cleo", "Dina", "Esme", "Finn", "Gwen" };
            for (int i = 1; i < players; i++) Players[i] = new Player(bots[(i - 1) % bots.Length], StartChips, false);
            _deck = new Deck(seed);
        }

        public int Pot
        {
            get { int p = 0; foreach (var pl in Players) p += pl.Contributed; return p; }
        }

        public int ToActSeat => _toAct;
        public bool AwaitingHuman => State == TableState.WaitingHuman;

        // ---------------- Hand setup ----------------

        public void StartHand()
        {
            // Reset busted tables so the game is endless.
            int solvent = 0; foreach (var p in Players) if (p.Chips > 0) solvent++;
            if (solvent < 2) foreach (var p in Players) p.Chips = StartChips;

            HandNo++;
            Variant = (Variant)((HandNo - 1) % 5);
            Board.Clear();
            ShowdownLines.Clear();
            _deck.Reset();
            foreach (var p in Players) p.ResetForHand();
            // Players with no chips sit this hand out.
            foreach (var p in Players) if (p.Chips <= 0) p.Folded = true;

            _street = 0;
            Message = VariantInfo.Title(Variant);

            if (VariantInfo.IsCommunity(Variant)) SetupFlopGame();
            else SetupStudGame();

            State = TableState.Running;
        }

        private void SetupFlopGame()
        {
            int holes = Variant == Variant.OmahaHiLo ? 4 : 2;
            // advance button to next solvent seat
            Button = NextSeat(Button < 0 ? _n - 1 : Button, p => !p.Folded);
            for (int k = 0; k < holes; k++)
                ForEachInHandFrom(Button, p => p.Down.Add(_deck.Deal()));

            int sb = NextSeat(Button, p => !p.Folded);
            int bb = NextSeat(sb, p => !p.Folded);
            Players[sb].Commit(SmallBlind);
            Players[bb].Commit(BigBlind);

            BeginBetting(NextSeat(bb, p => p.CanAct), BigBlind, SmallBet, countForcedBet: false);
        }

        private void SetupStudGame()
        {
            foreach (var p in Players) if (!p.Folded) { int a = System.Math.Min(Ante, p.Chips); p.Chips -= a; p.Contributed += a; }
            // 3rd street: 2 down + 1 up each
            for (int k = 0; k < 2; k++) ForEachInHandFrom(0, p => p.Down.Add(_deck.Deal()));
            ForEachInHandFrom(0, p => p.Up.Add(_deck.Deal()));

            // Bring-in: lowest up card (Razz: highest up card)
            bool razz = Variant == Variant.Razz;
            int bring = -1; int bestRank = razz ? -1 : 99;
            for (int i = 0; i < _n; i++)
            {
                if (Players[i].Folded) continue;
                int r = Players[i].Up[0].Rank;
                if (razz ? r > bestRank : r < bestRank) { bestRank = r; bring = i; }
            }
            Players[bring].Commit(BringIn);
            BeginBetting(NextSeat(bring, p => p.CanAct), BringIn, SmallBet, countForcedBet: false);
        }

        // ---------------- Betting ----------------

        private void BeginBetting(int firstActor, int currentBet, int betSize, bool countForcedBet)
        {
            _bettingOpen = true;
            _currentBet = currentBet;
            _betSize = betSize;
            _betsMade = countForcedBet ? 1 : 0;
            _toAct = SeatCanActFrom(firstActor);
            int canAct = CountCanAct();
            _toRespond = canAct <= 1 ? 0 : canAct;
        }

        public ActionView GetHumanOptions()
        {
            var p = Players[_toAct];
            int toCall = _currentBet - p.StreetBet;
            var v = new ActionView();
            v.CanCheck = toCall <= 0;
            v.CanCall = toCall > 0;
            v.CallAmount = System.Math.Min(toCall, p.Chips);
            int target = _currentBet < _betSize ? _betSize : _currentBet + _betSize;
            v.RaiseAmount = System.Math.Min(target - p.StreetBet, p.Chips);
            v.CanRaise = _betsMade < MaxBetsPerStreet && p.Chips > toCall && CountCanAct() > 1;
            v.RaiseIsBet = _currentBet == 0;
            return v;
        }

        public void HumanAct(ActionType a)
        {
            if (State != TableState.WaitingHuman) return;
            ApplyAction(Players[_toAct], a);
            State = TableState.Running;
        }

        private void ApplyAction(Player p, ActionType a)
        {
            int toCall = _currentBet - p.StreetBet;
            switch (a)
            {
                case ActionType.Fold:
                    p.Folded = true; _toRespond--; break;
                case ActionType.Check:
                    _toRespond--; break;
                case ActionType.Call:
                    p.Commit(System.Math.Min(toCall, p.Chips)); _toRespond--; break;
                case ActionType.Raise:
                    if (_betsMade >= MaxBetsPerStreet || p.Chips <= toCall) { goto case ActionType.Call; }
                    int target = _currentBet < _betSize ? _betSize : _currentBet + _betSize;
                    p.Commit(target - p.StreetBet);
                    _currentBet = System.Math.Max(_currentBet, p.StreetBet);
                    _betsMade++;
                    _toRespond = CountOthersCanAct(p);
                    break;
            }
            _toAct = SeatCanActFrom(NextSeatRaw(_toAct));
        }

        private void AiAct(Player p)
        {
            var view = GetOptionsFor(p);
            ActionType a = Ai.Decide(this, p, view, _currentBet, _betSize);
            ApplyAction(p, a);
        }

        private ActionView GetOptionsFor(Player p)
        {
            int saved = _toAct;
            // GetHumanOptions reads _toAct; reuse it for any player.
            int toCall = _currentBet - p.StreetBet;
            var v = new ActionView();
            v.CanCheck = toCall <= 0;
            v.CanCall = toCall > 0;
            v.CallAmount = System.Math.Min(toCall, p.Chips);
            int target = _currentBet < _betSize ? _betSize : _currentBet + _betSize;
            v.RaiseAmount = System.Math.Min(target - p.StreetBet, p.Chips);
            v.CanRaise = _betsMade < MaxBetsPerStreet && p.Chips > toCall && CountCanAct() > 1;
            v.RaiseIsBet = _currentBet == 0;
            _toAct = saved;
            return v;
        }

        // Advance the hand by one step. Returns true if something happened.
        public bool StepOnce()
        {
            if (State == TableState.HandOver || State == TableState.WaitingHuman) return false;

            if (CountInHand() <= 1) { AwardUncontested(); return true; }

            if (_bettingOpen)
            {
                if (_toRespond <= 0 || CountCanAct() == 0)
                {
                    _bettingOpen = false;
                    foreach (var p in Players) p.StreetBet = 0;
                    ProceedAfterStreet();
                    return true;
                }
                var actor = Players[_toAct];
                if (!actor.CanAct) { _toAct = SeatCanActFrom(NextSeatRaw(_toAct)); return true; }
                if (actor.IsHuman) { State = TableState.WaitingHuman; HumanActor = _toAct; return false; }
                AiAct(actor);
                return true;
            }
            return false;
        }

        private void ProceedAfterStreet()
        {
            int last = VariantInfo.IsCommunity(Variant) ? 3 : 4;
            if (_street >= last) { Showdown(); return; }
            _street++;
            DealStreet(_street);
            int firstActor = FirstActorForStreet();
            int betSize = BetSizeForStreet(_street);
            BeginBetting(firstActor, 0, betSize, countForcedBet: false);
        }

        private void DealStreet(int street)
        {
            if (VariantInfo.IsCommunity(Variant))
            {
                int add = street == 1 ? 3 : 1; // flop=3, turn/river=1
                for (int k = 0; k < add; k++) Board.Add(_deck.Deal());
            }
            else
            {
                if (street <= 3) ForEachInHandFrom(0, p => p.Up.Add(_deck.Deal()));  // 4th,5th,6th up
                else ForEachInHandFrom(0, p => p.Down.Add(_deck.Deal()));            // 7th down
            }
        }

        private int BetSizeForStreet(int street)
        {
            if (VariantInfo.IsCommunity(Variant)) return street <= 1 ? SmallBet : BigBet;
            return street <= 1 ? SmallBet : BigBet;
        }

        private int FirstActorForStreet()
        {
            if (VariantInfo.IsCommunity(Variant))
                return SeatCanActFrom(NextSeat(Button, p => p.CanAct));
            // Stud: best showing up-cards acts first (high for stud/stud8, low for razz).
            bool razz = Variant == Variant.Razz;
            int best = -1; int[] bestKey = null;
            for (int i = 0; i < _n; i++)
            {
                if (!Players[i].CanAct) continue;
                int[] key = UpKey(Players[i]);
                if (bestKey == null || (razz ? Less(key, bestKey) : Less(bestKey, key))) { bestKey = key; best = i; }
            }
            return best < 0 ? SeatCanActFrom(0) : best;
        }

        private static int[] UpKey(Player p)
        {
            var ranks = new List<int>();
            foreach (var c in p.Up) ranks.Add(c.Rank);
            ranks.Sort(); ranks.Reverse();
            return ranks.ToArray();
        }

        private static bool Less(int[] a, int[] b)
        {
            int n = System.Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++) if (a[i] != b[i]) return a[i] < b[i];
            return a.Length < b.Length;
        }

        // ---------------- Showdown & pots ----------------

        private void AwardUncontested()
        {
            int total = Pot;
            foreach (var p in Players)
                if (!p.Folded) { p.Chips += total; p.LastResultDelta = total - p.Contributed; Message = p.Name + " wins " + total; }
            foreach (var p in Players) if (p.Folded) p.LastResultDelta = -p.Contributed;
            State = TableState.HandOver;
        }

        private struct Eval { public HiValue Hi; public HiValue Lo; public bool LoQ; }

        private void Showdown()
        {
            var ev = new Eval[_n];
            for (int i = 0; i < _n; i++)
            {
                if (Players[i].Folded) continue;
                ev[i] = EvaluateSeat(i);
            }

            // Build side pots from contributions.
            var levels = new List<int>();
            foreach (var p in Players) if (p.Contributed > 0 && !levels.Contains(p.Contributed)) levels.Add(p.Contributed);
            levels.Sort();

            int[] delta = new int[_n];
            int prev = 0;
            foreach (int L in levels)
            {
                int contributors = 0;
                foreach (var p in Players) if (p.Contributed >= L) contributors++;
                int amount = (L - prev) * contributors;
                prev = L;
                if (amount <= 0) continue;

                var eligible = new List<int>();
                for (int i = 0; i < _n; i++) if (!Players[i].Folded && Players[i].Contributed >= L) eligible.Add(i);
                AwardPot(amount, eligible, ev, delta);
            }

            for (int i = 0; i < _n; i++) { Players[i].Chips += delta[i]; Players[i].LastResultDelta = delta[i] - Players[i].Contributed; }
            BuildShowdownText(ev);
            State = TableState.HandOver;
        }

        private Eval EvaluateSeat(int i)
        {
            var p = Players[i];
            var e = new Eval();
            if (Variant == Variant.Holdem)
            {
                var cards = p.Down.GetRange(0, p.Down.Count); cards.AddRange(Board);
                e.Hi = HandEval.High(cards);
            }
            else if (Variant == Variant.OmahaHiLo)
            {
                e.Hi = HandEval.OmahaHigh(p.Down, Board);
                e.Lo = HandEval.OmahaLow8(p.Down, Board, out e.LoQ);
            }
            else if (Variant == Variant.Razz)
            {
                e.Lo = HandEval.RazzLow(p.AllCards()); e.LoQ = true;
            }
            else if (Variant == Variant.Stud)
            {
                e.Hi = HandEval.High(p.AllCards());
            }
            else // StudHiLo
            {
                var all = p.AllCards();
                e.Hi = HandEval.High(all);
                e.Lo = HandEval.Low8(all, out e.LoQ);
            }
            return e;
        }

        private void AwardPot(int amount, List<int> eligible, Eval[] ev, int[] delta)
        {
            bool hiLo = VariantInfo.HasLow(Variant);
            bool lowOnly = VariantInfo.LowOnly(Variant);

            if (lowOnly)
            {
                var winners = BestLow(eligible, ev);
                SplitTo(amount, winners, delta);
                return;
            }
            if (!hiLo)
            {
                var winners = BestHigh(eligible, ev);
                SplitTo(amount, winners, delta);
                return;
            }
            // Hi-lo split with 8-or-better qualifier.
            var lowElig = new List<int>();
            foreach (int i in eligible) if (ev[i].LoQ) lowElig.Add(i);
            var lowWinners = lowElig.Count > 0 ? BestLow(lowElig, ev) : new List<int>();
            var highWinners = BestHigh(eligible, ev);
            if (lowWinners.Count == 0) { SplitTo(amount, highWinners, delta); return; }
            int lowHalf = amount / 2;
            int highHalf = amount - lowHalf;
            SplitTo(highHalf, highWinners, delta);
            SplitTo(lowHalf, lowWinners, delta);
        }

        private List<int> BestHigh(List<int> seats, Eval[] ev)
        {
            var win = new List<int>(); int best = -1;
            foreach (int i in seats)
            {
                if (best < 0) { best = i; win.Clear(); win.Add(i); continue; }
                int cmp = ev[i].Hi.CompareTo(ev[best].Hi);
                if (cmp > 0) { best = i; win.Clear(); win.Add(i); }
                else if (cmp == 0) win.Add(i);
            }
            return win;
        }

        private List<int> BestLow(List<int> seats, Eval[] ev)
        {
            var win = new List<int>(); int best = -1;
            foreach (int i in seats)
            {
                if (best < 0) { best = i; win.Clear(); win.Add(i); continue; }
                int cmp = ev[i].Lo.CompareTo(ev[best].Lo);
                if (cmp < 0) { best = i; win.Clear(); win.Add(i); }
                else if (cmp == 0) win.Add(i);
            }
            return win;
        }

        private void SplitTo(int amount, List<int> winners, int[] delta)
        {
            if (winners.Count == 0) return;
            int each = amount / winners.Count;
            int rem = amount - each * winners.Count;
            foreach (int i in winners) delta[i] += each;
            delta[winners[0]] += rem; // odd chip to first
        }

        private void BuildShowdownText(Eval[] ev)
        {
            ShowdownLines.Clear();
            for (int i = 0; i < _n; i++)
            {
                var p = Players[i];
                if (p.Folded) continue;
                string hand = VariantInfo.LowOnly(Variant) ? "low " + LowText(ev[i].Lo) : ev[i].Hi.Name;
                if (VariantInfo.HasLow(Variant) && ev[i].LoQ) hand += " / low " + LowText(ev[i].Lo);
                string res = p.LastResultDelta >= 0 ? "+" + p.LastResultDelta : p.LastResultDelta.ToString();
                ShowdownLines.Add($"{p.Name}: {hand} ({res})");
            }
        }

        private static string LowText(HiValue lo)
        {
            if (!lo.HasValue) return "-";
            int[] k = { lo.K0, lo.K1, lo.K2, lo.K3, lo.K4 };
            var sb = new System.Text.StringBuilder();
            foreach (int r in k) { if (r == 0) continue; sb.Append(r == 1 ? "A" : r.ToString()); }
            return sb.ToString();
        }

        // ---------------- seat helpers ----------------

        private int NextSeatRaw(int seat) => (seat + 1) % _n;

        private int NextSeat(int seat, System.Func<Player, bool> ok)
        {
            for (int k = 1; k <= _n; k++) { int s = (seat + k) % _n; if (ok(Players[s])) return s; }
            return seat;
        }

        private int SeatCanActFrom(int seat)
        {
            for (int k = 0; k < _n; k++) { int s = (seat + k) % _n; if (Players[s].CanAct) return s; }
            return seat;
        }

        private void ForEachInHandFrom(int start, System.Action<Player> act)
        {
            for (int k = 0; k < _n; k++) { int s = (start + k) % _n; if (!Players[s].Folded) act(Players[s]); }
        }

        private int CountInHand() { int c = 0; foreach (var p in Players) if (!p.Folded) c++; return c; }
        private int CountCanAct() { int c = 0; foreach (var p in Players) if (p.CanAct) c++; return c; }
        private int CountOthersCanAct(Player except) { int c = 0; foreach (var p in Players) if (p.CanAct && p != except) c++; return c; }

        public int CurrentBet => _currentBet;
        public int Street => _street;
    }
}
