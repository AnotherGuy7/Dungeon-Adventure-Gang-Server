using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DAGServer
{
    public class Server
    {
        public const string ConfigurationApplicationName = "Dungeon Adventure Gang";
        public const string NetworkIP = "127.0.0.1";
        public const int NetworkPort = 11223;
        public const int MaximumLobbySize = 4;

        public static NetServer mainServer;
        public static Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();

        public static void CreateNewServer()
        {
            Logger.Info("Creating Server...");
            NetPeerConfiguration config = new NetPeerConfiguration(ConfigurationApplicationName);
            config.Port = NetworkPort;
            mainServer = new NetServer(config);
            mainServer.Start();
            Logger.Info("Server created.");
        }

        public static void SearchForMessages(NetPeer peer)
        {
            NetIncomingMessage message = peer.ReadMessage();

            if (message == null)
                return;

            switch (message.MessageType)
            {
                case NetIncomingMessageType.StatusChanged:
                    if (message.SenderConnection.Status == NetConnectionStatus.Connected)
                        Logger.Info("New connection.");
                    else if (message.SenderConnection.Status == NetConnectionStatus.Disconnected)
                        Logger.Error("A connection has disconnected.");
                    break;
                case NetIncomingMessageType.DebugMessage:
                    Logger.Info("Debug Packet received: " + message.ReadString());
                    break;
                case NetIncomingMessageType.Data:
                    HandleDataMessages(message);
                    break;
                case NetIncomingMessageType.WarningMessage:
                    Logger.Error("Warning Packet received: " + message.ReadString());
                    break;
                default:
                    Logger.Error("Unknown packet type has arrived: " + message.MessageType);
                    break;
            }
        }

        public static void HandleDataMessages(NetIncomingMessage message)
        {
            ServerPacket.ClientPacketType messageDataType = (ServerPacket.ClientPacketType)message.ReadByte();
            int sender = message.ReadInt32();
            Logger.Info(messageDataType.ToString());

            switch (messageDataType)
            {
                case ServerPacket.ClientPacketType.RequestID:       //Gives the peer who requested it an ID
                    HandleIDRequest(message, sender);
                    break;

                case ServerPacket.ClientPacketType.RequestAllPlayerData:        //Returns the data of all current players in the game.
                    HandleAllPlayersDataRequest(message, sender);
                    break;

                case ServerPacket.ClientPacketType.RequestPlayerDataDeletion:
                    HandlePlayerDataDeletionRequest(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendClientPosition:      //Sends the peer's position to all peers that aren't the sender
                    HandleClientPositionInformation(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendPlayerInfo:      //Send the peer's player information to all peers that aren't the sender
                    HandlePlayerInfo(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendSound:
                    HandleSentSoundData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendStringMessageToOtherPlayers:
                    HandleSentMessage(message, sender);
                    break;
            }
        }

        public static void HandleIDRequest(NetIncomingMessage message, int sender)
        {
            int givenID = playerData.Count + 1;

            NetOutgoingMessage clientIDMessage = mainServer.CreateMessage();
            clientIDMessage.Write((byte)ServerPacket.ServerPacketType.GiveID);
            clientIDMessage.Write(sender);
            clientIDMessage.Write(givenID);

            mainServer.SendMessage(clientIDMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public static void HandleAllPlayersDataRequest(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage playerDataMessage = mainServer.CreateMessage();
            playerDataMessage.Write((byte)ServerPacket.ServerPacketType.GiveAllPlayerData);
            playerDataMessage.Write(sender);
            playerDataMessage.Write(playerData.Count);

            PlayerData[] playerDataArray = playerData.Values.ToArray();
            for (int i = 0; i < playerDataArray.Length; i++)        //The data has to be read by index cause we don't know how many players there are
            {
                playerDataMessage.Write(playerDataArray[i].playerID);
                playerDataMessage.Write(playerDataArray[i].name);
                playerDataMessage.Write(playerDataArray[i].health);
            }

            mainServer.SendMessage(playerDataMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public static void HandlePlayerDataDeletionRequest(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage playerDataDeletionMessage = mainServer.CreateMessage();
            playerDataDeletionMessage.Write((byte)ServerPacket.ServerPacketType.GivePlayerDataDeletion);
            playerDataDeletionMessage.Write(sender);

            playerData.Remove(sender);
            Dictionary<int, PlayerData> temporaryPlayersDict = playerData;
            playerData.Clear();
            for (int i = 0; i < temporaryPlayersDict.Count; i++)
            {
                if (i >= sender)
                {
                    playerData.Add(i, temporaryPlayersDict[i + 1]);
                }
                else
                {
                    playerData.Add(i, temporaryPlayersDict[i]);
                }
            }

            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(message.SenderConnection);
            if (otherConnectionsList.Count > 0)
                mainServer.SendMessage(playerDataDeletionMessage, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void HandleClientPositionInformation(NetIncomingMessage message, int sender)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            int direction = message.ReadInt32();

            NetOutgoingMessage playerPositionMessage = mainServer.CreateMessage();
            playerPositionMessage.Write((byte)ServerPacket.ServerPacketType.GiveClientPositon);
            playerPositionMessage.Write(sender);
            playerPositionMessage.Write(x);
            playerPositionMessage.Write(y);
            playerPositionMessage.Write(direction);

            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(message.SenderConnection);
            if (otherConnectionsList.Count > 0)
                mainServer.SendMessage(playerPositionMessage, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void HandlePlayerInfo(NetIncomingMessage message, int sender)
        {
            PlayerData newPlayerData = new PlayerData();
            string playerName = message.ReadString();
            int playerHealth = message.ReadInt32();
            int playerID = message.ReadInt32();

            playerData.Add(playerID, newPlayerData);
            playerData[playerID].name = playerName;
            playerData[playerID].health = playerHealth;
            playerData[playerID].playerID = playerID;


            if (mainServer.Connections.Count < 2)
                return;


            NetOutgoingMessage playerInfoMessage = mainServer.CreateMessage();
            playerInfoMessage.Write((byte)ServerPacket.ServerPacketType.GivePlayerInfo);
            playerInfoMessage.Write(sender);
            playerInfoMessage.Write(playerName);
            playerInfoMessage.Write(playerHealth);
            playerInfoMessage.Write(playerID);

            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(message.SenderConnection);
            if (otherConnectionsList.Count >= 1)
            {
                mainServer.SendMessage(playerInfoMessage, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public static void HandleSentSoundData(NetIncomingMessage message, int sender)
        {
            int soundType = message.ReadInt32();
            float soundPosX = message.ReadFloat();
            float soundPosY = message.ReadFloat();
            float soundTravelDistance = message.ReadFloat();

            NetOutgoingMessage soundInfoMessage = mainServer.CreateMessage();
            soundInfoMessage.Write((byte)ServerPacket.ServerPacketType.PlaySound);
            soundInfoMessage.Write(sender);
            soundInfoMessage.Write(soundType);
            soundInfoMessage.Write(soundPosX);
            soundInfoMessage.Write(soundPosY);
            soundInfoMessage.Write(soundTravelDistance);

            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(message.SenderConnection);
            if (otherConnectionsList.Count >= 1)
            {
                mainServer.SendMessage(soundInfoMessage, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public static void HandleSentMessage(NetIncomingMessage message, int sender)
        {
            string playerMessage = message.ReadString();

            NetOutgoingMessage soundInfoMessage = mainServer.CreateMessage();
            soundInfoMessage.Write((byte)ServerPacket.ServerPacketType.GiveStringMessageToOtherPlayers);
            soundInfoMessage.Write(sender);
            soundInfoMessage.Write(playerMessage);

            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(message.SenderConnection);
            if (otherConnectionsList.Count >= 1)
            {
                mainServer.SendMessage(soundInfoMessage, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public static void HandleWorldData(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage worldDataMessage = mainServer.CreateMessage();
            worldDataMessage.Data = message.Data;
            worldDataMessage.Data[0] = ((byte)ServerPacket.ServerPacketType.SendWorldArrayToAll);

            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(message.SenderConnection);
            if (otherConnectionsList.Count >= 1)
            {
                mainServer.SendMessage(worldDataMessage, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }
    }
}