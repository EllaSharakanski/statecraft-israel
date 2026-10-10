#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.Map
{
    /// <summary>Average and 95th-percentile frame time (ms), shared by the terrain and visual profiles.</summary>
    public static class FrameStats
    {
        public static (float avg, float p95) Summary(IReadOnlyList<float> frameMs)
        {
            if (frameMs == null) throw new ArgumentNullException(nameof(frameMs));
            if (frameMs.Count == 0) throw new ArgumentException("no frames were measured", nameof(frameMs));
            var sorted = frameMs.OrderBy(x => x).ToList();
            var p95 = sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * 0.95) - 1)];
            return (sorted.Average(), p95);
        }
    }
}
