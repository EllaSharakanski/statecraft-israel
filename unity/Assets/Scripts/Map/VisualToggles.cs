#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.Map
{
    /// <summary>One on/off switch per visual feature (shadows, tilt-shift, grade, haze, grid...), registered by the feature
    /// itself, so the visual profile can price each one and tests can isolate it. Names run in ordinal order; registering
    /// a name again replaces its switch (a rebuilt map re-registers).</summary>
    public static class VisualToggles
    {
        static readonly SortedDictionary<string, Action<bool>> Switches = new SortedDictionary<string, Action<bool>>(StringComparer.Ordinal);

        public static void Register(string name, Action<bool> set)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("a toggle needs a name", nameof(name));
            Switches[name] = set ?? throw new ArgumentNullException(nameof(set));
        }

        public static void Unregister(string name) => Switches.Remove(name);
        public static IReadOnlyList<string> Names => Switches.Keys.ToList();

        public static void Set(string name, bool on)
        {
            if (!Switches.TryGetValue(name, out var set)) throw new KeyNotFoundException($"no visual toggle named '{name}' (known: {string.Join(", ", Switches.Keys)})");
            set(on);
        }

        public static void SetAll(bool on) { foreach (var set in Switches.Values.ToList()) set(on); }
        public static void Clear() => Switches.Clear();
    }
}
