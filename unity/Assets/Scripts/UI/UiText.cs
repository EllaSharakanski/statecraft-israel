#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using YamlDotNet.Serialization;

namespace Statecraft.Game.UI
{
    /// <summary>UI strings kept outside code (unity/Assets/UI/strings.en.yaml): key -> string.Format pattern.</summary>
    public sealed class UiText
    {
        readonly Dictionary<string, string> map;
        readonly string file;
        UiText(Dictionary<string, string> map, string file) { this.map = map; this.file = file; }

        /// <summary>Parses the strings file. Throws InvalidOperationException naming the file when it is empty or not a map.</summary>
        public static UiText Parse(string yaml, string file = "strings.en.yaml")
        {
            Dictionary<string, string> m;
            try { m = new DeserializerBuilder().WithDuplicateKeyChecking().Build().Deserialize<Dictionary<string, string>>(yaml ?? ""); }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException($"{file}: {e.Message}", e); }
            if (m == null || m.Count == 0) throw new InvalidOperationException($"{file}: no strings");
            return new UiText(m, file);
        }

        public string this[string key] =>
            map.TryGetValue(key, out var v) ? v : throw new KeyNotFoundException($"{file} has no key '{key}'");

        public string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, this[key], args);
    }
}
