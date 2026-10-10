#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The top-bar buttons open the Cabinet, Budget, Defence and Why screens, one at a time; pressing the open
    /// screen's button again closes it.</summary>
    public sealed class ScreenNav
    {
        public static readonly string[] Screens = { "cabinet", "budget", "defence", "why" };
        readonly VisualElement root;

        public ScreenNav(VisualElement root)
        {
            this.root = root;
            foreach (var s in Screens)
            {
                var screen = s;
                root.Q<Button>("nav-" + screen).clicked += () => Toggle(screen);
            }
            Open(null);
        }

        /// <summary>The open screen's name, or null.</summary>
        public string? Current { get; private set; }

        public void Toggle(string screen) => Open(Current == screen ? null : screen);

        public void Open(string? screen)
        {
            Current = screen;
            foreach (var s in Screens)
                root.Q<VisualElement>("screen-" + s).style.display = s == screen ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
