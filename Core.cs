﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Thanks to Kine for the UnitTest idea
public class HyperEditModule : KSP.Testing.UnitTest
{
    public HyperEditModule()
    {
        HyperEdit.Immortal.AddImmortal<HyperEdit.HyperEditBehaviour>();
    }
}

namespace HyperEdit
{
    public class HyperEditBehaviour : MonoBehaviour
    {
        private static Krakensbane _krakensbane;
        private static readonly HyperEditWindow HyperEditWindow = new HyperEditWindow();

        public static Krakensbane Krakensbane
        {
            get { return _krakensbane ?? (_krakensbane = (Krakensbane)FindObjectOfType(typeof(Krakensbane))); }
        }

        public void Update()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.H))
                HyperEditWindow.OpenWindow();
        }
    }

    public static class ErrorPopup
    {
        public static void Error(string message)
        {
            PopupDialog.SpawnPopupDialog("Error", message, "Close", true, HighLogic.Skin);
        }
    }

    public class HyperEditWindow : Window
    {
        public HyperEditWindow()
        {
            EnsureSingleton(this);
            Title = "HyperEdit";
            WindowRect = new Rect(50, 50, 100, 100);
            Contents = new List<IWindowContent>
                {
                    new Button("Close all windows", CloseAll),
                    new Button("Help", new HelpWindow().OpenWindow),
                    new Button("Edit an orbit", new OrbitEditor().OpenWindow),
                    new Button("Land your ship", new Lander().OpenWindow),
                    new Button("Misc tools", new MiscTools().OpenWindow)
                };
        }
    }

    public static class Settings
    {
        private static GUIStyle _pressedButton;
        public static GUIStyle PressedButton
        {
            get
            {
                return _pressedButton ?? (_pressedButton = new GUIStyle(HighLogic.Skin.button)
                    {
                        normal = HighLogic.Skin.button.active,
                        hover = HighLogic.Skin.button.active,
                        active = HighLogic.Skin.button.normal
                    });
            }
        }
    }

    public static class Si
    {
        private static readonly Dictionary<string, double> Suffixes = new Dictionary<string, double>
            {
                {"Y", 1e24},
                {"Z", 1e21},
                {"E", 1e18},
                {"P", 1e15},
                {"T", 1e12},
                {"G", 1e9},
                {"M", 1e6},
                {"k", 1e3},
                {"h", 1e2},
                {"da", 1e1},

                {"d", 1e-1},
                {"c", 1e-2},
                {"m", 1e-3},
                {"u", 1e-6},
                {"n", 1e-9},
                {"p", 1e-12},
                {"f", 1e-15},
                {"a", 1e-18},
                {"z", 1e-21},
                {"y", 1e-24}
            };

        public static bool TryParse(string s, out double value)
        {
            s = s.Trim();
            double multiplier;
            var suffix = Suffixes.FirstOrDefault(suf => s.EndsWith(suf.Key));
            if (suffix.Key != null)
            {
                s = s.Substring(0, s.Length - suffix.Key.Length);
                multiplier = suffix.Value;
            }
            else
                multiplier = 1.0;
            if (double.TryParse(s, out value) == false)
                return false;
            value *= multiplier;
            return true;
        }

        public static string ToSiString(this double value)
        {
            var log = Math.Log10(Math.Abs(value));
            var minDiff = double.MaxValue;
            var minSuffix = new KeyValuePair<string, double>("", 1);
            foreach (var suffix in Suffixes.Concat(new[] { new KeyValuePair<string, double>("", 1) }))
            {
                var diff = Math.Abs(log - Math.Log10(suffix.Value));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    minSuffix = suffix;
                }
            }
            value /= minSuffix.Value;
            return value.ToString("F") + minSuffix.Key;
        }
    }

    public static class Extentions
    {
        public static bool ActiveVesselNullcheck(this Window window)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
            {
                ErrorPopup.Error("Could not find the active vessel (are you in the flight scene?)");
                window.CloseWindow();
                return true;
            }
            return false;
        }

        public static Vessel GetVessel(this Orbit orbit)
        {
            return FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
        }

        public static CelestialBody GetPlanet(this Orbit orbit)
        {
            return FlightGlobals.fetch == null ? null : FlightGlobals.Bodies.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
        }

        public static void Set(this Orbit orbit, Orbit newOrbit)
        {
            var vessel = FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
            var body = FlightGlobals.fetch == null ? null : FlightGlobals.Bodies.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
            if (vessel != null)
                WarpShip(vessel, newOrbit);
            else if (body != null)
                WarpPlanet(body, newOrbit);
            else
                HardsetOrbit(orbit, newOrbit);
        }

        private static void WarpShip(Vessel vessel, Orbit newOrbit)
        {
            if (newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude > newOrbit.referenceBody.sphereOfInfluence)
            {
                ErrorPopup.Error("Destination position was above the sphere of influence");
                return;
            }

            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = string.Empty;
            var parts = vessel.parts;
            if (parts != null)
            {
                var clamps = parts.Where(p => p.Modules != null && p.Modules.OfType<LaunchClamp>().Any()).ToList();
                foreach (var clamp in clamps)
                    clamp.Die();
            }

            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
            }

            foreach (var v in (FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels).Where(v => v.packed == false))
                v.GoOnRails();

            HardsetOrbit(vessel.orbit, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        private static void WarpPlanet(CelestialBody body, Orbit newOrbit)
        {
            var oldBody = body.referenceBody;
            HardsetOrbit(body.orbit, newOrbit);
            if (oldBody != newOrbit.referenceBody)
            {
                oldBody.orbitingBodies.Remove(body);
                newOrbit.referenceBody.orbitingBodies.Add(body);
            }
            body.CBUpdate();
        }

        private static void HardsetOrbit(Orbit orbit, Orbit newOrbit)
        {
            orbit.inclination = newOrbit.inclination;
            orbit.eccentricity = newOrbit.eccentricity;
            orbit.semiMajorAxis = newOrbit.semiMajorAxis;
            orbit.LAN = newOrbit.LAN;
            orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            orbit.epoch = newOrbit.epoch;
            orbit.referenceBody = newOrbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(Planetarium.GetUniversalTime());
        }

        public static void Teleport(this Krakensbane krakensbane, Vector3d offset)
        {
            foreach (var vessel in FlightGlobals.Vessels.Where(v => v.packed == false && v != FlightGlobals.ActiveVessel))
                vessel.GoOnRails();
            krakensbane.setOffset(offset);
        }

        public static Rect Set(this Rect rect, int width, int height)
        {
            return new Rect(rect.xMin, rect.yMin, width, height);
        }

        public static Orbit Clone(this Orbit o)
        {
            return new Orbit(o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN,
                             o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, o.referenceBody);
        }

        public static float Soi(this CelestialBody body)
        {
            var radius = (float)(body.sphereOfInfluence * 0.95);
            if (Planetarium.fetch != null && body == Planetarium.fetch.Sun || float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0 || radius > 200000000000f)
                radius = 200000000000f; // jool apo = 72,212,238,387
            return radius;
        }

        public static string Aggregate(this IEnumerable<string> source, string middle)
        {
            return source.Aggregate("", (total, part) => total + middle + part).Substring(middle.Length);
        }
    }


    /// <summary>
    /// This class handles KSP <-> HyperEdit interaction. Picking funcs and "say something to the screen in order for him to show it to the player" ahead !
    /// </summary>
    public static class Interaction
    {
        static bool IsPickingActivated = false;
        
        /// <summary>
        /// Shows "<paramref name="message"/>" for 3 seconds in the upper center of the screen, like the "Time Warp xNN"
        /// </summary>
        /// <param name="message">Message to show</param>
        public static void Say(string message)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_CENTER));
        }

        /// <summary>
        /// Shows "<paramref name="message"/>" for <paramref name="duration" /> seconds, like the "Time Warp xNN"
        /// </summary>
        /// <param name="message">Message to show</param>
        /// <param name="duration">Duration in seconds</param>
        public static void Say(string message, double duration)
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage(message, (float)duration, ScreenMessageStyle.UPPER_CENTER));
        }

        
        /// <summary>
        /// Thanks on MechJeb for this one. Shows the player the map to let him choose coordinates.
        /// </summary>
        public static void PickCoords()
        {
            if (!IsPickingActivated) return;
            if (!MapView.MapIsEnabled) MapView.EnterMapView();


        }
    }
    
    /// <summary>
    /// Coordinates things, ripped from MechJeb too, this thing is too damn awesome :D
    /// Gives funcs to input and output real DMS formatted strings.
    /// </summary>
    public class Coordinates
    {
        public double latitude;
        public double longitude;

        public string DMSlatitude /// Latitude in DMS format (x° x' x")
        {
            get { return AngleToDMS(latitude); }
            set { latitude = AngleFromDMS(value); }
        }
        public string DMSlongitude /// Longitude in DMS format (x° x' x")
        {
            get { return AngleToDMS(longitude); }
            set { longitude = AngleFromDMS(value); }
        }

        public Coordinates(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public static string ToStringDecimal(double latitude, double longitude, bool newline = false, int precision = 3)
        {
            double clampedLongitude = ClampDegrees180(longitude);
            double latitudeAbs = Math.Abs(latitude);
            double longitudeAbs = Math.Abs(clampedLongitude);
            return latitudeAbs.ToString("F" + precision) + "° " + (latitude > 0 ? "N" : "S") + (newline ? "\n" : ", ")
                + longitudeAbs.ToString("F" + precision) + "° " + (clampedLongitude > 0 ? "E" : "W");
        }

        public string ToStringDecimal(bool newline = false, int precision = 3)
        {
            return ToStringDecimal(latitude, longitude, newline, precision);
        }

        public static string ToStringDMS(double latitude, double longitude, bool newline = false)
        {
            double clampedLongitude = ClampDegrees180(longitude);
            return AngleToDMS(latitude) + (latitude > 0 ? " N" : " S") + (newline ? "\n" : ", ")
                 + AngleToDMS(clampedLongitude) + (clampedLongitude > 0 ? " E" : " W");
        }

        public string ToStringDMS(bool newline = false)
        {
            return ToStringDMS(latitude, longitude, newline);
        }

        public static string AngleToDMS(double angle)
        {
            int degrees = (int)Math.Floor(Math.Abs(angle));
            int minutes = (int)Math.Floor(60 * (Math.Abs(angle) - degrees));
            int seconds = (int)Math.Floor(3600 * (Math.Abs(angle) - degrees - minutes / 60.0));

            return String.Format("{0:0}° {1:00}' {2:00}\"", degrees, minutes, seconds);
        }
        public static double AngleFromDMS(string DMS)
        {
            if (!(DMS.Contains('°') && DMS.Contains('\'') && DMS.Contains('"'))) return -1;
            string[] split = DMS.Split(new char[] {'°', '\'', '"'});

            int degree = 0, minute = 0, second = 0;
            if (!(int.TryParse(split[0], out degree) && int.TryParse(split[1], out minute) && int.TryParse(split[2], out second))) return -1;

            return Math.Abs(degree + (minute / 60) + (second / 3600));
        }

        static double ClampDegrees180(double angle)
        {
            angle = ClampDegrees360(angle);
            if (angle > 180) angle -= 360;
            return angle;
        }

        static double ClampDegrees360(double angle)
        {
            angle = angle % 360.0;
            if (angle < 0) return angle + 360.0;
            else return angle;
        }
    }
}
