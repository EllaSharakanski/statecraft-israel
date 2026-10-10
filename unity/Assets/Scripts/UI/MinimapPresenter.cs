#nullable enable
using System;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Statecraft.Game.UI
{
    /// <summary>The minimap (bottom-right): every hex in its biome colour, north up, the camera's view as a frame, and a
    /// click to move the camera there. The texture is made at runtime and destroyed by Dispose.</summary>
    public sealed class MinimapPresenter : IDisposable
    {
        public const int Width = 128;
        public static readonly Color Sea = MapBuilder.BiomeColours["sea"];
        readonly VisualElement box, image, view; readonly HexFile hexes;
        readonly double widthKm, lengthKm;

        public Texture2D Texture { get; }
        /// <summary>Raised with the lon/lat the player clicked.</summary>
        public event Action<double, double>? Clicked;

        public MinimapPresenter(VisualElement root, HexFile hexes)
        {
            this.hexes = hexes;
            var b = hexes.bounds;
            widthKm = (b.lon1 - b.lon0) * HexPicker.KmPerDegLon(b.lat0, b.lat1);
            lengthKm = (b.lat1 - b.lat0) * HexPicker.KmPerDegLat;
            Texture = Paint(hexes, Width, (int)Math.Round(Width * lengthKm / widthKm));
            box = root.Q<VisualElement>("minimap");
            image = box.Q<VisualElement>("minimap-image");
            view = box.Q<VisualElement>("minimap-view");
            image.style.backgroundImage = new StyleBackground(Texture);
            image.RegisterCallback<PointerDownEvent>(OnPointerDown);
            box.RegisterCallback<GeometryChangedEvent>(OnBoxGeometry);
            FitImage();
        }

        /// <summary>The largest size with the picture's aspect that fits the container (scale-to-fit would draw the picture
        /// smaller than its element and leave a strip that clicks and the view frame would still count as map).</summary>
        public static Vector2 Fit(float containerWidth, float containerHeight, float aspect)
        {
            if (!(containerWidth > 0) || !(containerHeight > 0) || !(aspect > 0)) return Vector2.zero;
            return containerWidth / containerHeight > aspect ? new Vector2(containerHeight * aspect, containerHeight) : new Vector2(containerWidth, containerWidth / aspect);
        }

        void OnBoxGeometry(GeometryChangedEvent e) => FitImage();

        // The picture's element is exactly as big as the picture, so (0..1, 0..1) in the element is the whole map.
        void FitImage()
        {
            var size = Fit(box.contentRect.width, box.contentRect.height, (float)Texture.width / Texture.height);
            if (size == Vector2.zero) return;
            image.style.width = size.x; image.style.height = size.y;
        }

        void OnPointerDown(PointerDownEvent e)
        {
            if (e.button != 0) return;                                   // only the left button
            if (!(image.layout.width > 0) || !(image.layout.height > 0)) return;   // before layout (or NaN) there is no map to click
            var (lon, lat) = ToLonLat(e.localPosition.x / image.layout.width, e.localPosition.y / image.layout.height);
            Clicked?.Invoke(lon, lat);
        }

        /// <summary>One pixel per ~1.3 km: each pixel takes the biome of the hex its centre falls in (sea off the grid).
        /// Row 0 of a Texture2D is the bottom, so it is the south edge.</summary>
        public static Texture2D Paint(HexFile hexes, int width, int height)
        {
            var biome = new System.Collections.Generic.Dictionary<(int, int), string>();
            foreach (var h in hexes.hexes) biome[(h.q, h.r)] = h.biome;
            var b = hexes.bounds;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false) { name = "Minimap", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    double lon = b.lon0 + (x + 0.5) / width * (b.lon1 - b.lon0), lat = b.lat0 + (y + 0.5) / height * (b.lat1 - b.lat0);
                    var hex = HexPicker.FromLonLat(lon, lat, b, hexes.hex_size_km);
                    pixels[y * width + x] = biome.TryGetValue(hex, out var name) ? MapBuilder.BiomeColours[name] : Sea;
                }
            tex.SetPixels(pixels); tex.Apply();
            return tex;
        }

        /// <summary>(u, v) in the image, top-left origin, 0..1 -> lon/lat.</summary>
        public (double lon, double lat) ToLonLat(float u, float v)
        {
            var b = hexes.bounds;
            return (b.lon0 + Mathf.Clamp01(u) * (b.lon1 - b.lon0), b.lat1 - Mathf.Clamp01(v) * (b.lat1 - b.lat0));
        }

        /// <summary>Frames the area the camera looks at: about one and a half view distances wide, centred on lookAt.</summary>
        public void SetView(GeoFrame frame, Vector3 lookAt, float distanceM)
        {
            var (lon, lat) = frame.ToLonLat(lookAt);
            var b = hexes.bounds;
            float u = (float)((lon - b.lon0) / (b.lon1 - b.lon0)), v = (float)((b.lat1 - lat) / (b.lat1 - b.lat0));
            float w = Mathf.Min(1f, (float)(1.5 * distanceM / 1000 / widthKm)), h = Mathf.Min(1f, (float)(1.0 * distanceM / 1000 / lengthKm));
            // the frame is clamped inside the picture, which is the whole element
            float left = Mathf.Clamp(u - w / 2, 0f, 1f - w), top = Mathf.Clamp(v - h / 2, 0f, 1f - h);
            view.style.left = Length.Percent(left * 100); view.style.top = Length.Percent(top * 100);
            view.style.width = Length.Percent(w * 100); view.style.height = Length.Percent(h * 100);
        }

        public void Dispose()
        {
            image.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            box.UnregisterCallback<GeometryChangedEvent>(OnBoxGeometry);
            image.style.backgroundImage = StyleKeyword.Null;   // nothing may keep drawing a destroyed texture
            if (Texture != null) Object.DestroyImmediate(Texture);
        }
    }
}
