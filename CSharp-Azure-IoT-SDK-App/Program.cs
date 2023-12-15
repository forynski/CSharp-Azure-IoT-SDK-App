using System;
using System.Text;
using System.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System.Net.NetworkInformation;
using System.Diagnostics;

class Program
{
    static async Task Main(string[] args)
    {

        // Device Provisioning Service parameters
        string dpsIdScope = ConfigurationManager.AppSettings["DpsIdScope"];
        string dpsGlobalDeviceEndpoint = ConfigurationManager.AppSettings["DpsGlobalDeviceEndpoint"];
        string deviceId = ConfigurationManager.AppSettings["DeviceId"];
        string deviceKey = ConfigurationManager.AppSettings["DeviceKey"];

        var security = new SecurityProviderSymmetricKey(deviceId, deviceKey, null);

        // Create a DeviceClient for IoT Hub communication using MQTT
        using (var securityClient = new ProvisioningTransportHandlerMqtt())
        {
            var provisioningClient = ProvisioningDeviceClient.Create(dpsGlobalDeviceEndpoint, dpsIdScope, security, securityClient);

            DeviceRegistrationResult result = await provisioningClient.RegisterAsync();

            if (result.Status == ProvisioningRegistrationStatusType.Assigned)
            {
                var auth = new DeviceAuthenticationWithRegistrySymmetricKey(result.DeviceId, security.GetPrimaryKey());

                using (var deviceClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Mqtt))
                {
                    // Run a continuous loop with a delay of 5 seconds
                    while (true)
                    {
                        // Gather and send power metrics data
                        string powerMetricsInfo = RunPowerMetricsCommandWithSudo();
                        await SendTelemetry(deviceClient, powerMetricsInfo);

                        // Delay for 5 seconds before the next iteration
                        await Task.Delay(5000);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Device registration failed: {result.Status}");
            }
        }
    }

    static async Task SendTelemetry(DeviceClient deviceClient, string telemetryData)
    {
        var message = new Message(Encoding.UTF8.GetBytes(telemetryData));
        await deviceClient.SendEventAsync(message);
        Console.WriteLine($"Telemetry sent: {telemetryData}");
    }

    static string RunPowerMetricsCommandWithSudo()
    {
        string powerMetricsOutput = string.Empty;

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/sudo",
                Arguments = "/usr/bin/powermetrics",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = psi })
            {
                process.Start();

                using (StreamReader reader = process.StandardOutput)
                {
                    // Read only the first three lines of powermetrics output
                    for (int i = 0; i < 3; i++)
                    {
                        powerMetricsOutput += reader.ReadLine() + Environment.NewLine;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running powermetrics with sudo: {ex.Message}");
        }

        return powerMetricsOutput;
    }
}