using System;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;
using System.Drawing;

public class SpeedRadarGTA : Script
{
    private Vector3 radarMarker;
    private float radarSpeedLimit;
    private bool radarActivated;
    private Vehicle lastDetectedVehicle;
    private Vector3 markerPos;

    public SpeedRadarGTA()
    {
        radarSpeedLimit = 60.0f;
        radarActivated = false;
        lastDetectedVehicle = null;
        radarMarker = Game.Player.Character.Position;

        Tick += OnTick;
        KeyUp += OnKeyUp;
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (radarActivated)
        {
            World.DrawMarker(MarkerType.VerticalCylinder, radarMarker, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), Color.Red);

            Vehicle nearestVehicle = World.GetClosestVehicle(radarMarker, 10.0f);
            if (nearestVehicle != null && nearestVehicle != lastDetectedVehicle && nearestVehicle != Game.Player.Character.CurrentVehicle)
            {
                float vehicleSpeed = nearestVehicle.Speed * 2.23693629f;
                string speedColor = vehicleSpeed > radarSpeedLimit ? "~r~" : "~w~";
                string plateNumber = nearestVehicle.Mods.LicensePlate;
                

                DisplaySubtitle($"Vehicle: {nearestVehicle.DisplayName}, Plate: {plateNumber}, Speed: {speedColor}{vehicleSpeed:F1} mph~w~");

                if (vehicleSpeed > radarSpeedLimit)
                {
                    lastDetectedVehicle = nearestVehicle;
                }
            }
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            radarActivated = !radarActivated;
            if (radarActivated)
            {
                DisplaySubtitle("Speed Radar Enabled");
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
                radarMarker.Y += 1.0f;
            }
            else if (e.KeyCode == Keys.NumPad4)
            {
                radarMarker.X -= 1.0f;
            }
            else if (e.KeyCode == Keys.NumPad6)
            {
                radarMarker.X += 1.0f;
            }
            else if (e.KeyCode == Keys.NumPad8)
            {
                radarMarker.Y -= 1.0f;
            }
            else if (e.KeyCode == Keys.NumPad0)
            {
                markerPos = new Vector3(markerPos.X, markerPos.Y, World.GetGroundHeight(markerPos));
            }
            else if (e.KeyCode == Keys.Add)
            {
                radarSpeedLimit += 5.0f;
                DisplaySubtitle($"Speed Limit: {radarSpeedLimit}");

                if (radarSpeedLimit > 200f)
                {
                    radarSpeedLimit = 60f;
                }
            }
            else if (e.KeyCode == Keys.Subtract)
            {
                radarSpeedLimit -= 5.0f;
                DisplaySubtitle($"Speed Limit: {radarSpeedLimit}");
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