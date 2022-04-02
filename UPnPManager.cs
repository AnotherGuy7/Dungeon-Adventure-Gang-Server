using Mono.Nat;
using System;
using System.Collections.Generic;

namespace DAGServer
{
    public class UPnPManager
    {
        public static List<INatDevice> connectedDevices = new List<INatDevice>();

        public static void InitializeUPnP()
        {
            Logger.LogCustomMessage("Enable UPnP? Y/N");
            Logger.LogCustomMessage("Warning: UPnP is a protocol that allows clients to bypass all protections against invalid data in order to create an environment where all packets can arrive. " +
                                        "\nIt should only be used when connecting to trustworthy clients. If you are using this option to connect to untrusted people over the Internet, it is at your own risk.", ConsoleColor.Red);

            ConsoleKeyInfo key = Console.ReadKey();
            if (key.Key == ConsoleKey.Y)
            {
                Logger.LogCustomMessage("UPnP Activated.", ConsoleColor.Blue);
                Logger.LogCustomMessage("Hosting on: \nIP: " + connectedDevices[0].GetExternalIP() + "\nPort: " + Server.NetworkPort + "\n", ConsoleColor.Blue);
            }

            Console.WriteLine("\n");
            NatUtility.DeviceFound += DeviceFound;
            NatUtility.UnknownDeviceFound += UnknownDeviceFound;
            //NatUtility.StartDiscovery(NatProtocol.Upnp);
            NatUtility.Search(System.Net.IPAddress.Parse("192.168.0.1"), NatProtocol.Upnp);
        }

        public static void DisableUPnP()
        {
            NatUtility.StopDiscovery();
        }



        private static void DeviceFound(object sender, DeviceEventArgs args)
        {
            INatDevice device = args.Device;
            connectedDevices.Add(device);
            Logger.LogCustomMessage("Device found: " + device.GetType().Name);

            //Creating the open port
            device.CreatePortMap(new Mapping(Protocol.Tcp, Server.NetworkPort, Server.NetworkPort));
        }

        private static void UnknownDeviceFound(object sender, DeviceEventUnknownArgs e)
        {

        }
    }
}
