using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    // Drives the HORSE table: renders the procedural poker table, paces the AI, and handles touch input.
    public class PokerGame : MonoBehaviour
    {
        private HorseTable _table;
        private GameObject _dyn;
        private float _hH, _hW;
        private float _timer;

        private TextMesh _variantLabel, _handLabel, _potLabel, _msgLabel;
        private TouchButton _btnFold, _btnCall, _btnRaise, _btnDeal;
        private Sprite _face, _back;

        private int _lastHand, _lastStreet, _lastPot;
        private TableState _lastState;
        private bool _sfxReady;

        private void Start()
        {
            var cam = Camera.main;
            _hH = cam.orthographicSize;
            _hW = _hH * cam.aspect;

            _face = SpriteFactory.RoundedRect(100, 140, 16, Color.white, new Color(0.80f, 0.80f, 0.84f), 3);
            _back = SpriteFactory.RoundedRect(100, 140, 16, new Color(0.16f, 0.32f, 0.55f), new Color(0.85f, 0.88f, 0.95f), 6);

            _variantLabel = TextFactory.Create("Variant", null, new Vector3(0, _hH - 0.55f, 0), 46, Color.white);
            _handLabel = TextFactory.Create("Hand", null, new Vector3(0, _hH - 1.05f, 0), 28, new Color(0.78f, 0.85f, 0.78f));
            _potLabel = TextFactory.Create("Pot", null, new Vector3(0, 1.9f, 0), 36, new Color(1f, 0.9f, 0.5f));
            _msgLabel = TextFactory.Create("Msg", null, new Vector3(0, -_hH + 2.35f, 0), 30, new Color(0.95f, 0.95f, 0.7f));

            float bw = _hW * 0.58f;
            float bScale = Mathf.Min(1f, bw / 2.4f);
            float by = -_hH + 0.95f;
            _btnFold = MakeButton("FOLD", new Vector3(-_hW * 0.64f, by, 0), new Color(0.70f, 0.25f, 0.25f), bScale);
            _btnCall = MakeButton("CHECK", new Vector3(0, by, 0), new Color(0.20f, 0.45f, 0.80f), bScale);
            _btnRaise = MakeButton("RAISE", new Vector3(_hW * 0.64f, by, 0), new Color(0.20f, 0.60f, 0.32f), bScale);
            _btnDeal = MakeButton("DEAL", new Vector3(0, by, 0), new Color(0.30f, 0.55f, 0.35f), Mathf.Min(1f, (_hW * 0.8f) / 2.4f));

            _btnFold.Clicked += () => { if (_table.AwaitingHuman) { Sfx.Fold(); _table.HumanAct(ActionType.Fold); AfterHuman(); } };
            _btnCall.Clicked += () => { if (_table.AwaitingHuman) { Sfx.Click(); var v = _table.GetHumanOptions(); _table.HumanAct(v.CanCheck ? ActionType.Check : ActionType.Call); AfterHuman(); } };
            _btnRaise.Clicked += () => { if (_table.AwaitingHuman) { Sfx.Click(); var v = _table.GetHumanOptions(); _table.HumanAct(v.CanRaise ? ActionType.Raise : (v.CanCheck ? ActionType.Check : ActionType.Call)); AfterHuman(); } };
            _btnDeal.Clicked += () => { if (_table.State == TableState.HandOver) { Sfx.Click(); _table.StartHand(); _timer = 0; Render(); } };

            Sfx.Init(gameObject);
            _table = new HorseTable(4); // You + 3 AI opponents
            _lastHand = 0; _lastStreet = 0; _lastPot = 0; _lastState = TableState.HandOver; _sfxReady = true;
            _table.StartHand();
            Render();
        }

        private TouchButton MakeButton(string label, Vector3 pos, Color color, float scale)
        {
            var go = new GameObject(label);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var b = go.AddComponent<TouchButton>();
            b.Init(label, color);
            return b;
        }

        private void AfterHuman() { _timer = 0; Render(); }

        private void Update()
        {
            HandleTouch();

            if (_table.State == TableState.Running)
            {
                _timer += Time.deltaTime;
                if (_timer >= 0.7f) { _timer = 0; _table.StepOnce(); Render(); }
            }
            else if (_table.State == TableState.HandOver)
            {
                _timer += Time.deltaTime;
                if (_timer >= 4f) { _timer = 0; _table.StartHand(); Render(); }
            }
        }

        private void HandleTouch()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            Vector3 w = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(new Vector2(w.x, w.y));
            if (hit != null)
            {
                var b = hit.GetComponent<TouchButton>();
                if (b != null) b.Press();
            }
        }

        // ---------------- rendering ----------------

        private void Render()
        {
            if (_dyn != null) Destroy(_dyn);
            _dyn = new GameObject("Table");

            var v = _table.Variant;
            _variantLabel.text = VariantInfo.Title(v);

            string letters = "";
            for (int i = 0; i < 5; i++)
            {
                char l = VariantInfo.Letter((Variant)i);
                letters += i == (int)v ? "[" + l + "]" : " " + l + " ";
            }
            _handLabel.text = "Hand " + _table.HandNo + "    " + letters;
            _potLabel.text = "POT  $" + _table.Pot;

            bool over = _table.State == TableState.HandOver;

            // Opponents across the top, evenly spread for whatever the count is.
            int nOpp = _table.Players.Length - 1;
            for (int k = 0; k < nOpp; k++)
            {
                int seat = k + 1;
                float x = nOpp == 1 ? 0f : Mathf.Lerp(-_hW * 0.6f, _hW * 0.6f, k / (float)(nOpp - 1));
                DrawSeat(seat, new Vector3(x, _hH - 2.4f, 0), 0.42f, true);
            }

            // Community board.
            if (VariantInfo.IsCommunity(v) && _table.Board.Count > 0)
            {
                var faces = new List<KeyValuePair<Card, bool>>();
                foreach (var c in _table.Board) faces.Add(new KeyValuePair<Card, bool>(c, true));
                DrawCardRow(faces, new Vector3(0, 0.55f, 0), 0.58f);
            }

            // Your seat.
            DrawSeat(0, new Vector3(0, -_hH + 3.6f, 0), 0.6f, false);

            if (over) _msgLabel.text = _table.Message;
            else if (_table.AwaitingHuman) _msgLabel.text = "Your move";
            else _msgLabel.text = _table.Players[_table.ToActSeat].Name + " to act";

            UpdateButtons();
            FireSfx();
        }

        private void FireSfx()
        {
            if (!_sfxReady) return;
            if (_table.HandNo != _lastHand || (_table.Street != _lastStreet && _table.State != TableState.HandOver)) Sfx.Deal();
            if (_table.Pot > _lastPot) Sfx.Chip();
            if (_table.State == TableState.HandOver && _lastState != TableState.HandOver) Sfx.Win();
            _lastHand = _table.HandNo;
            _lastStreet = _table.Street;
            _lastPot = _table.Pot;
            _lastState = _table.State;
        }

        private void DrawSeat(int seat, Vector3 center, float cardScale, bool compact)
        {
            var p = _table.Players[seat];
            bool over = _table.State == TableState.HandOver;

            Color nameCol = p.Folded ? new Color(0.5f, 0.5f, 0.5f)
                : (seat == _table.ToActSeat && !over ? new Color(1f, 0.95f, 0.4f) : Color.white);
            var nm = TextFactory.Create("nm", _dyn.transform, center + new Vector3(0, compact ? 0.95f : 1.45f, 0), compact ? 26 : 32, nameCol);
            nm.text = p.Name + (p.Folded ? " (fold)" : "");

            string extra = "";
            if (over && p.LastResultDelta != 0) extra = "  (" + (p.LastResultDelta > 0 ? "+" : "") + p.LastResultDelta + ")";
            else if (p.StreetBet > 0) extra = "  bet " + p.StreetBet;
            var ch = TextFactory.Create("ch", _dyn.transform, center + new Vector3(0, compact ? 0.66f : 1.12f, 0), compact ? 22 : 26, new Color(1f, 0.92f, 0.6f));
            ch.text = "$" + p.Chips + extra;

            var faces = new List<KeyValuePair<Card, bool>>();
            foreach (var d in p.Down)
                faces.Add(new KeyValuePair<Card, bool>(d, p.IsHuman || (over && !p.Folded)));
            foreach (var u in p.Up)
                faces.Add(new KeyValuePair<Card, bool>(u, true));
            if (faces.Count > 0) DrawCardRow(faces, center, cardScale);
        }

        private void DrawCardRow(List<KeyValuePair<Card, bool>> cards, Vector3 center, float scale)
        {
            int n = cards.Count;
            float cw = 1.0f * scale;
            float step = cw * 1.12f;
            float startX = center.x - (step * n - (step - cw)) / 2f + cw / 2f;
            for (int k = 0; k < n; k++)
                SpawnCard(new Vector3(startX + step * k, center.y, 0), scale, cards[k].Value, cards[k].Key);
        }

        private void SpawnCard(Vector3 pos, float scale, bool faceUp, Card card)
        {
            var go = new GameObject("c");
            go.transform.SetParent(_dyn.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3;
            if (faceUp)
            {
                sr.sprite = _face;
                var t = TextFactory.Create("t", go.transform, new Vector3(0, 0, -0.1f), 70, card.Color, TextAnchor.MiddleCenter, 0.08f);
                t.text = card.RankLabel + "\n" + card.SuitLabel;
            }
            else
            {
                sr.sprite = _back;
            }
        }

        private void UpdateButtons()
        {
            bool human = _table.AwaitingHuman;
            _btnFold.gameObject.SetActive(human);
            _btnCall.gameObject.SetActive(human);
            _btnDeal.gameObject.SetActive(_table.State == TableState.HandOver);

            if (human)
            {
                var v = _table.GetHumanOptions();
                _btnCall.SetLabel(v.CanCheck ? "CHECK" : "CALL " + v.CallAmount);
                if (v.CanRaise)
                {
                    _btnRaise.gameObject.SetActive(true);
                    _btnRaise.SetLabel((v.RaiseIsBet ? "BET " : "RAISE ") + v.RaiseAmount);
                }
                else _btnRaise.gameObject.SetActive(false);
            }
            else
            {
                _btnRaise.gameObject.SetActive(false);
            }
        }
    }
}
