#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Cabinet screen: the government of the day, its coalition, and every office with its holder (portrait,
    /// party, competence and loyalty). Selecting an office lists every person with Appoint, or the reason they cannot be
    /// appointed (Cabinet.Check). Appointing queues the Appoint order for End Turn.</summary>
    public sealed class CabinetPresenter : System.IDisposable
    {
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;

        public CabinetPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits)
        {
            screen = root.Q<VisualElement>("screen-cabinet"); this.bridge = bridge; this.text = text; this.portraits = portraits;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and removes the rows (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render;
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

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var gov = Offices.Government(c, s.Date);
            var (seats, size) = PoliticsSystem.CoalitionSeats(c, s.Date);
            screen.Q<Label>("cabinet-government").text = gov?.Name ?? "";
            screen.Q<Label>("cabinet-coalition").text = text.Format("cabinet.coalition", seats, size, s.CoalitionStability * 100);
            var list = screen.Q<ScrollView>("cabinet-seats");
            list.Clear();
            var lineup = Cabinet.Lineup(c, s);
            if (Selected != null && lineup.All(x => x.Office.Id != Selected)) Selected = null;   // that office no longer exists (abolished)
            foreach (var seat in lineup)
            {
                var office = seat.Office.Id;
                var row = new Button(() => Select(office)) { name = "seat-" + office };
                row.AddToClassList("seat");
                row.EnableInClassList("seat-selected", office == Selected);
                var portrait = new VisualElement();
                portrait.AddToClassList("seat-portrait");
                var tex = portraits?.Get(seat.Holder?.Id);
                if (tex != null) portrait.style.backgroundImage = new StyleBackground(tex);
                portrait.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;   // never an invented portrait
                row.Add(portrait);
                var who = seat.Holder == null ? text["cabinet.vacant"] : Describe(seat.Holder) + (seat.Appointed ? text["cabinet.appointed"] : "");
                var label = new Label(text.Format("cabinet.seat", seat.Office.Title, who)) { name = "seat-" + office + "-holder" };
                label.EnableInClassList("appointed", seat.Appointed);
                row.Add(label);
                list.Add(row);
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

        void Appoint(string office, string person)
        {
            // The bridge reports a refusal itself (Refused); the sim re-checks legality when the turn runs.
            bridge.Queue(new Appoint(office, person));
        }
    }
}
