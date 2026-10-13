using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Statecraft.Sim
{
    /// <summary>The GDD v0.1 slice before cutover (spec 2026-10-12 section 2.1): the real content with the loop switch on
    /// (eras.yaml systems.loop: gdd in every era) and monthly turns (rules.yaml turn_lengths all month). Tests, the MVP
    /// goldens and the dev flag load it; the real eras.yaml and rules.yaml keep the defaults until cutover. It edits YAML
    /// nodes, not text, so a comment or the key order in the real files cannot break it. Balance values the owner has not set
    /// may live here, and never under content/ (controller ruling 2, 12 October 2026). R-LOOP-003.</summary>
    public static class MvpSlice
    {
        static readonly IDeserializer Reader = new DeserializerBuilder().WithDuplicateKeyChecking().Build();
        static readonly ISerializer Writer = new SerializerBuilder().Build();

        /// <summary>Wraps <paramref name="readFile"/> (the loader's file reader): eras.yaml and rules.yaml come back with the
        /// slice's switches, every other file unchanged.</summary>
        public static Func<string, string?> Overlay(Func<string, string?> readFile)
        {
            if (readFile == null) throw new ArgumentNullException(nameof(readFile));
            return rel =>
            {
                var text = readFile(rel);
                if (text == null) return null;
                if (rel == "eras.yaml")
                {
                    var root = Reader.Deserialize<Dictionary<object, object>>(text);
                    if (root != null && root.TryGetValue("eras", out var list) && list is List<object> eras)
                        foreach (var item in eras.OfType<Dictionary<object, object>>())
                        {
                            var systems = item.TryGetValue("systems", out var x) && x is Dictionary<object, object> d ? d : new Dictionary<object, object>();
                            systems["loop"] = EraSystems.Gdd;
                            item["systems"] = systems;
                        }
                    return Writer.Serialize(root);
                }
                if (rel == "rules.yaml")
                {
                    var root = Reader.Deserialize<Dictionary<object, object>>(text) ?? new Dictionary<object, object>();
                    root["turn_lengths"] = new List<object> { "month", "month", "month", "month", "month" };
                    return Writer.Serialize(root);
                }
                return text;
            };
        }

        /// <summary>The content at <paramref name="contentRoot"/> (a directory laid out like content/) through <see cref="Overlay"/>,
        /// with the same directory checks as ContentLoader.Load(string).</summary>
        public static Content Load(string contentRoot) => ContentLoader.Load(contentRoot, Overlay);
    }
}
