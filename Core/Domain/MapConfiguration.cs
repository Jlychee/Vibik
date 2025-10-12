namespace Vibik.Core.Domain;

public record MapConfiguration(
    double CenterLatitude,
    double CenterLongitude,
    int ZoomLevel,
    string MapType
);