using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;

namespace Flow.Launcher.Plugin.AudioDevices
{
    public class Main : IPlugin
    {
        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();
            try
            {
                var audioDevices = GetAudioDevices();

                if (audioDevices.Any())
                {
                    results.AddRange(audioDevices
                        .Select(device => CreateResult(device.Name, device.IsDefault, device))
                    );
                }
                else
                {
                    results.Add(CreateResult("No audio devices found", false));
                }
            }
            catch (Exception ex)
            {
                results.Add(CreateResult($"Error: {ex.Message}", false));
            }

            return results;
        }

        private List<AudioDevice> GetAudioDevices()
        {
            var devices = new List<AudioDevice>();
            try
            {
                string pb_script = "Get-PlaybackDevices.ps1";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy unrestricted -File \"{_context.CurrentPluginMetadata.PluginDirectory}/Scripts/{pb_script}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    return devices;
                }

                var output = process.StandardOutput.ReadToEnd();
                var errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return devices;
                }

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var parts = line.Split('|').Select(p => p.Trim()).ToArray();
                    if (parts.Length >= 3)
                    {
                        var index = parts[0];
                        var name = parts[1];
                        var isDefault = parts[2].Equals("True", StringComparison.OrdinalIgnoreCase);

                        devices.Add(new AudioDevice
                        {
                            Id = index,
                            Name = name,
                            IsDefault = isDefault
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving audio devices: {ex.Message}", ex);
            }

            return devices;
        }

        private Result CreateResult(string title, bool isDefault, AudioDevice device = null)
        {
            return new Result
            {
                Title = title,
                SubTitle = isDefault ? "Default" : "",
                IcoPath = "Images/icon.png",
                Action = c =>
                {
                    if (device != null)
                    {
                        SetDefaultPlaybackDevice(device.Id);
                        return true;
                    }
                    return false;
                }
            };

        }

        // set playback device as default
        private static void SetDefaultPlaybackDevice(string deviceId)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy unrestricted -Command \"Set-AudioDevice -Index {deviceId}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return;
            }

            var output = process.StandardOutput.ReadToEnd();
            var errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                return;
            }

        }

        private class AudioDevice
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public bool IsDefault { get; set; }
        }
    }
}