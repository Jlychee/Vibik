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
//
// using Microsoft.Maui.Controls.Maps;
// using Microsoft.Maui.Maps;
// using Vibik.Utils;
//
// namespace Vibik;
//
// public partial class MapPage
// {
//     private readonly SnowfallDrawable snowfall = new();
//     private DateTime lastUpdate = DateTime.UtcNow;
//
//     public MapPage()
//     {
//         InitializeComponent();
//         InitializeMap();
//         SnowView.Drawable = snowfall;
//         InitializeSnowfall();
//     }
//
//     private void InitializeMap()
//     {
//         var ekbCenter = new Location(56.8389, 60.6057);
//         var region = MapSpan.FromCenterAndRadius(ekbCenter, Distance.FromKilometers(5));
//
//         MapControl.IsShowingUser = false;
//         MapControl.MapType = MapType.Street;
//         MapControl.MoveToRegion(region);
//
//         MapControl.Pins.Add(new Pin
//         {
//             Label = "Екатеринбург",
//             Location = ekbCenter
//         });
//         
//         
//     }
//
//     private void InitializeSnowfall()
//     {
//         Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
//         {
//             var now = DateTime.UtcNow;
//             var dt = (float)(now - lastUpdate).TotalSeconds;
//             lastUpdate = now;
//
//             snowfall.Update(dt);
//             SnowView.Invalidate();
//
//             return true;
//         });
//
//     }
// }
