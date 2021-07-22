using DAGServer.Data;
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
        public static Dictionary<int, ClientData> clientData = new Dictionary<int, ClientData>();
        public static Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();

        public void CreateNewServer()
        {
            Logger.Info("Creating Server...");
            NetPeerConfiguration config = new NetPeerConfiguration(ConfigurationApplicationName);
            config.Port = NetworkPort;
            mainServer = new NetServer(config);
            mainServer.Start();
            Logger.Info("Server created.");
        }

        public void SearchForMessages(NetPeer peer)
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

        public void HandleDataMessages(NetIncomingMessage message)
        {
            ServerPacket.ClientPacketType messageDataType = (ServerPacket.ClientPacketType)message.ReadByte();
            int sender = message.ReadInt32();
            Logger.Info(messageDataType.ToString());

            switch (messageDataType)
            {
                case ServerPacket.ClientPacketType.RequestID:       //Gives the peer who requested it an ID
                    HandleIDRequest(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendClientInfo:
                    HandleNewClientInfo(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendClientCharacterType:
                    HandleClientCharacterType(message, sender);
                    break;

                case ServerPacket.ClientPacketType.RequestAllClientData:
                    HandleAllClientsDataRequest(message, sender);
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

                /*case ServerPacket.ClientPacketType.SendPlayerInfo:      //Send the peer's player information to all peers that aren't the sender
                    HandlePlayerInfo(message, sender);
                    break;*/

                case ServerPacket.ClientPacketType.SendSound:
                    HandleSentSoundData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendStringMessageToOtherPlayers:
                    HandleSentMessage(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendWorldArray:
                    HandleWorldData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendNewObjectInfo:
                    HandleNewObjectInfo(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendObjectPosition:
                    HandleSentObjectPosition(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendObjectData:
                    HandleSentObjectData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendAllPlayerSpawnData:
                    HandleReceivedPlayerSpawnData(message, sender);
                    break;
            }
        }

        public void HandleIDRequest(NetIncomingMessage message, int sender)
        {
            int givenID = clientData.Count + 1;

            NetOutgoingMessage clientIDMessage = mainServer.CreateMessage();
            clientIDMessage.Write((byte)ServerPacket.ServerPacketType.GiveID);
            clientIDMessage.Write(sender);
            clientIDMessage.Write(givenID);

            SendMessageBackToSender(clientIDMessage, message.SenderConnection);
        }

        public void HandleNewClientInfo(NetIncomingMessage message, int sender)
        {
            int clientID = sender;
            string clientName = message.ReadString();

            ClientData newClientData = new ClientData();
            newClientData.clientID = clientID;
            newClientData.clientName = clientName;
            clientData.Add(clientID, newClientData);


            if (mainServer.Connections.Count < 2)
                return;


            NetOutgoingMessage clientInfoMessage = mainServer.CreateMessage();
            clientInfoMessage.Write((byte)ServerPacket.ServerPacketType.GiveClientInfo);
            clientInfoMessage.Write(sender);
            clientInfoMessage.Write(clientID);
            clientInfoMessage.Write(clientName);

            SendMessageToAllOthers(clientInfoMessage, message.SenderConnection);
        }

        public void HandleClientCharacterType(NetIncomingMessage message, int sender)
        {
            int clientID = sender;
            int clientCharacterType = message.ReadInt32();

            clientData[clientID].chosenCharacterType = clientCharacterType;


            if (mainServer.Connections.Count < 2)
                return;


            NetOutgoingMessage clientCharacterTypeMessage = mainServer.CreateMessage();
            clientCharacterTypeMessage.Write((byte)ServerPacket.ServerPacketType.GiveClientCharacterType);
            clientCharacterTypeMessage.Write(sender);
            clientCharacterTypeMessage.Write(clientID);
            clientCharacterTypeMessage.Write(clientCharacterType);

            SendMessageToAllOthers(clientCharacterTypeMessage, message.SenderConnection);
        }

        public void HandleAllClientsDataRequest(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage clientDataMessage = mainServer.CreateMessage();
            clientDataMessage.Write((byte)ServerPacket.ServerPacketType.GiveAllClientData);
            clientDataMessage.Write(sender);
            clientDataMessage.Write(clientData.Count);

            ClientData[] clientDataArray = clientData.Values.ToArray();
            for (int i = 0; i < clientDataArray.Length; i++)        //The data has to be read by index cause we don't know how many players there are
            {
                clientDataMessage.Write(clientDataArray[i].clientID);
                clientDataMessage.Write(clientDataArray[i].clientName);
            }

            SendMessageBackToSender(clientDataMessage, message.SenderConnection);
        }

        public void HandleAllPlayersDataRequest(NetIncomingMessage message, int sender)
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

            SendMessageBackToSender(playerDataMessage, message.SenderConnection);
        }

        public void HandlePlayerDataDeletionRequest(NetIncomingMessage message, int sender)
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

            SendMessageToAllOthers(playerDataDeletionMessage, message.SenderConnection);
        }

        public void HandleClientPositionInformation(NetIncomingMessage message, int sender)
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

            SendMessageToAllOthers(playerPositionMessage, message.SenderConnection);
        }

        /*public void HandlePlayerInfo(NetIncomingMessage message, int sender)
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

            SendMessageToAllOthers(playerInfoMessage, message.SenderConnection);
        }*/

        public void HandleSentSoundData(NetIncomingMessage message, int sender)
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

            SendMessageToAllOthers(soundInfoMessage, message.SenderConnection);

        }

        public void HandleSentMessage(NetIncomingMessage message, int sender)
        {
            string playerMessage = message.ReadString();

            NetOutgoingMessage stringMessage = mainServer.CreateMessage();
            stringMessage.Write((byte)ServerPacket.ServerPacketType.GiveStringMessageToOtherPlayers);
            stringMessage.Write(sender);
            stringMessage.Write(playerMessage);

            SendMessageToAllOthers(stringMessage, message.SenderConnection);
        }

        public void HandleWorldData(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage worldDataMessage = mainServer.CreateMessage();
            worldDataMessage.Data = message.Data;
            worldDataMessage.Data[0] = ((byte)ServerPacket.ServerPacketType.SendWorldArrayToAll);

            SendMessageToAllOthers(worldDataMessage, message.SenderConnection);
        }

        public void HandleNewObjectInfo(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage newObjectDataMessage = mainServer.CreateMessage();
            newObjectDataMessage.Data = message.Data;
            newObjectDataMessage.Data[0] = ((byte)ServerPacket.ServerPacketType.SendNewObjectInfo);

            SendMessageToAllOthers(newObjectDataMessage, message.SenderConnection);
        }

        public void HandleSentObjectPosition(NetIncomingMessage message, int sender)
        {
            int objectIndex = message.ReadInt32();
            float posX = message.ReadFloat();
            float posY = message.ReadFloat();

            NetOutgoingMessage objectPositionMessage = mainServer.CreateMessage();
            objectPositionMessage.Write((byte)ServerPacket.ServerPacketType.SendObjectPosition);
            objectPositionMessage.Write(sender);
            objectPositionMessage.Write(objectIndex);
            objectPositionMessage.Write(posX);
            objectPositionMessage.Write(posY);

            SendMessageToAllOthers(objectPositionMessage, message.SenderConnection);
        }

        public void HandleSentObjectData(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage objectDataMessage = mainServer.CreateMessage();
            objectDataMessage.Data = message.Data;
            objectDataMessage.Data[0] = ((byte)ServerPacket.ServerPacketType.SendObjectData);

            SendMessageToAllOthers(objectDataMessage, message.SenderConnection);
        }

        public void HandleReceivedPlayerSpawnData(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage spawnDataMessage = mainServer.CreateMessage();
            spawnDataMessage.Data = message.Data;
            spawnDataMessage.Data[0] = (byte)ServerPacket.ServerPacketType.SendAllPlayerSpawnDataToOthers;

            SendMessageToAllOthers(spawnDataMessage, message.SenderConnection);
        }

        public static void SendMessageToAllOthers(NetOutgoingMessage message, NetConnection senderConnection)
        {
            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(senderConnection);
            if (otherConnectionsList.Count >= 1)
            {
                mainServer.SendMessage(message, otherConnectionsList, NetDeliveryMethod.ReliableOrdered, 0);
            }
        }

        public static void SendMessageBackToSender(NetOutgoingMessage message, NetConnection senderConnection)
        {
            mainServer.SendMessage(message, senderConnection, NetDeliveryMethod.ReliableOrdered);
        }
    }
}