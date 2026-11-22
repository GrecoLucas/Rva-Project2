using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// City-wide view of the cleaning progress and of every sorting attempt.
    /// Static so that items, bins and UI can talk to it without scene references.
    /// </summary>
    public static class CityCleanlinessManager
    {
        static readonly List<CleanableZone> s_Zones = new List<CleanableZone>();

        /// <summary>Raised after every sorting attempt: (item, bin, correct).</summary>
        public static event Action<WasteItem, RecyclingBin, bool> SortAttempted;
        /// <summary>Raised when the city progress changes (0..1).</summary>
        public static event Action<float> ProgressChanged;

        public static int correctSorts { get; private set; }
        public static int wrongSorts { get; private set; }
        public static IReadOnlyList<CleanableZone> zones => s_Zones;

        /// <summary>Share of correct attempts (1 when nothing was attempted yet).</summary>
        public static float accuracy => correctSorts + wrongSorts == 0 ? 1f : (float)correctSorts / (correctSorts + wrongSorts);

        public static float progress
        {
            get
            {
                int total = 0, sorted = 0;
                foreach (var z in s_Zones) { total += z.total; sorted += z.sorted; }
                return total == 0 ? 0f : (float)sorted / total;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Zones.Clear();
            correctSorts = 0;
            wrongSorts = 0;
            SortAttempted = null;
            ProgressChanged = null;
        }

        internal static void Register(CleanableZone zone)
        {
            if (!s_Zones.Contains(zone))
                s_Zones.Add(zone);
        }

        internal static void Unregister(CleanableZone zone) => s_Zones.Remove(zone);

        internal static void ReportSort(WasteItem item, RecyclingBin bin, bool correct)
        {
            if (correct) correctSorts++; else wrongSorts++;
            SortAttempted?.Invoke(item, bin, correct);
        }

        internal static void NotifyProgress() => ProgressChanged?.Invoke(progress);
    }
}
