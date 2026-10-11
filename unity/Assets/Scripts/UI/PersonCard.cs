#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>A person card in the promo's style (promo/final/04 and 08), shared by the Cabinet and Defence screens: the
    /// framed portrait (or, with none in the catalog, a plain plate with the initials: never an invented likeness), the
    /// name, the office, the party, and competence and loyalty as two bars with their values out of 10 (people.yaml). A
    /// vacant office shows its title and "(vacant)" only. Element names start with the given prefix
    /// (<c>{prefix}-holder</c>, <c>-office</c>, <c>-party</c>, <c>-portrait</c>, <c>-initials</c>, <c>-competence</c>,
    /// <c>-competence-value</c>, <c>-loyalty</c>, <c>-loyalty-value</c>).</summary>
    public static class PersonCard
    {
        public const string PortraitClass = "card-portrait", InitialsClass = "card-initials", NameClass = "card-name",
            OfficeClass = "card-office", PartyClass = "card-party", StatClass = "card-stat";
        /// <summary>The scale of competence and loyalty (people.yaml; ContentLoader refuses values outside 0-10).</summary>
        public const int Scale = 10;

        /// <summary>The person's initials (first letter of the first and last words of the name), for the plate shown when
        /// there is no portrait.</summary>
        public static string Initials(string name)
        {
            var words = name.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "";
            return words.Length == 1 ? words[0].Substring(0, 1) : words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1);
        }

        /// <summary>Fills <paramref name="card"/> (cleared first). <paramref name="office"/> is the line under the name;
        /// <paramref name="portraitSize"/> is the portrait box in logical pixels (Statecraft.uss), used to keep the face in
        /// view.</summary>
        public static void Fill(VisualElement card, string prefix, Content c, GameState s, Person? person, string office, bool appointed,
                                UiText text, PortraitCatalog? portraits, Vector2 portraitSize, bool partyLabel = false)
        {
            card.Clear();
            var plate = new VisualElement { name = prefix + "-plate", pickingMode = PickingMode.Ignore };
            plate.AddToClassList("card-plate");
            var portrait = new VisualElement { name = prefix + "-portrait", pickingMode = PickingMode.Ignore };
            portrait.AddToClassList(PortraitClass);
            var entry = portraits?.Find(person?.Id);
            var tex = entry?.image;
            if (tex != null)
            {
                portrait.style.backgroundImage = new StyleBackground(tex);
                portrait.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top,
                    new Length(PortraitFraming.PositionYPercent(tex.width, tex.height, portraitSize.x, portraitSize.y, entry!.Value.focusY), LengthUnit.Percent));
            }
            portrait.style.display = tex != null ? DisplayStyle.Flex : DisplayStyle.None;   // never an invented portrait
            plate.Add(portrait);
            var initials = new Label(person == null || tex != null ? "" : Initials(person.Name)) { name = prefix + "-initials", pickingMode = PickingMode.Ignore };
            initials.AddToClassList(InitialsClass);
            initials.style.display = tex == null ? DisplayStyle.Flex : DisplayStyle.None;
            plate.Add(initials);
            card.Add(plate);

            var holder = new Label(person == null ? text["cabinet.vacant"] : person.Name + (appointed ? text["cabinet.appointed"] : "")) { name = prefix + "-holder", pickingMode = PickingMode.Ignore };
            holder.AddToClassList(NameClass);
            holder.EnableInClassList("appointed", appointed);
            card.Add(holder);
            var officeLabel = new Label(office) { name = prefix + "-office", pickingMode = PickingMode.Ignore };
            officeLabel.AddToClassList(OfficeClass);
            card.Add(officeLabel);
            if (person == null) return;

            var partyId = Offices.PartyOf(c, person, s.Date);
            var partyName = partyId == "" ? text["cabinet.no_party"] : c.Parties.First(x => x.Id == partyId).Name;
            var party = new Label(partyLabel ? text.Format("card.party", partyName) : partyName) { name = prefix + "-party", pickingMode = PickingMode.Ignore };
            party.AddToClassList(PartyClass);
            card.Add(party);
            card.Add(Stat(prefix + "-competence", text["card.competence"], person.Competence, "ui-bar--competence"));
            card.Add(Stat(prefix + "-loyalty", text["card.loyalty"], person.Loyalty, "ui-bar--loyalty"));
        }

        static VisualElement Stat(string name, string caption, int value, string modifier)
        {
            var row = new VisualElement { name = name + "-row", pickingMode = PickingMode.Ignore };
            row.AddToClassList(StatClass);
            var label = new Label(caption) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("card-stat-name");
            row.Add(label);
            var bar = new UiBar { name = name, Value = (float)value / Scale };
            bar.AddToClassList(modifier);
            row.Add(bar);
            var number = new Label(value.ToString(System.Globalization.CultureInfo.InvariantCulture)) { name = name + "-value", pickingMode = PickingMode.Ignore };
            number.AddToClassList("card-stat-value");
            row.Add(number);
            return row;
        }
    }
}
