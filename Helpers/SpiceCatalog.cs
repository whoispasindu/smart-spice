namespace SmartSpice.Helpers;

/// <summary>
/// The fixed list of raw spice products the factory accepts, and the finished
/// powder each one becomes after processing. New batches may only be one of these
/// products, and on reaching the Stored stage a batch is converted into its powder.
/// </summary>
public static class SpiceCatalog
{
    /// <summary>Raw product → finished powder name.</summary>
    public static readonly IReadOnlyDictionary<string, string> RawToPowder =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dried Red Chillies"] = "Chilli Powder",
            ["Black Pepper Corns"] = "Pepper Powder",
            ["Fresh Turmeric Rhizomes"] = "Turmeric Powder",
            ["Coriander Seeds"] = "Coriander Powder",
            ["Cumin Seeds"] = "Cumin Powder",
            ["Fennel Seeds"] = "Fennel Powder",
            ["Mustard Seeds"] = "Mustard Powder",
            ["Curry Leaves"] = "Curry Leaf Powder",
            ["Cardamom"] = "Cardamom Powder",
            ["Cloves"] = "Clove Powder",
            ["Cinnamon"] = "Cinnamon Powder",
            ["Fenugreek Seeds"] = "Fenugreek Powder",
            ["Pandan Leaves"] = "Pandan Powder",
        };

    /// <summary>The allowed raw products (for the new-batch picker).</summary>
    public static IReadOnlyList<string> RawProducts { get; } = RawToPowder.Keys.ToList();

    /// <summary>The finished powder name for a raw product (falls back to "{raw} Powder").</summary>
    public static string PowderFor(string rawProduct) =>
        RawToPowder.TryGetValue(rawProduct, out var p) ? p : $"{rawProduct} Powder";
}
