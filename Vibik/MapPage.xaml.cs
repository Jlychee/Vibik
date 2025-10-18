using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace Vibik;

public partial class MapPage
{
    public MapPage()
    {
        InitializeComponent();

        var ekbCenter = new Location(56.8389, 60.6057);
        
        var region = MapSpan.FromCenterAndRadius(ekbCenter, Distance.FromKilometers(5));
        var map = new Map
        {
            IsShowingUser = false,
            MapType = MapType.Street,
        };
        
        map.MoveToRegion(region);
        
        map.Pins.Add(new Pin
        {
            Label = "Екатеринбург",
            Location = ekbCenter
        });
        
        Content = map;
    }
}
