using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// Owns the world-space colour map read by the "Arcane Restoration/City Lit" shader.
    /// Each region (one per zone) has a colour amount from 0 (grey, polluted) to 1 (full colour).
    /// Regions are painted into a small texture with soft edges, so neighbouring zones blend smoothly
    /// and every zone regains its colour on its own, visible from anywhere in the city.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class CityColorMap : MonoBehaviour
    {
        [Serializable]
        public class Region
        {
            public string name = "Zone";
            [Tooltip("Area in world space: x/y = world X/Z minimum, width/height = size in metres.")]
            public Rect area = new Rect(-20f, -20f, 40f, 40f);
            [Range(0f, 1f)] public float colour;
        }

        static readonly int ColorMapId = Shader.PropertyToID("_CityColorMap");
        static readonly int ColorRectId = Shader.PropertyToID("_CityColorRect");
        static readonly int GreyTintId = Shader.PropertyToID("_CityGreyTint");

        [Header("Map")]
        [Tooltip("World area covered by the map (X/Z). Outside of it the edge value is used.")]
        [SerializeField] Rect m_WorldArea = new Rect(-160f, -160f, 320f, 320f);
        [SerializeField, Range(16, 256)] int m_Resolution = 128;
        [Tooltip("Soft edge between regions, in metres.")]
        [SerializeField, Min(0.1f)] float m_Feather = 4f;
        [Tooltip("Colour amount for places outside every region.")]
        [SerializeField, Range(0f, 1f)] float m_OutsideColour;

        [Header("Grey look")]
        [SerializeField] Color m_GreyTint = new Color(0.9f, 0.93f, 1f);
        [SerializeField, Range(0.3f, 1.2f)] float m_GreyBrightness = 0.85f;

        [Header("Regions")]
        [SerializeField] List<Region> m_Regions = new List<Region>();

        Texture2D m_Texture;
        byte[] m_Pixels;
        bool m_Dirty = true;

        public IReadOnlyList<Region> regions => m_Regions;

        /// <summary>Sets the colour amount (0..1) of a region by name.</summary>
        public void SetRegionColour(string regionName, float colour)
        {
            foreach (var region in m_Regions)
            {
                if (region.name != regionName)
                    continue;
                colour = Mathf.Clamp01(colour);
                if (!Mathf.Approximately(region.colour, colour))
                {
                    region.colour = colour;
                    m_Dirty = true;
                }
                return;
            }
            Debug.LogWarning($"CityColorMap: no region named '{regionName}'.", this);
        }

        public float GetRegionColour(string regionName)
        {
            foreach (var region in m_Regions)
                if (region.name == regionName)
                    return region.colour;
            return 0f;
        }

        void OnEnable()
        {
            m_Dirty = true;
            Rebuild();
        }

        void OnValidate() => m_Dirty = true;

        void Update()
        {
            if (m_Dirty)
                Rebuild();
        }

        void OnDisable()
        {
            // Fall back to "everything in colour" so the art is never stuck grey without the map.
            Shader.SetGlobalTexture(ColorMapId, Texture2D.whiteTexture);
        }

        void OnDestroy()
        {
            if (m_Texture == null)
                return;
            if (Application.isPlaying) Destroy(m_Texture);
            else DestroyImmediate(m_Texture);
        }

        void Rebuild()
        {
            m_Dirty = false;
            int res = m_Resolution;
            if (m_Texture == null || m_Texture.width != res)
            {
                if (m_Texture != null)
                    DestroyImmediate(m_Texture);
                m_Texture = new Texture2D(res, res, TextureFormat.R8, false, true)
                {
                    name = "CityColorMap",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.DontSave
                };
                m_Pixels = new byte[res * res];
            }

            float cell = m_WorldArea.width / res;
            for (int y = 0; y < res; y++)
            {
                float worldZ = m_WorldArea.yMin + (y + 0.5f) * cell;
                for (int x = 0; x < res; x++)
                {
                    float worldX = m_WorldArea.xMin + (x + 0.5f) * cell;
                    m_Pixels[y * res + x] = (byte)Mathf.RoundToInt(SampleColour(worldX, worldZ) * 255f);
                }
            }

            m_Texture.SetPixelData(m_Pixels, 0);
            m_Texture.Apply(false, false);

            Shader.SetGlobalTexture(ColorMapId, m_Texture);
            Shader.SetGlobalVector(ColorRectId, new Vector4(m_WorldArea.xMin, m_WorldArea.yMin, 1f / m_WorldArea.width, 1f / m_WorldArea.height));
            Shader.SetGlobalVector(GreyTintId, new Vector4(m_GreyTint.r, m_GreyTint.g, m_GreyTint.b, m_GreyBrightness));
        }

        float SampleColour(float x, float z)
        {
            float weightSum = 0f;
            float valueSum = 0f;
            foreach (var region in m_Regions)
            {
                float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-m_Feather, m_Feather, SignedDistance(region.area, x, z)));
                weightSum += w;
                valueSum += w * region.colour;
            }
            valueSum += Mathf.Max(0f, 1f - weightSum) * m_OutsideColour;
            return valueSum / Mathf.Max(1f, weightSum);
        }

        static float SignedDistance(Rect r, float x, float z)
        {
            float dx = Mathf.Max(r.xMin - x, x - r.xMax);
            float dz = Mathf.Max(r.yMin - z, z - r.yMax);
            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dz, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(dx, dz), 0f);
            return outside + inside;
        }
    }
}
