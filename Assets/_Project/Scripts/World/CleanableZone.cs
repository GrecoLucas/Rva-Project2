using System;
using UnityEngine;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// One area of the plaza. Counts the waste items placed under it and, as they are sorted,
    /// brings its region of the <see cref="CityColorMap"/> from grey to full colour.
    /// </summary>
    [DisallowMultipleComponent]
    public class CleanableZone : MonoBehaviour
    {
        [Tooltip("Name of the matching region in the CityColorMap.")]
        [SerializeField] string m_RegionName = "MainStreet";
        [Tooltip("Speed of the colour transition (colour amount per second).")]
        [SerializeField] float m_ColourSpeed = 0.35f;

        CityColorMap m_Map;
        int m_Total;
        int m_Sorted;
        float m_DisplayedColour;

        public event Action<CleanableZone> ProgressChanged;

        public string regionName => m_RegionName;
        public int total => m_Total;
        public int sorted => m_Sorted;
        public float progress => m_Total == 0 ? 1f : (float)m_Sorted / m_Total;

        void Awake()
        {
            m_Total = GetComponentsInChildren<WasteItem>(true).Length;
            m_Map = FindAnyObjectByType<CityColorMap>();
        }

        void Start()
        {
            if (m_Map != null)
            {
                m_DisplayedColour = progress >= 1f ? 1f : 0f;
                m_Map.SetRegionColour(m_RegionName, m_DisplayedColour);
            }
            CityCleanlinessManager.Register(this);
        }

        void OnDestroy() => CityCleanlinessManager.Unregister(this);

        void Update()
        {
            if (m_Map == null)
                return;
            float target = progress;
            if (Mathf.Approximately(m_DisplayedColour, target))
                return;
            m_DisplayedColour = Mathf.MoveTowards(m_DisplayedColour, target, m_ColourSpeed * Time.deltaTime);
            m_Map.SetRegionColour(m_RegionName, m_DisplayedColour);
        }

        /// <summary>Called by a <see cref="WasteItem"/> once it has been sorted into the right bin.</summary>
        public void ReportSorted(WasteItem item)
        {
            m_Sorted = Mathf.Min(m_Sorted + 1, m_Total);
            ProgressChanged?.Invoke(this);
            CityCleanlinessManager.NotifyProgress();
        }
    }
}
