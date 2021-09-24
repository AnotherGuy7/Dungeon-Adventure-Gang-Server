using DAGServer.Data;
using Lidgren.Network;
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
            config.MaximumConnections = 4;
            mainServer = new NetServer(config);
            mainServer.Start();
            Logger.Info("Server created at: " + NetworkIP + " (Port: " + NetworkPort + ")");
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

                case ServerPacket.ClientPacketType.SendEnemyVariableData:
                    HandleSentEnemyVariableData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendProjectileVariableData:
                    HandleSentProjectileVariableData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendPlayerUsedItem:
                    HandlePlayerItemUsage(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendDoneLoading:
                    HandleReceivedDoneLoading(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendAllPlayerSpawnData:
                    HandleReceivedPlayerSpawnData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendPlayerState:
                    HandleReceivedPlayerState(message, sender);
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
            /*worldDataMessage.Data = message.Data;
            worldDataMessage.Data[0] = (byte)ServerPacket.ServerPacketType.SendWorldArrayToAll;*/       //Doesn't work for some reason

            /*worldDataMessage.Data = new byte[message.Data.Length];
            message.Data.CopyTo(worldDataMessage.Data, 0);
            worldDataMessage.Data[0] = (byte)ServerPacket.ServerPacketType.SendWorldArrayToAll;
            worldDataMessage.Data[1] = Convert.ToByte(sender); */

             worldDataMessage.Write((byte)ServerPacket.ServerPacketType.SendWorldArrayToAll);
             worldDataMessage.Write(sender);
             for (int x = 0; x < 400; x++)
             {
                 for (int y = 0; y < 400; y++)
                 {
                     byte tileType = message.ReadByte();
                     byte textureType = message.ReadByte();

                     worldDataMessage.Write(tileType);
                     worldDataMessage.Write(textureType);
                 }
             }

             SendMessageToAllOthers(worldDataMessage, message.SenderConnection);
        }

        public void HandleNewObjectInfo(NetIncomingMessage message, int sender)
        {
            int bodyType = message.ReadInt32();
            int objectType = message.ReadInt32();
            int objectIndex = message.ReadInt32();
            float posX = message.ReadFloat();
            float posY = message.ReadFloat();
            /*int objectInfoLength = 0;
            byte[] objectInfo = null;
            int objectExtraInfoLength = 0;
            byte[] objectExtraInfo = null;
            if (bodyType == 1)
            {
                objectInfoLength = message.ReadInt32();
                objectInfo = message.ReadBytes(objectInfoLength);
                objectExtraInfoLength = message.ReadInt32();
                objectExtraInfo = message.ReadBytes(objectExtraInfoLength);
            }*/

            NetOutgoingMessage newObjectDataMessage = mainServer.CreateMessage();
            newObjectDataMessage.Write((byte)ServerPacket.ServerPacketType.SendNewObjectInfo);
            newObjectDataMessage.Write(sender);
            newObjectDataMessage.Write(bodyType);
            newObjectDataMessage.Write(objectType);
            newObjectDataMessage.Write(objectIndex);
            newObjectDataMessage.Write(posX);
            newObjectDataMessage.Write(posY);
            /*if (bodyType == 1)
            {
                message.Write(objectInfoLength);
                message.Write(objectInfo);
                message.Write(objectExtraInfoLength);
                message.Write(objectExtraInfo);
            }*/

            SendMessageToAllOthers(newObjectDataMessage, message.SenderConnection);
        }

        public void HandleSentEnemyVariableData(NetIncomingMessage message, int sender)
        {
            int objectIndex = message.ReadInt32();
            byte variableIndex = message.ReadByte();
            int value1 = message.ReadInt32();
            int value2 = message.ReadInt32();
            int value3 = message.ReadInt32();

            NetOutgoingMessage enemyDataMessage = mainServer.CreateMessage();
            enemyDataMessage.Write((byte)ServerPacket.ServerPacketType.SendProjectileVariableData);
            enemyDataMessage.Write(sender);
            enemyDataMessage.Write(objectIndex);
            enemyDataMessage.Write(variableIndex);
            enemyDataMessage.Write(value1);
            enemyDataMessage.Write(value2);
            enemyDataMessage.Write(value3);

            SendMessageToAllOthers(enemyDataMessage, message.SenderConnection);
        }

        public void HandleSentProjectileVariableData(NetIncomingMessage message, int sender)
        {
            int objectIndex = message.ReadInt32();
            byte variableIndex = message.ReadByte();
            int value1 = message.ReadInt32();
            int value2 = message.ReadInt32();
            int value3 = message.ReadInt32();

            NetOutgoingMessage projectileDataMessage = mainServer.CreateMessage();
            projectileDataMessage.Write((byte)ServerPacket.ServerPacketType.SendProjectileVariableData);
            projectileDataMessage.Write(sender);
            projectileDataMessage.Write(objectIndex);
            projectileDataMessage.Write(variableIndex);
            projectileDataMessage.Write(value1);
            projectileDataMessage.Write(value2);
            projectileDataMessage.Write(value3);

            SendMessageToAllOthers(projectileDataMessage, message.SenderConnection);
        }

        public void HandlePlayerItemUsage(NetIncomingMessage message, int sender)
        {
            byte itemType = message.ReadByte();
            int emulatedMousePosX = message.ReadInt32();
            int emulatedMousePosY = message.ReadInt32();
            bool secondaryUse = message.ReadBoolean();

            NetOutgoingMessage itemUsageMessage = mainServer.CreateMessage();
            itemUsageMessage.Write((byte)ServerPacket.ServerPacketType.SendPlayerUsedItem);
            itemUsageMessage.Write(sender);
            itemUsageMessage.Write(itemType);
            itemUsageMessage.Write(emulatedMousePosX);
            itemUsageMessage.Write(emulatedMousePosY);
            itemUsageMessage.Write(secondaryUse);

            SendMessageToAllOthers(itemUsageMessage, message.SenderConnection);
        }

        public void HandleReceivedDoneLoading(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage doneLoadingMessage = mainServer.CreateMessage();
            doneLoadingMessage.Write((byte)ServerPacket.ServerPacketType.SendOtherPlayerDoneLoading);
            doneLoadingMessage.Write(sender);

            SendMessageToAllOthers(doneLoadingMessage, message.SenderConnection);
        }

        public void HandleReceivedPlayerSpawnData(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage spawnDataMessage = mainServer.CreateMessage();
            spawnDataMessage.Write((byte)ServerPacket.ServerPacketType.SendAllPlayerSpawnDataToOthers);
            spawnDataMessage.Write(sender);

            int amountOfPlayers = message.ReadInt32();
            spawnDataMessage.Write(amountOfPlayers);

            for (int i = 0; i < amountOfPlayers; i++)
            {
                float playerPosX = message.ReadFloat();
                float playerPosY = message.ReadFloat();

                spawnDataMessage.Write(playerPosX);
                spawnDataMessage.Write(playerPosY);
            }

            SendMessageToAllOthers(spawnDataMessage, message.SenderConnection);
        }

        public void HandleReceivedPlayerState(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage playerStateMessage = mainServer.CreateMessage();
            playerStateMessage.Write((byte)ServerPacket.ServerPacketType.SendOtherPlayerState);
            playerStateMessage.Write(sender);

            byte playerState = message.ReadByte();
            playerStateMessage.Write(playerState);

            SendMessageToAllOthers(playerStateMessage, message.SenderConnection);
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