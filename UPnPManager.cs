using Mono.Nat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DAGServer
{
    public class UPnPManager
    {
        public static List<INatDevice> connectedDevices = new List<INatDevice>();

        public static void InitializeUPnP()
        {
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
