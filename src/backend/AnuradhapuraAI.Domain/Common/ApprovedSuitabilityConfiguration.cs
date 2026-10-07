namespace AnuradhapuraAI.Domain.Common;

public static class ApprovedSuitabilityConfiguration
{
    public static class Types
    {
        public const string FactorWeight = "FactorWeight";
        public const string CategoryThreshold = "CategoryThreshold";

        public static readonly string[] All =
        [
            FactorWeight,
            CategoryThreshold
        ];
    }

    public static class Keys
    {
        public const string Rainfall = "Rainfall";
        public const string Temperature = "Temperature";
        public const string Humidity = "Humidity";
        public const string SoilCompatibility = "SoilCompatibility";
        public const string HighlySuitable = "Highly Suitable";
        public const string Suitable = "Suitable";
        public const string ModeratelySuitable = "Moderately Suitable";
        public const string Unsuitable = "Unsuitable";

        public static readonly string[] All =
        [
            Rainfall,
            Temperature,
            Humidity,
            SoilCompatibility,
            HighlySuitable,
            Suitable,
            ModeratelySuitable,
            Unsuitable
        ];
    }
}
