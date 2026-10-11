#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Cabinet screen, laid out as promo/final/04: the government of the day and the body it answers to; the
    /// Prime Minister's card at the side (every office the PM holds); the legislature as a half circle of seats coloured by
    /// party with the majority line, and the coalition's seats, the opposition's, the largest party and coalition stability
    /// beside it; and a card for every other office with its holder (portrait, party, competence and loyalty bars). Every
    /// number is the sim's: legislatures.yaml seats and size, governments.yaml coalition, people.yaml competence and
    /// loyalty, GameState coalition stability. Party colours are a UI palette (the coalition's parties in blues, the
    /// opposition's in greys, alternating so neighbours differ), not the parties' own colours, which content does not
    /// record. Selecting an office lists every person with Appoint, or the reason they cannot be appointed (Cabinet.Check).
    /// Appointing queues the Appoint order for End Turn.</summary>
    public sealed class CabinetPresenter : System.IDisposable
    {
        public const string PrimeMinister = "prime_minister";
        /// <summary>Portrait boxes in logical pixels (Statecraft.uss .seat / .pm-card .card-portrait).</summary>
        public static readonly Vector2 CardPortrait = new Vector2(188f, 150f), PmPortrait = new Vector2(286f, 250f);
        /// <summary>The palette: coalition blues and opposition greys, each at least 3:1 against the screen's navy.</summary>
        public static readonly Color[] CoalitionColours = { new Color32(92, 138, 200, 255), new Color32(146, 184, 232, 255), new Color32(74, 116, 180, 255), new Color32(120, 162, 218, 255) };
        public static readonly Color[] OppositionColours = { new Color32(176, 170, 158, 255), new Color32(132, 128, 120, 255), new Color32(208, 202, 190, 255) };

        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly Button pmCard; readonly System.Action onPm;

        public CabinetPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits)
        {
            screen = root.Q<VisualElement>("screen-cabinet"); this.bridge = bridge; this.text = text; this.portraits = portraits;
            pmCard = screen.Q<Button>("seat-" + PrimeMinister);
            onPm = () => Select(PrimeMinister);
            pmCard.clicked += onPm;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and removes the rows (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render;
            pmCard.clicked -= onPm;
            screen.Q<ScrollView>("cabinet-seats")?.Clear();
            screen.Q<ScrollView>("cabinet-candidates")?.Clear();
            screen.Q<VisualElement>("cabinet-pick").style.display = DisplayStyle.None;
        }

        /// <summary>The office whose candidates are listed, or null.</summary>
        public string? Selected { get; private set; }

        public void Select(string? office) { Selected = office; Render(); }

        string Describe(Person p)
        {
            var c = bridge.Content; var party = Offices.PartyOf(c, p, bridge.State.Date);
            var partyName = party == "" ? text["cabinet.no_party"] : c.Parties.First(x => x.Id == party).Name;
            return text.Format("cabinet.person", p.Name, partyName, p.Competence, p.Loyalty);
        }

        /// <summary>The legislature's party blocks in drawing order: the coalition's parties, then the opposition's, each by
        /// seats (most first, then content order), with their palette colours. Parties without a seat are left out.</summary>
        public static IReadOnlyList<Hemicycle.Group> Blocks(Content c, Legislature leg, Government? gov)
        {
            var order = c.Parties.Select((p, i) => (p.Id, i)).ToDictionary(x => x.Id, x => x.i);
            int Order(string id) => order.TryGetValue(id, out var i) ? i : int.MaxValue;
            var inCoalition = new HashSet<string>(gov?.Coalition ?? new List<string>());
            var blocks = new List<Hemicycle.Group>();
            foreach (var coalition in new[] { true, false })
            {
                var palette = coalition ? CoalitionColours : OppositionColours;
                var parties = leg.Seats.Where(kv => kv.Value > 0 && inCoalition.Contains(kv.Key) == coalition)
                    .OrderByDescending(kv => kv.Value).ThenBy(kv => Order(kv.Key)).ToList();
                for (int i = 0; i < parties.Count; i++)
                    blocks.Add(new Hemicycle.Group(parties[i].Key, parties[i].Value, palette[i % palette.Length], coalition));
            }
            return blocks;
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var gov = Offices.Government(c, s.Date);
            var leg = Offices.Legislature(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            screen.Q<Label>("cabinet-government").text = gov?.Name ?? "";
            var legislature = screen.Q<Label>("cabinet-legislature");
            legislature.text = leg == null ? "" : text.Format("cabinet.legislature", leg.Body, leg.Size);
            RenderHouse(c, s, gov, leg, seats, size);

            var lineup = Cabinet.Lineup(c, s);
            if (Selected != null && lineup.All(x => x.Office.Id != Selected)) Selected = null;   // that office no longer exists (abolished)

            // The Prime Minister's card, with every office the PM holds today (Ben-Gurion was also Minister of Defense).
            var pm = pmCard;
            var pmSeat = lineup.FirstOrDefault(x => x.Office.Id == PrimeMinister);
            pm.EnableInClassList("seat-selected", Selected == PrimeMinister);
            if (pmSeat != null)
            {
                var offices = pmSeat.Holder == null ? pmSeat.Office.Title
                    : string.Join(text["card.offices_separator"], lineup.Where(x => x.Holder?.Id == pmSeat.Holder.Id).Select(x => x.Office.Title));
                PersonCard.Fill(pm, "seat-" + PrimeMinister, c, s, pmSeat.Holder, offices, pmSeat.Appointed, text, portraits, PmPortrait, partyLabel: true);
            }

            var list = screen.Q<ScrollView>("cabinet-seats");
            list.Clear();
            foreach (var seat in lineup.Where(x => x.Office.Id != PrimeMinister))
            {
                var office = seat.Office.Id;
                var card = new Button(() => Select(office)) { name = "seat-" + office };
                card.AddToClassList("seat");
                card.EnableInClassList("seat-selected", office == Selected);
                card.EnableInClassList("seat-vacant", seat.Holder == null);
                PersonCard.Fill(card, "seat-" + office, c, s, seat.Holder, seat.Office.Title, seat.Appointed, text, portraits, CardPortrait);
                list.Add(card);
            }

            var pick = screen.Q<VisualElement>("cabinet-pick");
            var candidates = screen.Q<ScrollView>("cabinet-candidates");
            candidates.Clear();
            pick.style.display = Selected == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Selected != null)
            {
                var office = c.Offices.First(o => o.Id == Selected);
                screen.Q<Label>("cabinet-pick-title").text = text.Format("cabinet.pick", office.Title);
                foreach (var (person, reason) in Cabinet.Candidates(c, s, Selected))
                {
                    var p = person;
                    var row = new VisualElement { name = "cand-" + p.Id };
                    row.AddToClassList("candidate");
                    var appoint = new Button(() => Appoint(office.Id, p.Id)) { name = "appoint-" + p.Id, text = Describe(p) };
                    appoint.SetEnabled(reason == null);
                    row.Add(appoint);
                    if (reason != null) { var why = new Label(reason) { name = "cand-" + p.Id + "-reason" }; why.AddToClassList("reason"); row.Add(why); }
                    candidates.Add(row);
                }
            }
            var queued = bridge.Queued.OfType<Appoint>().Select(a =>
                text.Format("cabinet.queued", c.People.First(p => p.Id == a.Person).Name, c.Offices.First(o => o.Id == a.Office).Title));
            screen.Q<Label>("cabinet-status").text = string.Join("\n", queued);
        }

        /// <summary>The half circle of seats and its legend.</summary>
        void RenderHouse(Content c, GameState s, Government? gov, Legislature? leg, int seats, int size)
        {
            var house = screen.Q<VisualElement>("cabinet-house");
            house.style.display = leg == null ? DisplayStyle.None : DisplayStyle.Flex;
            var hemicycle = screen.Q<Hemicycle>("cabinet-hemicycle");
            var chips = screen.Q<VisualElement>("cabinet-parties");
            chips.Clear();
            screen.Q<Label>("cabinet-coalition").text = text.Format("cabinet.coalition", seats, size, PlayerText.Percent(s.CoalitionStability));
            screen.Q<UiBar>("cabinet-stability").Value = (float)s.CoalitionStability;
            if (leg == null) { hemicycle.Set(new Hemicycle.Group[0], 0); return; }
            var blocks = Blocks(c, leg, gov);
            hemicycle.Set(blocks, leg.Size);
            screen.Q<Label>("cabinet-house-size").text = leg.Size.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var opposition = blocks.Where(b => !b.Coalition).Sum(b => b.Seats);
            screen.Q<Label>("cabinet-opposition").text = text.Format("cabinet.opposition", opposition);
            screen.Q<Label>("cabinet-majority").text = text.Format("cabinet.majority", hemicycle.Majority);
            var largest = blocks.OrderByDescending(b => b.Seats).FirstOrDefault();
            var largestLabel = screen.Q<Label>("cabinet-largest");
            largestLabel.text = largest.Seats > 0 ? text.Format("cabinet.largest", c.Parties.First(p => p.Id == largest.Party).Name, largest.Seats) : "";
            var unrecorded = leg.Size - blocks.Sum(b => b.Seats);
            foreach (var b in blocks) chips.Add(Chip("party-" + b.Party, b.Colour, text.Format("cabinet.party_seats", c.Parties.First(p => p.Id == b.Party).Name, b.Seats), false));
            if (unrecorded > 0) chips.Add(Chip("party-unrecorded", Color.clear, text.Format("cabinet.party_seats", text["cabinet.unrecorded"], unrecorded), true));
        }

        static VisualElement Chip(string name, Color colour, string caption, bool hollow)
        {
            var chip = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            chip.AddToClassList("party-chip");
            var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
            swatch.AddToClassList("party-swatch");
            swatch.EnableInClassList("party-swatch--hollow", hollow);
            if (!hollow) swatch.style.backgroundColor = colour;
            chip.Add(swatch);
            var label = new Label(caption) { name = name + "-label", pickingMode = PickingMode.Ignore };
            chip.Add(label);
            return chip;
        }

        void Appoint(string office, string person)
        {
            // The bridge reports a refusal itself (Refused); the sim re-checks legality when the turn runs.
            bridge.Queue(new Appoint(office, person));
        }
    }
}
