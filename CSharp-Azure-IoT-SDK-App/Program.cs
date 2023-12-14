using System;
using System.Text;
using System.Configuration;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

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
                    // Your application logic here

                    // Simulate sending telemetry data
                    for (int i = 0; i < 10; i++)
                    {
                        string telemetryData = $"{{\"temperature\": {20 + i}}}";
                        var message = new Message(Encoding.UTF8.GetBytes(telemetryData));

                        await deviceClient.SendEventAsync(message);
                        Console.WriteLine($"Telemetry sent: {telemetryData}");

                        await Task.Delay(1000); // Simulate a delay between telemetry messages
                    }

                    // Ensure the device stays connected
                    Console.WriteLine("Press Enter to exit.");
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine($"Device registration failed: {result.Status}");
            }
        }
    }

}