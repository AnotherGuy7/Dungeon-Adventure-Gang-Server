using LiteNetLib.Utils;
using System;
using System.IO;

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

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[" + DateTime.Now + "]: " + "Received " + packetType + " packet.");
            Console.ResetColor();
        }

        public static void LogPacketSent(NetDataWriter packet)
        {
            if (!Server.PacketLogs)
                return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[" + DateTime.Now + "]: " + "Sent " + (ServerPacket.ServerPacketType)packet.Data[0] + " packet.");
            Console.ResetColor();
        }

        public static void LogCustomMessage(string message, ConsoleColor color = ConsoleColor.White, bool showDateTime = false)
        {
            string output = "";
            if (showDateTime)
                output = "[" + DateTime.Now + "]: ";
            output += message;

            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void CreateFileLogs(Exception exception)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Dungeon_Adventure_Gang\\Error Logs\\ServerLog_" + DateTime.Now.Month + "_" + DateTime.Today.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + ".txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            FileStream fileStream = File.OpenWrite(path);
            StreamWriter writer = new StreamWriter(fileStream);
            writer.WriteLine(exception.Message);
            writer.WriteLine(exception.StackTrace);
            writer.WriteLine(exception.Source);
            writer.Close();
            fileStream.Close();
        }
    }
}
