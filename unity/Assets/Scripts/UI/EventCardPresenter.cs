#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The decision card, in the middle of the screen under the bar (decision cards, 2026-10-13): the first pending
    /// event the player has not yet answered, with its 16:9 media header (an archival photograph of the event with its credit
    /// line, as the promo's card; EventImageCatalog) or, when the event has none, the responsible minister's portrait, the
    /// text with today's office holders filled in, and one button per option.
    /// While it shows, the root has OpenClass: a backdrop dims the map and takes the pointer, and ScreenNav.DisableCovered
    /// takes the panels under it out of reach. Hide collapses it to the "Decision waiting" button, which brings it back; a
    /// new event opens it again by itself. A new event puts the keyboard focus on the card itself, not on an
    /// option, so no choice is lit as if recommended; Tab then reaches the options in order.
    /// A choice is an order: it takes effect at End Turn, and an unanswered event is delegated to the minister.</summary>
    public sealed class EventCardPresenter : System.IDisposable
    {
        /// <summary>The portrait thumbnail's size in the card (Statecraft.uss .event-image).</summary>
        public const float PortraitWidth = 96f, PortraitHeight = 128f;
        /// <summary>The class on each option button (Statecraft.uss).</summary>
        public const string OptionClass = "event-option";
        /// <summary>On the "root" element while the card shows (not collapsed): Statecraft.uss shows the backdrop.</summary>
        public const string OpenClass = "event-open";
        /// <summary>On the "root" element while the card shows and no start screen or screen covers it (set by
        /// ScreenNav.DisableCovered): the panels under the backdrop are disabled and the toast moves clear of the card.</summary>
        public const string ModalClass = "event-modal";
        /// <summary>The media header's width over its height (the photographs and the clips are 960x540).</summary>
        public const float MediaAspect = 16f / 9f;

        readonly VisualElement card; readonly SimBridge bridge; readonly UiText text; readonly PortraitCatalog? portraits;
        readonly EventImageCatalog? photos;
        readonly VisualElement band, head, scroll, options, layer, hudRoot; readonly Label credit;
        readonly Button hide, reopen;
        readonly System.Action onHide, onReopen;
        /// <summary>The event the player collapsed the card on (Hide), or null; a different event opens the card again.</summary>
        string? collapsedFor;
        /// <summary>The event whose first option last took the focus, so an event already open never takes it again.</summary>
        string? focusedFor;
        float appliedWidth = float.NaN;

        public EventCardPresenter(VisualElement root, SimBridge bridge, UiText text, PortraitCatalog? portraits, EventImageCatalog? photos = null)
        {
            card = root.Q<VisualElement>("event-card"); this.bridge = bridge; this.text = text; this.portraits = portraits; this.photos = photos;
            band = card.Q<VisualElement>("event-photo"); credit = card.Q<Label>("event-photo-credit");
            // The band and its credit settle the drop (FitPhoto); the column, which only the window sizes, brings a dropped
            // photograph back when the window grows. A hidden element keeps its old layout, so a band shown again at the
            // size it was dropped at gets no geometry event of its own; the head below it always moves, so it checks too.
            head = card.Q<VisualElement>(className: "event-head");
            scroll = card.Q<VisualElement>("event-scroll"); options = card.Q<VisualElement>("event-options");
            layer = card.parent; hudRoot = root.Q("root") ?? root;
            hide = card.Q<Button>("event-hide"); reopen = layer.Q<Button>("event-reopen");
            hide.text = text["event.hide"]; reopen.text = text["event.reopen"];
            onHide = Collapse; onReopen = Reopen;
            hide.clicked += onHide; reopen.clicked += onReopen;
            card.focusable = true; card.tabIndex = 0;   // the card takes the focus itself (FocusCard); it has no :focus style
            foreach (var e in new[] { band, credit, head, scroll, options }) e.RegisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            layer.RegisterCallback<GeometryChangedEvent>(OnColumnGeometry);
            card.RegisterCallback<GeometryChangedEvent>(OnCardGeometry);
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge, removes the option buttons and hides the card (the owner calls this when disabled).</summary>
        public void Dispose()
        {
            bridge.Changed -= Render;
            hide.clicked -= onHide; reopen.clicked -= onReopen;
            foreach (var e in new[] { band, credit, head, scroll, options }) e.UnregisterCallback<GeometryChangedEvent>(OnPhotoGeometry);
            layer.UnregisterCallback<GeometryChangedEvent>(OnColumnGeometry);
            card.UnregisterCallback<GeometryChangedEvent>(OnCardGeometry);
            card.Q<VisualElement>("event-options")?.Clear(); card.style.display = DisplayStyle.None;
            reopen.style.display = DisplayStyle.None;
            hudRoot.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
        }

        /// <summary>True while the card is collapsed to the "Decision waiting" button (Hide).</summary>
        public bool Collapsed => Shown != null && collapsedFor == Shown.Id;

        /// <summary>Hide: the card gives the map back and waits behind the "Decision waiting" button, which takes the focus.</summary>
        public void Collapse()
        {
            if (Shown == null) return;
            collapsedFor = Shown.Id;
            Render();
            reopen.Focus();
        }

        /// <summary>"Decision waiting": the card opens again and takes the focus.</summary>
        public void Reopen()
        {
            collapsedFor = null; focusedFor = null;
            Render();
        }

        /// <summary>The event the card shows, or null when every pending event has a queued answer.</summary>
        public GameEvent? Shown { get; private set; }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var pending = s.PendingEvents.Select(id => c.Events.First(e => e.Id == id)).ToList();
            var open = pending.Where(e => bridge.QueuedLike(new ChooseEventOption(e.Id, "")) == null).ToList();
            Shown = open.FirstOrDefault();
            if (Shown == null) collapsedFor = null;
            var shown = Shown != null && !Collapsed;
            card.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            card.SetEnabled(shown);   // a child of an undisplayed element can still take the focus (#400): a collapsed card's options must not
            reopen.style.display = Collapsed ? DisplayStyle.Flex : DisplayStyle.None;
            hudRoot.EnableInClassList(OpenClass, shown);
            ScreenNav.DisableCovered(hudRoot);
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
            if (shown && focusedFor != e.Id) FocusCard(e.Id);
        }

        /// <summary>A newly shown event puts the keyboard focus on the card (keyboard play, #41): the next Tab reaches its
        /// first option, and no option is lit before the player picks. Not when the layer is out of reach (the start screen or
        /// a screen covers it). It is tried again once styles resolve, as a still-hidden card cannot take it.</summary>
        void FocusCard(string id)
        {
            if (!layer.enabledInHierarchy) return;
            focusedFor = id;
            card.Focus();
            card.schedule.Execute(() =>
            {
                if (Shown?.Id == id && !Collapsed && layer.enabledInHierarchy && card.panel != null
                    && card.focusController?.focusedElement == null) card.Focus();
            });
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
            ApplyMediaHeight();
        }

        /// <summary>The logical window height below which the event body may scroll (UiLayoutTests, playtest 2 P2-3: from
        /// 990 up, the body and the options fit without scrolling).</summary>
        public const float ScrollFallbackHeight = 990f;

        /// <summary>The media header's height: 16:9 of the card's content width (the stylesheet's 540 px until the card is laid
        /// out). On every window the header gives way first when the card is taller than the layer (Statecraft.uss
        /// flex-shrink 1000; the picture is cropped, background-size cover), so the text and the options show whole; below
        /// PhotoNeeds it is dropped (FitPhoto), and only then may the body scroll. The 200 px band of the old right-hand
        /// column needed the body to scroll first on short windows, or 21:9 never showed a photograph; a 16:9 header has
        /// the room to give.</summary>
        void ApplyMediaHeight()
        {
            var w = card.contentRect.width;
            appliedWidth = w;
            band.style.height = float.IsNaN(w) || w <= 0 ? (StyleLength)StyleKeyword.Null : new StyleLength(UnityEngine.Mathf.Round(w / MediaAspect));
        }

        /// <summary>The card's width changed (it was first laid out, or the window changed): the media header follows it. The
        /// card's height changes with the header, so only a new width counts, or the two would chase each other.</summary>
        void OnCardGeometry(GeometryChangedEvent evt)
        {
            var w = card.contentRect.width;
            if (float.IsNaN(w) || w <= 0 || UnityEngine.Mathf.Abs(w - appliedWidth) < 0.5f) return;
            ApplyMediaHeight(); FitPhoto();
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
            if (h < needs - 0.5f) SetPhotoShown(false);
        }

        void OnPhotoGeometry(GeometryChangedEvent evt) => FitPhoto();

        /// <summary>The decision layer changed size (the window did; the card's content never moves it, as the layer is pinned
        /// to the window on every side). A shown photograph is checked again; a dropped one comes back only when the room now
        /// free in the layer holds it (<see cref="RoomForDroppedPhoto"/>), so it is never shown just to be measured (a hidden
        /// element keeps its old layout, so a re-shown band may get no new geometry event).</summary>
        void OnColumnGeometry(GeometryChangedEvent evt)
        {
            if (current != null && PhotoAlt == null) { if (RoomForDroppedPhoto() >= PhotoNeeds) SetPhotoShown(true); }
            else { ApplyMediaHeight(); FitPhoto(); }
        }

        /// <summary>The band height a dropped photograph would get now (at most; the band itself stops at 16:9): the layer's
        /// height the card leaves free (the card's max-height is the layer's), less the band's margins.</summary>
        float RoomForDroppedPhoto()
        {
            var room = layer.contentRect.height;
            if (float.IsNaN(room) || float.IsNaN(card.layout.height)) return 0;
            var slack = room - card.layout.height;
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
