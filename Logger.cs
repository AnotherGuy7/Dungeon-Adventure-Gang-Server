﻿using LiteNetLib.Utils;
using System;

namespace DAGServer
{
    public class Logger
    {
        public static void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + DateTime.Now + "]: " + message);
            Console.ResetColor();
        }

        public static void UserFriendlyInfo(string message)
        {
            if (!Server.ReadablePacketInfo)
                return;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[" + DateTime.Now + "]: " + message);
            Console.ResetColor();
        }

        public static void DebugInfo(string message)
        {
            if (!Server.DebugMode)
                return;

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("[" + DateTime.Now + "]: " + message);
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[" + DateTime.Now + "]: " + message);
            Console.ResetColor();
        }

        public static void LogPacketReceieve(ServerPacket.ClientPacketType packetType)
        {
            if (!Server.PacketLogs)
                return;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("[" + DateTime.Now + "]: " + "Received " + packetType + " packet.");
            Console.ResetColor();
        }

        public static void LogPacketSent(NetDataWriter packet)
        {
            if (!Server.PacketLogs)
                return;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("[" + DateTime.Now + "]: " + "Sent " + (ServerPacket.ServerPacketType)packet.Data[0] + " packet.");
            Console.ResetColor();
        }
    }
}
