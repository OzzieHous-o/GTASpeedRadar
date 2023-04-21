using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Drawing;
using GTA.UI;
using System.IO;
using System.Collections.Generic;


public class SpeedRadarGTA : Script
{
    private Vector3 radarMarker;
    private float radarSpeedLimit;
    private bool radarActivated;
    private Vehicle lastDetectedVehicle;
    private TextElement speedLimitText;
    private TextElement vehicleSpeedText;

    private bool showViolationsGUI;
    private List<SpeedViolation> violations;
    private int violationDisplayDuration;
    private Vehicle targetVehicle;

    private Keys enableRadarKey;
    private Keys toggleViolationsGUIKey;
    private Keys increaseSpeedLimitKey;
    private Keys decreaseSpeedLimitKey;
    private bool useMPH;
    private string radarDirection;
    private float speedTolerance = 5.0f;
    private Color markerColor;
    public SpeedRadarGTA()
    {
        radarActivated = false;
        showViolationsGUI = false;
        lastDetectedVehicle = null;
        radarMarker = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 5.0f;
        
        string iniFilePath = Path.Combine(Directory.GetCurrentDirectory(), "scripts\\SpeedRadar.ini");
        violations = new List<SpeedViolation>();
        if (!File.Exists(iniFilePath))
        {
            using (StreamWriter sw = File.CreateText(iniFilePath))
            {
                sw.WriteLine("; SpeedRadar.ini - auto generated! - created by System32/ECMJET\n");
                sw.WriteLine("IncreaseSpeedLimitKey = PageUp");
                sw.WriteLine("DecreaseSpeedLimitKey = PageDown");
                sw.WriteLine("; Speed units can be either MPH or KMH");
                sw.WriteLine("SpeedUnits = KMH");
                sw.WriteLine("EnableRadar = F5");
                sw.WriteLine("; Default speed limit MUST be a number. Greater than 1");
                sw.WriteLine("DefaultSpeedLimit = 60");
                sw.WriteLine("; Toggle Violations Key");
                sw.WriteLine("ToggleViolationsGUI = NumPad7");
                sw.WriteLine("; Violation display duration in seconds. (This option is for how long it takes for a previous violation to be removed from the gui). MUST BE SECONDS. DEFAULT is 10");
                sw.WriteLine("ViolationDisplayDuration = 10");
                sw.WriteLine("; RadarDirection. Options: Both, Towards, Away");
                sw.WriteLine("RadarDirection = Both");
                sw.WriteLine("; Speed tolerance in units (either MPH or KMH depending on the setting). Default is 5. This is how far over the speedlimit the driver has to be for it to be a crime.");
                sw.WriteLine("SpeedTolerance = 5");
                sw.WriteLine("; Marker color in RGB format. Default is Red");
                sw.WriteLine("MarkerColor = 255,0,0");
            }
        }

        string[] lines = File.ReadAllLines(iniFilePath);
        foreach (string line in lines)
        {
            string[] parts = line.Split('=');
            if (parts.Length !=2)
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
            else if (key == "ToggleViolationsGUI")
            {
                toggleViolationsGUIKey = (Keys)Enum.Parse(typeof(Keys), value, true);
                
            }            
            else if (key == "ViolationDisplayDuration")
            {
                if (int.TryParse(value, out int parsedValue) && parsedValue > 0)
                {
                    violationDisplayDuration = parsedValue;
                }
                else
                {
                    violationDisplayDuration = 10;
                }
            }
            else if (key == "DefaultSpeedLimit")
            {
                radarSpeedLimit = float.Parse(value);
            }
            else if (key == "RadarDirection")
            {
                radarDirection = value;
            }
            else if (key == "SpeedTolerance")
            {
                speedTolerance = float.Parse(value);
            }
            else if (key == "MarkerColor")
            {
                string[] rgb = value.Split(',');
                if (rgb.Length == 3 && int.TryParse(rgb[0], out int r) && int.TryParse(rgb[1], out int g) && int.TryParse(rgb[2], out int b))
                {
                    markerColor = Color.FromArgb(r, g, b);
                }
                else
                {
                    markerColor = Color.Red;
                }
            }
        }
        speedLimitText = new TextElement("Speed Limit: " + radarSpeedLimit.ToString("F0") + (useMPH ? " M/PH" : " K/MH"), new Point(10, 50), 0.5f, Color.White, GTA.UI.Font.Pricedown, Alignment.Left);
        vehicleSpeedText = new TextElement("", new Point(10, 80), 0.5f, Color.White, GTA.UI.Font.Pricedown, Alignment.Left);
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

            if (showViolationsGUI)
            {
                DrawViolationsGUI();
            }

            if (targetVehicle != null && targetVehicle.Exists())
            {

            }

            speedLimitText.Draw();
            //World.DrawMarker(MarkerType.VerticalCylinder, radarMarker, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), Color.Red);
            World.DrawMarker(MarkerType.VerticalCylinder, radarMarker, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), markerColor);

