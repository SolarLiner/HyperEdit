using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit
{
    public class HelpWindow : Window
    {
        public HelpWindow()
        {
            EnsureSingleton(this);
            Title = "Help";
            WindowRect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 200, 500, 400);
            Contents = new List<IWindowContent>
                {
                    new Scroller(new[]{(IWindowContent)new Label(HelpContents) }),
                    new Button("Close", CloseWindow)
                };
        }

        private const string HelpContents = @"Main window:
Edit an orbit: Opens the orbit editor window, which edits any and all orbits
Land your ship: Opens the lander window, which automatically lands the active vessel
Misc tools: Random functions Khyperia has found useful to include

Orbit editor window:
Select orbit: Chooses the orbit to edit. This is not the destination, as might be suggested by planetary names - if you select a planet, you are actually editing that planet's orbit
Simple: Teleports yourself to a circular, equatorial orbit at the specified altitude
Complex: Edit raw Keplarian orbital components (see wikipedia)
Graphical: Use sliders to edit Keplarian orbital components and see results immediately (advised to use map view)
Velocity: Edit instantaneous velocity of the orbit
Rendezvous (only when editing a vessel's orbit): Teleport to (nearly) the same orbit as another ship, at 'lead time' seconds before the other ship

Lander window:
Pressing land teleports you to latitude/longitude at altitude above the terrain and slowly lowers you to the ground.
Pressing land again cancels the landing (you'll probably fall and explode, so it's advised to do something to prevent that right after canceling)
Save coordanates saves the currently entered coordanates to disk, prompting you to name it
Load coordanates loads a coordanate pair from disk by name
Delete coordanates deletes a coordanate pair off the disk
Set to current position sets lat/lon to the current vessel's position (useful for saving a spot you're at)

Misc editor:
Refill ship resources sets all resources on your ship to their maximum capacity.
Time sets the Universal Time of your save game, in seconds.
Destroy a vessel kills the vessel you select. (Killing the active vessel has... interesting results)
Align SMA sets many orbits' semi major axis to be equal, which makes their period be exactly the same - useful for satellite constellations
(note: if the active vessel is one of the satellites you are setting, it is advised to go into non-physical warp, since if the ship runs physics for even one frame, the alignment messes up)
(also, Bad Things(tm) will happen if you choose satellites on different planets or ones that are landed)

Side notes:
This is a highly eccentric plugin, so there will be lots of bugs and explosions - please tell khyperia if you find one.
All text input fields can be suffixed with a SI multiplier. For example, 10k turns into 10000. Avalible SI suffixes (case sensitive) (largest to smallest):
Y, Z, E, P, T, G, M, k, h, da, (no suffix), d, c, m, u, n, p, f, a, z, y";
    }
}
