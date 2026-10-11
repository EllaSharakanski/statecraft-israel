#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The event card (right of the screen): the first pending event the player has not yet answered, with its
    /// header picture (an archival photograph of the event with its credit line, as the promo's card; EventImageCatalog) or,
    /// when the event has none, the responsible minister's portrait, the text with today's office holders filled in, and
    /// one button per option.
    /// A choice is an order: it takes effect at End Turn, and an unanswered event is delegated to the minister.</summary>
    public sealed class EventCardPresenter : System.IDisposable
    {
        /// <summary>The portrait thumbnail's size in the card (Statecraft.uss .event-image).</summary>
        public const float PortraitWidth = 96f, PortraitHeight = 128f;
        /// <summary>The class on each option button (Statecraft.uss).</summary>
        public const string OptionClass = "event-option";

        readonly VisualElement card; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly EventImageCatalog? photos;
        readonly VisualElement band, head; readonly Label credit;

        public EventCardPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits, EventImageCatalog? photos = null)
        {
            card = root.Q<VisualElement>("event-card"); this.bridge = bridge; this.text = text; this.portraits = portraits; this.photos = photos;
            band = card.Q<VisualElement>("event-photo"); credit = card.Q<Label>("event-photo-credit");
            // The band and its credit settle the drop (FitPhoto); the column, which only the window sizes, brings a dropped
            // photograph back when the window grows. A hidden element keeps its old layout, so a band shown again at the
            // size it was dropped at gets no geometry event of its own; the head below it always moves, so it checks too.
            head = card.Q<VisualElement>(className: "event-head");
            band.RegisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            credit.RegisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            head.RegisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            card.parent?.RegisterCallback<GeometryChangedEvent>(OnColumnGeometry);
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge, removes the option buttons and hides the card (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render;
            band.UnregisterCallback<GeometryChangedEvent>(OnPhotoGeometry); credit.UnregisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            head.UnregisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            card.parent?.UnregisterCallback<GeometryChangedEvent>(OnColumnGeometry);
            card.Q<VisualElement>("event-options")?.Clear(); card.style.display = DisplayStyle.None;
        }

        /// <summary>The event the card shows, or null when every pending event has a queued answer.</summary>
        public GameEvent? Shown { get; private set; }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var pending = s.PendingEvents.Select(id => c.Events.First(e => e.Id == id)).ToList();
            var open = pending.Where(e => bridge.QueuedLike(new ChooseEventOption(e.Id, "")) == null).ToList();
            Shown = open.FirstOrDefault();
            card.style.display = Shown == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (Shown == null) { ShowPhoto(null); return; }
            var e = Shown;
            var date = EventSystem.FillDate(e, s.Date);
            var holder = e.Office == "" ? null : Cabinet.Holder(c, s, e.Office, date);
            // The header photograph when the event has one; otherwise the minister's portrait plate (the fallback).
            var photo = photos?.Find(e.Id);
            ShowPhoto(photo);
            var image = card.Q<VisualElement>("event-image");
            var entry = photo == null ? portraits?.Find(holder?.Id) : null;
            var portrait = entry?.image;
            image.style.backgroundImage = portrait == null ? (StyleBackground)StyleKeyword.None : new StyleBackground(portrait);
            image.style.display = portrait == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (portrait != null)   // keep the face in view (playtest 2, P2-5)
                image.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top,
                    new Length(PortraitFraming.PositionYPercent(portrait.width, portrait.height, PortraitWidth, PortraitHeight, entry!.Value.focusY), LengthUnit.Percent));
            card.Q<Label>("event-draft").style.display = e.Status == "draft" ? DisplayStyle.Flex : DisplayStyle.None;
            card.Q<Label>("event-title").text = Cabinet.Fill(c, s, e.Title, date);
            card.Q<Label>("event-office").text = holder == null ? "" : text.Format("event.office", c.Offices.First(o => o.Id == e.Office).Title, holder.Name);
            card.Q<Label>("event-text").text = EventSystem.Render(c, s, e);
            var options = card.Q<VisualElement>("event-options");
            options.Clear();
            foreach (var o in e.Options)
            {
                var option = o;
                var b = new Button(() => Choose(e, option)) { name = "option-" + o.Id, text = Cabinet.Fill(c, s, o.Text, date) };
                b.AddToClassList(OptionClass);
                b.generateVisualContent += PaintSign;                      // the promo's round sign at the option's left
                options.Add(b);
            }
            card.Q<Label>("event-count").text = open.Count > 1 ? text.Format("event.more", open.Count - 1) : "";
        }

        /// <summary>Shows the event's photograph with its credit line, its alt text as the picture's tooltip, or hides both.</summary>
        void ShowPhoto(EventImageCatalog.Entry? photo)
        {
            current = photo;
            credit.text = photo?.credit ?? "";
            SetPhotoShown(photo != null);
        }

        /// <summary>Shows the current photograph (band, credit, tooltip, texture) or hides all of it. Idempotent.</summary>
        void SetPhotoShown(bool shown)
        {
            var photo = shown ? current : null;
            var display = photo != null ? DisplayStyle.Flex : DisplayStyle.None;
            band.style.display = display; credit.style.display = display;
            band.style.backgroundImage = photo == null ? (StyleBackground)StyleKeyword.None : new StyleBackground(photo.Value.image);
            band.tooltip = photo?.alt ?? "";
            PhotoAlt = photo?.alt;
        }

        /// <summary>The smallest height of the picture left visible above its credit strip (Statecraft.uss .event-photo
        /// shrinks first). On a window too short for a long event the band would show a sliver, or cut into the credit that
        /// CC BY 4.0 requires: below credit height + MinPhotoHeight the photograph is dropped (credit, tooltip and all), so
        /// the body and the options keep their room and the card reads as a card without a picture. The minister's
        /// portrait does not come back in its place: it is taller still (docs/event-images.md).</summary>
        public const float MinPhotoHeight = 48f;

        /// <summary>The band height the photograph needs: its whole credit strip plus MinPhotoHeight of picture. The credit is
        /// measured from its text at the band's width, not read from its layout: in a squeezed band the label's layout is
        /// clamped to the room left, which is exactly the clipping this guards against.</summary>
        public float PhotoNeeds => CreditHeight() + MinPhotoHeight;

        float CreditHeight()
        {
            var s = credit.resolvedStyle;
            var bandWidth = band.layout.width > 0 ? band.layout.width : card.contentRect.width;
            var width = bandWidth - band.resolvedStyle.borderLeftWidth - band.resolvedStyle.borderRightWidth - s.paddingLeft - s.paddingRight;
            if (float.IsNaN(width) || width <= 0) return float.NaN;
            var text = credit.MeasureTextSize(credit.text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
            return text.y + s.paddingTop + s.paddingBottom + s.borderTopWidth + s.borderBottomWidth;
        }

        /// <summary>Drops the shown photograph when its band is shorter than <see cref="PhotoNeeds"/>. Only ever hides, so the
        /// layout pass it causes cannot bring the photo back and loop; OnColumnGeometry is what restores it.</summary>
        void FitPhoto()
        {
            if (PhotoAlt == null) return;
            var h = band.layout.height; var needs = PhotoNeeds;
            if (float.IsNaN(h) || float.IsNaN(needs)) return;
            if (h < needs) SetPhotoShown(false);
        }

        void OnPhotoGeometry(GeometryChangedEvent evt) => FitPhoto();

        /// <summary>The right-hand column changed size (the window did; the card's content never moves it, as the column is
        /// pinned to the window by top and bottom). A shown photograph is checked again; a dropped one comes back only when
        /// the room now free in the column holds it (<see cref="RoomForDroppedPhoto"/>), so it is never shown just to be
        /// measured (a hidden element keeps its old layout, so a re-shown band may get no new geometry event).</summary>
        void OnColumnGeometry(GeometryChangedEvent evt)
        {
            if (current != null && PhotoAlt == null) { if (RoomForDroppedPhoto() >= PhotoNeeds) SetPhotoShown(true); }
            else FitPhoto();
        }

        /// <summary>The band height a dropped photograph would get now (at most; the band itself stops at 200 px): the slack
        /// of the column's flexible gap above its minimum, capped by the card's max-height, less the band's margins.</summary>
        float RoomForDroppedPhoto()
        {
            var gap = card.parent?.Q(className: "right-gap");
            if (gap == null || float.IsNaN(gap.layout.height) || float.IsNaN(card.layout.height)) return 0;
            var slack = gap.layout.height - gap.resolvedStyle.minHeight.value;
            var max = card.resolvedStyle.maxHeight;
            if (max.keyword == StyleKeyword.Undefined && !float.IsNaN(max.value)) slack = System.Math.Min(slack, max.value - card.layout.height);
            return slack - band.resolvedStyle.marginTop - band.resolvedStyle.marginBottom;
        }

        /// <summary>The event's photograph, whether or not the window has room to show it.</summary>
        EventImageCatalog.Entry? current;

        /// <summary>The shown photograph's alt text (what it depicts), or null when the card shows no photograph (none for
        /// this event, or dropped on a short window).</summary>
        public string? PhotoAlt { get; private set; }

        /// <summary>The round sign in an option's left padding (Statecraft.uss .event-option), painted into the button so it
        /// stays a leaf whose wrapped text Yoga measures (UiIcon.Paint).</summary>
        const float SignRadius = 15f, SignLeft = 9f;

        static void PaintSign(MeshGenerationContext m) =>
            UiIcon.PaintDisc(m, "chevron", new UnityEngine.Vector2(SignLeft + SignRadius, m.visualElement.layout.height / 2f), SignRadius);

        void Choose(GameEvent e, EventOption o)
        {
            // The bridge raises Refused (the toast shows it) or Changed (the toast clears); nothing more to do here.
            bridge.Queue(new ChooseEventOption(e.Id, o.Id));
        }
    }
}
