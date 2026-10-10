#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Networking;

namespace Statecraft.Game.Boot
{
    /// <summary>The map's raw inputs, already read (no System.IO needed afterwards).</summary>
    public sealed class MapInputs
    {
        public string HeightmapJson = "", HexesJson = "", SettlementsYaml = "";
        public byte[] HeightmapR16 = new byte[0];
    }

    /// <summary>Everything the game loads at start: the sim content and the map inputs. Desktop and the editor read
    /// files (FromDisk); WebGL cannot read StreamingAssets with System.IO, so it fetches them with UnityWebRequest from
    /// the manifest (FetchAsync, tech notes section 2).</summary>
    public sealed class GameInputs
    {
        public Content Content = null!;
        public MapInputs Map = null!;

        public static MapInputs MapFromDisk(string contentDir, string mapDir) => new MapInputs
        {
            HeightmapJson = File.ReadAllText(Path.Combine(mapDir, "heightmap.json")),
            HeightmapR16 = File.ReadAllBytes(Path.Combine(mapDir, "heightmap.r16")),
            HexesJson = File.ReadAllText(Path.Combine(contentDir, "map", "hexes.json")),
            SettlementsYaml = File.ReadAllText(Path.Combine(contentDir, "map", SettlementPlacer.DefaultFileName)),
        };

        public static GameInputs FromDisk(string contentDir, string mapDir) =>
            new GameInputs { Content = ContentLoader.Load(contentDir), Map = MapFromDisk(contentDir, mapDir) };

        /// <summary>Builds the inputs from fetched texts (keys are manifest paths) and heightmap bytes.</summary>
        public static GameInputs FromTexts(IReadOnlyDictionary<string, string> texts, IEnumerable<string> manifest, string heightmapJson, byte[] r16)
        {
            var events = manifest.Where(f => f.StartsWith("events/", StringComparison.Ordinal)).ToList();
            var content = ContentLoader.Load(rel => texts.TryGetValue(rel, out var t) ? t : null, events);
            return new GameInputs
            {
                Content = content,
                Map = new MapInputs
                {
                    HeightmapJson = heightmapJson, HeightmapR16 = r16,
                    HexesJson = texts["map/hexes.json"], SettlementsYaml = texts["map/" + SettlementPlacer.DefaultFileName],
                },
            };
        }

        /// <summary>StreamingAssets as a URL: the page-relative URL on WebGL, a file:// URI elsewhere.</summary>
        public static string StreamingUrl() =>
            Application.streamingAssetsPath.Contains("://") ? Application.streamingAssetsPath : new Uri(Application.streamingAssetsPath).AbsoluteUri;

        static UnityWebRequest Start(string url)
        {
            var r = UnityWebRequest.Get(url);
            r.SendWebRequest();
            return r;
        }

        /// <summary>Waits a frame at a time (no threads on WebGL; an Awaitable is never awaited twice).</summary>
        static async Awaitable<UnityWebRequest> Done(UnityWebRequest r, string name)
        {
            while (!r.isDone) await Awaitable.NextFrameAsync();
            if (r.result != UnityWebRequest.Result.Success)
            {
                var error = $"{name}: {r.error} ({r.url})";
                r.Dispose();
                throw new IOException(error);
            }
            return r;
        }

        static async Awaitable<string> Text(UnityWebRequest r, string name)
        {
            using var done = await Done(r, name);
            return done.downloadHandler.text;
        }

        /// <summary>Fetches the manifest, then starts every download before awaiting any (parallel), then loads.</summary>
        public static async Awaitable<GameInputs> FetchAsync(string baseUrl)
        {
            var manifest = ContentManifest.Parse(await Text(Start(baseUrl + "/content/" + ContentManifest.FileName), ContentManifest.FileName));
            var requests = manifest.Select(f => (file: f, request: Start(baseUrl + "/content/" + f))).ToList();
            var meta = Start(baseUrl + "/map/heightmap.json");
            var r16 = Start(baseUrl + "/map/heightmap.r16");
            var texts = new Dictionary<string, string>();
            foreach (var (file, request) in requests) texts[file] = await Text(request, file);
            var metaText = await Text(meta, "heightmap.json");
            byte[] bytes;
            using (var done = await Done(r16, "heightmap.r16")) bytes = done.downloadHandler.data;
            return FromTexts(texts, manifest, metaText, bytes);
        }
    }
}
