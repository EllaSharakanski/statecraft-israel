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

        static readonly HashSet<UnityWebRequest> open = new HashSet<UnityWebRequest>();
        /// <summary>Requests started and not yet released (diagnostic; 0 once a fetch has ended, whether it succeeded or failed).</summary>
        public static int OpenRequests => open.Count;

        static UnityWebRequest Start(string url)
        {
            var r = UnityWebRequest.Get(url);
            open.Add(r);
            r.SendWebRequest();
            return r;
        }

        /// <summary>Aborts (when still running) and disposes a request; safe to call twice.</summary>
        static void Release(UnityWebRequest r)
        {
            if (!open.Remove(r)) return;
            if (!r.isDone) r.Abort();
            r.Dispose();
        }

        /// <summary>Waits a frame at a time (no threads on WebGL; an Awaitable is never awaited twice). The caller releases r.</summary>
        static async Awaitable Done(UnityWebRequest r, string name)
        {
            while (!r.isDone) await Awaitable.NextFrameAsync();
            if (r.result != UnityWebRequest.Result.Success) throw new IOException($"{name}: {r.error} ({r.url})");
        }

        static async Awaitable<string> Text(UnityWebRequest r, string name)
        {
            try { await Done(r, name); return r.downloadHandler.text; }
            finally { Release(r); }
        }

        /// <summary>Fetches the manifest, then starts every download before awaiting any (parallel), then loads. When one
        /// download fails, the others still running are aborted and disposed.</summary>
        public static async Awaitable<GameInputs> FetchAsync(string baseUrl)
        {
            var manifest = ContentManifest.Parse(await Text(Start(baseUrl + "/content/" + ContentManifest.FileName), ContentManifest.FileName));
            var requests = manifest.Select(f => (file: f, request: Start(baseUrl + "/content/" + f))).ToList();
            var meta = Start(baseUrl + "/map/heightmap.json");
            var r16 = Start(baseUrl + "/map/heightmap.r16");
            try
            {
                var texts = new Dictionary<string, string>();
                foreach (var (file, request) in requests) texts[file] = await Text(request, file);
                var metaText = await Text(meta, "heightmap.json");
                await Done(r16, "heightmap.r16");
                return FromTexts(texts, manifest, metaText, r16.downloadHandler.data);
            }
            finally
            {
                foreach (var (_, request) in requests) Release(request);
                Release(meta); Release(r16);
            }
        }
    }
}
