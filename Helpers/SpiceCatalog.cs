namespace SmartSpice.Helpers;


public static class SpiceCatalog
{
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

    public static IReadOnlyList<string> RawProducts { get; } = RawToPowder.Keys.ToList();

    public static string PowderFor(string rawProduct) =>
        RawToPowder.TryGetValue(rawProduct, out var p) ? p : $"{rawProduct} Powder";
}
