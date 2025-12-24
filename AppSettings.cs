using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace Cropalicious
{
    public class AppSettings
    {
        public Keys HotkeyKey { get; set; } = Keys.C;
        public Keys HotkeyModifiers { get; set; } = Keys.Control | Keys.Shift;
        public int CaptureWidth { get; set; } = 1024;
        public int CaptureHeight { get; set; } = 1024;
        public string OutputFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Cropalicious");
        public List<CustomSize> CustomSizes { get; set; } = new List<CustomSize>();
        public int WindowWidth { get; set; } = 600;
        public int WindowHeight { get; set; } = 400;
        public int WindowX { get; set; } = -1;
        public int WindowY { get; set; } = -1;
        public bool StayOnTop { get; set; } = false;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cropalicious",
            "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}\nUsing defaults.", 
                    "Cropalicious", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                Directory.CreateDirectory(OutputFolder);
                
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", 
                    "Cropalicious", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class CustomSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}