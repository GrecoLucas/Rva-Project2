using UnityEngine;

namespace ArcaneRestoration.World
{
    /// <summary>
    /// Waste categories, following the Portuguese ecopontos.
    /// </summary>
    public enum WasteCategory
    {
        /// <summary>Yellow ecoponto: plastic and metal packaging.</summary>
        Packaging,
        /// <summary>Blue ecoponto: paper and cardboard.</summary>
        Paper,
        /// <summary>Green ecoponto: glass.</summary>
        Glass,
        /// <summary>Brown ecoponto: organic waste.</summary>
        Organic,
        /// <summary>Grey bin: everything else.</summary>
        Undifferentiated,
    }

    public static class WasteCategoryInfo
    {
        /// <summary>Bin colour of a category (matches the ecoponto models).</summary>
        public static Color BinColour(this WasteCategory category) => category switch
        {
            WasteCategory.Packaging => new Color(0.99f, 0.80f, 0.10f),
            WasteCategory.Paper => new Color(0.13f, 0.42f, 0.86f),
            WasteCategory.Glass => new Color(0.16f, 0.62f, 0.27f),
            WasteCategory.Organic => new Color(0.50f, 0.32f, 0.17f),
            _ => new Color(0.42f, 0.44f, 0.47f),
        };

        public static string BinName(this WasteCategory category) => category switch
        {
            WasteCategory.Packaging => "Yellow bin",
            WasteCategory.Paper => "Blue bin",
            WasteCategory.Glass => "Green bin",
            WasteCategory.Organic => "Brown bin",
            _ => "Grey bin",
        };

        public static string DisplayName(this WasteCategory category) => category switch
        {
            WasteCategory.Packaging => "Packaging",
            WasteCategory.Paper => "Paper & cardboard",
            WasteCategory.Glass => "Glass",
            WasteCategory.Organic => "Organic",
            _ => "Undifferentiated",
        };
    }
}
