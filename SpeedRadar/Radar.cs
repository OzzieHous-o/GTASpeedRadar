using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Drawing;
using GTA.UI;
using System.IO;


public class SpeedRadarGTA : Script
{
    private Vector3 radarMarker;
    private float radarSpeedLimit;
    private bool radarActivated;
    private Vehicle lastDetectedVehicle;
    private Vector3 markerPos;
    private TextElement speedLimitText;
    private TextElement vehicleSpeedText;

    private Keys increaseSpeedLimitKey;
    private Keys decreaseSpeedLimitKey;
    private bool useMPH;
    private Keys enableRadarKey;


    public SpeedRadarGTA()
    {
        radarSpeedLimit = 60.0f;
        radarActivated = false;
        lastDetectedVehicle = null;
        radarMarker = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 5.0f;

        speedLimitText = new TextElement("Speed Limit: " + radarSpeedLimit.ToString("F0") + (useMPH ? "MPH" : "KMH"), new Point(10, 50), 0.5f, Color.White, GTA.UI.Font.Pricedown, Alignment.Left);
        vehicleSpeedText = new TextElement("", new Point(10, 80), 0.5f, Color.White, GTA.UI.Font.Pricedown, Alignment.Left);

        string iniFilePath = Path.Combine(Directory.GetCurrentDirectory(), "scripts\\SpeedRadar.ini");
        if (File.Exists(iniFilePath))
        {
            string[] lines = File.ReadAllLines(iniFilePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }
                string key = parts[0].Trim();
                string value = parts[1].Trim();
                if (key == "IncreaseSpeedLimitKey")
                {
                    increaseSpeedLimitKey = (Keys)Enum.Parse(typeof(Keys), value, true);
                }
                else if (key == "DecreaseSpeedLimitKey")
                {
                    decreaseSpeedLimitKey = (Keys)Enum.Parse(typeof(Keys), value, true);
                }
                else if (key == "SpeedUnits")
                {
                    useMPH = value == "MPH";
                }
                else if (key == "EnableRadar")
                {
                    enableRadarKey = (Keys)Enum.Parse(typeof(Keys), value, true);
                }
            }
        }

        Tick += OnTick;
        KeyUp += OnKeyUp;
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (radarActivated)
        {
            if (radarMarker == null)
            {
                radarMarker = Game.Player.Character.Position;
            }
            if (Game.Player.Character.Position.DistanceTo(radarMarker) >= 25f)
            {
                radarMarker = Game.Player.Character.Position;
            }

            speedLimitText.Draw();
            World.DrawMarker(MarkerType.VerticalCylinder, radarMarker, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), Color.Red);

            Vehicle nearestVehicle = World.GetClosestVehicle(radarMarker, 10.0f);
            if (nearestVehicle != null && nearestVehicle != lastDetectedVehicle)
            {
                float vehicleSpeed = nearestVehicle.Speed * 2.23693629f;
                string speedColor = vehicleSpeed > radarSpeedLimit ? "~r~" : "~w~";
                string plateNumber = nearestVehicle.Mods.LicensePlate;



                DisplaySubtitle($"Vehicle: {nearestVehicle.DisplayName}, Plate: {plateNumber}, Speed: {speedColor}{vehicleSpeed:F1} {(useMPH ? "MPH" : "KMH")}~w~"); 
                if (vehicleSpeed > radarSpeedLimit)
                {
                    lastDetectedVehicle = nearestVehicle;
                }
            }
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
       if (e.KeyCode == enableRadarKey)
        {
            radarActivated = !radarActivated;
            if (radarActivated)
            {
                DisplaySubtitle("Speed Radar Enabled");
                Notification.Show(NotificationIcon.Facebook, "Speed Radar", "Instructions", "Use numpad keys ~g~(2,4,6,8)~w~ to move the marker.\n~g~PageUp~w~, ~g~PageDown~w~ to increase/decrease speedlimit.\n\nSee ~y~SpeedRadar.ini~w~ for more.");
            }
            else
            {
                DisplaySubtitle("Speed Radar Disabled");
            }
        }
        else if (radarActivated)
        {
            if (e.KeyCode == Keys.NumPad2)
            {
                radarMarker.Y += 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad4)
            {
                radarMarker.X -= 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad6)
            {
                radarMarker.X += 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad8)
            {
                radarMarker.Y -= 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad0)
            {
                markerPos = new Vector3(markerPos.X, markerPos.Y, World.GetGroundHeight(markerPos));

            }
            else if (e.KeyCode == increaseSpeedLimitKey)
            {
                radarSpeedLimit += 5.0f;


                if (radarSpeedLimit > 200f)
                {
                    radarSpeedLimit = 60f;
                }
                speedLimitText.Caption = "Speed Limit: " + radarSpeedLimit.ToString("F0") + (useMPH ? "MPH" : "KMH");
            }
            else if (e.KeyCode == decreaseSpeedLimitKey)
            {
                radarSpeedLimit -= 5.0f;
                if (radarSpeedLimit < 0)
                {
                    radarSpeedLimit = 0;
                }
                speedLimitText.Caption = "Speed Limit: " + radarSpeedLimit.ToString("F0") + (useMPH ? "MPH" : "KMH");
            }
        }
    }

    private void DisplaySubtitle(string text, Color textColor = default, int duration = 5000)
    {
        if (textColor == default)
        {
            textColor = Color.White;
        }
        string hexColor = $"~#{textColor.R:X2}{textColor.G:X2}{textColor.B:X2}~";
        GTA.UI.Screen.ShowSubtitle(hexColor + text, duration);
    }
}