            Vehicle nearestVehicle = World.GetClosestVehicle(radarMarker, 10.0f);
            if (nearestVehicle != null && nearestVehicle != lastDetectedVehicle)
            {
                if(nearestVehicle.ClassType == VehicleClass.Emergency)
                {
                    return;
                }
                Vector3 vehicleToRadar = radarMarker - nearestVehicle.Position;
                float dotProduct = Vector3.Dot(nearestVehicle.Velocity.Normalized, Game.Player.Character.ForwardVector);

                if ((radarDirection == "Towards" && dotProduct > 0) || (radarDirection == "Away" && dotProduct < 0))
                {
                    return;
                }

                float vehicleSpeed = nearestVehicle.Speed * 2.23693629f;
                string speedColor = vehicleSpeed > radarSpeedLimit ? "~r~" : "~w~";
                string plateNumber = nearestVehicle.Mods.LicensePlate;

                DisplaySubtitle($"Vehicle: {nearestVehicle.DisplayName}, Plate: {plateNumber}, Speed: {speedColor}{vehicleSpeed:F1} {(useMPH ? "MPH" : "KMH")}~w~");
                if (vehicleSpeed > radarSpeedLimit + speedTolerance)
                {
                    violations.Add(new SpeedViolation(nearestVehicle.DisplayName, plateNumber, vehicleSpeed));
                    lastDetectedVehicle = nearestVehicle;
                }
            }
        }
    }
    public class SpeedViolation
    {
        public string VehicleName { get; set; }
        public string PlateNumber { get; set; }
        public float Speed { get; set; }
        public DateTime TimeOfViolation { get; set; }

        public TimeSpan Age
        {
            get { return DateTime.Now - TimeOfViolation; }
        }
        public SpeedViolation(string vehicleName, string plateNumber, float speed)
        {
            VehicleName = vehicleName;
            PlateNumber = plateNumber;
            Speed = speed;
            TimeOfViolation = DateTime.Now;
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
                radarMarker = new Vector3(radarMarker.X, radarMarker.Y, World.GetGroundHeight(radarMarker));
                World.DrawMarker(MarkerType.VerticalCylinder, radarMarker, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), Color.Red);
                Notification.Show(NotificationIcon.SocialClub, "Speed Radar", "Instructions", "Please read the ini file for all settings. scripts\\SpeedRadar.ini\n\n Created by System32");
            }
            else
            {
                DisplaySubtitle("Speed Radar Disabled");
            }
        }            
        else if (radarActivated)
        {
            Vector3 forwardVector;

            if (Game.Player.Character.IsInVehicle())
            {
                forwardVector = Game.Player.Character.CurrentVehicle.ForwardVector;
            }
            else
            {
                forwardVector = Game.Player.Character.ForwardVector;
            }
            forwardVector.Z = 0; // Ignore vertical movement
            if (e.KeyCode == toggleViolationsGUIKey)
            {
                showViolationsGUI = !showViolationsGUI;
            }

            if (e.KeyCode == Keys.NumPad2)
            {
                radarMarker -= forwardVector * 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad4)
            {
                radarMarker -= Vector3.Cross(forwardVector, Vector3.WorldUp) * 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad6)
            {
                radarMarker += Vector3.Cross(forwardVector, Vector3.WorldUp) * 2.0f;
            }
            else if (e.KeyCode == Keys.NumPad8)
            {
                radarMarker += forwardVector * 2.0f;
            }
            else if (e.KeyCode == increaseSpeedLimitKey)
            {
                radarSpeedLimit += 5.0f;

                if (radarSpeedLimit > 200f)
                {
                    radarSpeedLimit = 60f;
                }
                speedLimitText.Caption = "Speed Limit: " + radarSpeedLimit.ToString("F0") + (useMPH ? " M/PH" : " K/MH");
            }            
        else if (e.KeyCode == decreaseSpeedLimitKey)
        {
            radarSpeedLimit -= 5.0f;
            if (radarSpeedLimit < 0)
            {
                radarSpeedLimit = 60;
            }
            speedLimitText.Caption = "Speed Limit: " + radarSpeedLimit.ToString("F0") + (useMPH ? " M/PH" : " K/MH");
        }
        }
    }
    private void DrawRec(float xPos, float yPos, float width, float height, System.Drawing.Color color)
    {
        float w = width / 1280;
        float h = height / 720;
        float x = (xPos / 1280) + w * 0.5F;
        float y = (yPos / 720) + h * 0.5F;
        Function.Call(Hash.DRAW_RECT, x, y, w, h, color.R, color.G, color.B, color.A);
    }
    private void DrawViolationsGUI()
    {
        const int maxViolationsToShow = 10;
        const int lineHeight = 20;
        const int initialY = 100;
        const int backgroundWidth = 400;
        const int backgroundHeight = maxViolationsToShow * lineHeight + 10;
        int counter = 0;

        var backgroundColor = Color.FromArgb(150, 0, 0, 0);
        float screenWidth = 1280;
        float xPos = screenWidth - backgroundWidth - 5;

        DrawRec(xPos, initialY - 5, backgroundWidth, backgroundHeight, backgroundColor);
        foreach (var violation in violations)
        {
            if (counter >= maxViolationsToShow)
            {
                break;
            }

            if (violation.Age.TotalSeconds > violationDisplayDuration)
            {
                continue;
            }

            string speedColor = violation.Speed > radarSpeedLimit ? "~r~" : "~w~";
            var text = $"Vehicle: {violation.VehicleName}, Plate: {violation.PlateNumber}, Speed: {speedColor}{violation.Speed:F1}{(useMPH ? " M/PH" : " K/MH")}~w~, Time: {violation.TimeOfViolation.ToShortTimeString()}";
            var screenText = new TextElement(text, new Point((int)xPos + 5, initialY + (counter * lineHeight)), 0.30f, Color.White, GTA.UI.Font.ChaletLondon, Alignment.Left);
            screenText.Draw();
            counter++;
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