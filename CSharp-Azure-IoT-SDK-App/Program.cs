using System;
using System.Text;
using System.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using System.Net.NetworkInformation;

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
                        // Gather and send system information
                        string systemInfo = $"{{\"os\": \"{Environment.OSVersion}\", \"machine\": \"{Environment.MachineName}\"}}";
                        await SendTelemetry(deviceClient, systemInfo);

                        // Gather and send file system information
                        string fileSystemInfo = GetFileSystemInfo();
                        await SendTelemetry(deviceClient, fileSystemInfo);

                        // Gather and send network information
                        string networkInfo = GetNetworkInfo();
                        await SendTelemetry(deviceClient, networkInfo);

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

    static string GetFileSystemInfo()
    {
        // Implement file system information retrieval here
        // Example: Get information about available drives and their free space
        DriveInfo[] allDrives = DriveInfo.GetDrives();
        string fileSystemInfo = $"{{\"drives\": [";
        foreach (DriveInfo drive in allDrives)
        {
            fileSystemInfo += $"{{\"name\": \"{drive.Name}\", \"totalSpace\": {drive.TotalSize}, \"freeSpace\": {drive.TotalFreeSpace}}},";
        }
        fileSystemInfo = fileSystemInfo.TrimEnd(',') + "]}}";
        return fileSystemInfo;
    }

    static string GetNetworkInfo()
    {
        // Implement network information retrieval here
        // Example: Get information about network interfaces
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        string networkInfo = $"{{\"networkInterfaces\": [";
        foreach (NetworkInterface nic in networkInterfaces)
        {
            networkInfo += $"{{\"name\": \"{nic.Description}\", \"type\": \"{nic.NetworkInterfaceType}\", \"speed\": {nic.Speed}}},";
        }
        networkInfo = networkInfo.TrimEnd(',') + "]}}";
        return networkInfo;
    }
}