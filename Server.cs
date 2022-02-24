using Lidgren.Network;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using static DAGServer.ServerData;

namespace DAGServer
{
    public class Server
    {
        public const string ConfigurationApplicationName = "Dungeon Adventure Gang";
        public const string NetworkIP = "127.0.0.1";
        public const int NetworkPort = 11223;
        public const int MaximumLobbySize = 4;
        public const bool DebugMode = true;     //Writes out all incoming and outgoing packets.
        public const bool ReadablePacketInfo = false;       //Writes out messages that normal people can understand.

        public static NetServer mainServer;
        public static Dictionary<int, ClientData> clientData = new Dictionary<int, ClientData>();
        public static Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();

        public static int[] dungeonEnemies = new int[255];
        public static int[] gameProjectileExists = new int[1000];

        public void CreateNewServer()
        {
            Logger.Info("Creating Server...");
            NetPeerConfiguration config = new NetPeerConfiguration(ConfigurationApplicationName)
            {
                Port = NetworkPort,
                MaximumConnections = 4,
            };

            string hostName = Dns.GetHostName(); // Retrive the Name of HOST
            string myIP = Dns.GetHostEntry(hostName).AddressList[0].MapToIPv4().ToString() + " or " + Dns.GetHostEntry(hostName).AddressList[1].MapToIPv4().ToString();
            /*
            string routerIP = Dns.GetHostEntry(hostName).AddressList[0].ToString()
            string localIP = Dns.GetHostEntry(hostName).AddressList[1].ToString()
            */

            mainServer = new NetServer(config);
            mainServer.Start();
            Logger.Info("Server created at: " + myIP + " (Port: " + NetworkPort + ")");
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
                    Logger.DebugInfo("Debug Packet received: " + message.ReadString());
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
            Logger.DebugInfo(messageDataType.ToString());

            switch (messageDataType)
            {
                case ServerPacket.ClientPacketType.SendPing:
                    HandleReceivedPingPacket(message, sender);
                    break;

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

                /*case ServerPacket.ClientPacketType.RequestAllPlayerData:        //Returns the data of all current players in the game.
                    HandleAllPlayersDataRequest(message, sender);
                    break;*/

                case ServerPacket.ClientPacketType.RequestPlayerDataDeletion:
                    HandlePlayerDataDeletionRequest(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendMovementInformation:      //Sends the peer's position to all peers that aren't the sender
                    HandleClientMovementInformation(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendPlayerVariableData:
                    HandleReceivedPlayerVariableData(message, sender);
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

                case ServerPacket.ClientPacketType.SendNewEnemyInfo:
                    HandleNewEnemyInfo(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendEnemyVariableData:
                    HandleSentEnemyVariableData(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendNewProjectileInfo:
                    HandleNewProjectileInfo(message, sender);
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

                case ServerPacket.ClientPacketType.SendNewItemCreation:
                    HandleReceivedItemCreation(message, sender);
                    break;

                case ServerPacket.ClientPacketType.SendEnemyListForSync:
                    HandleReceivedEnemyListSync(message, sender);
                    break;
            }
        }

        public void HandleReceivedPingPacket(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage pingMessage = mainServer.CreateMessage();
            pingMessage.Write((byte)ServerPacket.ServerPacketType.GivePing);
            pingMessage.Write(sender);
            pingMessage.Write(0);

            SendMessageBackToSender(pingMessage, message.SenderConnection);
        }

        public void HandleIDRequest(NetIncomingMessage message, int sender)
        {
            int givenID = clientData.Count + 1;

            NetOutgoingMessage clientIDMessage = mainServer.CreateMessage();
            clientIDMessage.Write((byte)ServerPacket.ServerPacketType.GiveID);
            clientIDMessage.Write(sender);
            clientIDMessage.Write(givenID);

            SendMessageBackToSender(clientIDMessage, message.SenderConnection);
            Logger.UserFriendlyInfo("Assigned ID of " + givenID + " to a newly connected client.");
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

            ClientData clientDataClone = clientData[clientID];
            clientDataClone.chosenCharacterType = clientCharacterType;
            clientData[clientID] = clientDataClone;

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
                clientDataMessage.Write((byte)clientDataArray[i].clientID);
                clientDataMessage.Write(clientDataArray[i].clientName);
                clientDataMessage.Write((byte)clientDataArray[i].chosenCharacterType);
            }

            SendMessageBackToSender(clientDataMessage, message.SenderConnection);
        }

        /*public void HandleAllPlayersDataRequest(NetIncomingMessage message, int sender)
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
        }*/

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
            Logger.UserFriendlyInfo("A client has been removed from the game.");
        }

        public void HandleClientMovementInformation(NetIncomingMessage message, int sender)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            float velX = message.ReadFloat();
            float velY = message.ReadFloat();
            int direction = message.ReadInt32();

            NetOutgoingMessage playerMovementMessage = mainServer.CreateMessage();
            playerMovementMessage.Write((byte)ServerPacket.ServerPacketType.GiveClientMovementInformation);
            playerMovementMessage.Write(sender);
            playerMovementMessage.Write(x);
            playerMovementMessage.Write(y);
            playerMovementMessage.Write(velX);
            playerMovementMessage.Write(velY);
            playerMovementMessage.Write(direction);

            SendMessageToAllOthers(playerMovementMessage, message.SenderConnection, NetDeliveryMethod.Unreliable);
        }

        public void HandleReceivedPlayerVariableData(NetIncomingMessage message, int sender)
        {
            byte variableIndex = message.ReadByte();
            int value1 = message.ReadInt32();
            int value2 = message.ReadInt32();
            int value3 = message.ReadInt32();

            NetOutgoingMessage enemyDataMessage = mainServer.CreateMessage();
            enemyDataMessage.Write((byte)ServerPacket.ServerPacketType.SendPlayerVariableData);
            enemyDataMessage.Write(sender);
            enemyDataMessage.Write(variableIndex);
            enemyDataMessage.Write(value1);
            enemyDataMessage.Write(value2);
            enemyDataMessage.Write(value3);

            SendMessageToAllOthers(enemyDataMessage, message.SenderConnection);
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
            float soundPitch = message.ReadFloat();
            float soundVolume = message.ReadFloat();

            NetOutgoingMessage soundInfoMessage = mainServer.CreateMessage();
            soundInfoMessage.Write((byte)ServerPacket.ServerPacketType.PlaySound);
            soundInfoMessage.Write(sender);
            soundInfoMessage.Write(soundType);
            soundInfoMessage.Write(soundPosX);
            soundInfoMessage.Write(soundPosY);
            soundInfoMessage.Write(soundPitch);
            soundInfoMessage.Write(soundVolume);
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
            Logger.UserFriendlyInfo("Player " + sender + ": " + playerMessage);
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

            int width = message.ReadInt32();
            int height = message.ReadInt32();
            worldDataMessage.Write(width);
            worldDataMessage.Write(height);

             for (int x = 0; x < width; x++)
             {
                 for (int y = 0; y < height; y++)
                 {
                     byte tileType = message.ReadByte();
                     byte textureType = message.ReadByte();

                     worldDataMessage.Write(tileType);
                     worldDataMessage.Write(textureType);
                 }
             }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte objType = message.ReadByte();
                    worldDataMessage.Write(objType);
                    if (objType == 0)
                        continue;

                    int posX = message.ReadInt32();
                    int posY = message.ReadInt32();
                    byte info1 = message.ReadByte();
                    byte info2 = message.ReadByte();

                    worldDataMessage.Write(posX);
                    worldDataMessage.Write(posY);
                    worldDataMessage.Write(info1);
                    worldDataMessage.Write(info2);
                }
            }

            SendMessageToAllOthers(worldDataMessage, message.SenderConnection);
            Logger.UserFriendlyInfo("Created World of [" + width + ", " + height + "].");
        }

        public void HandleNewEnemyInfo(NetIncomingMessage message, int sender)
        {
            byte enemyType = message.ReadByte();
            float posX = message.ReadFloat();
            float posY = message.ReadFloat();
            byte enemyIndex = message.ReadByte();

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

            NetOutgoingMessage newEnemyDataMessage = mainServer.CreateMessage();
            newEnemyDataMessage.Write((byte)ServerPacket.ServerPacketType.SendNewEnemyInfo);
            newEnemyDataMessage.Write(sender);
            newEnemyDataMessage.Write(enemyType);
            newEnemyDataMessage.Write(posX);
            newEnemyDataMessage.Write(posY);
            newEnemyDataMessage.Write(enemyIndex);
            /*if (bodyType == 1)
            {
                message.Write(objectInfoLength);
                message.Write(objectInfo);
                message.Write(objectExtraInfoLength);
                message.Write(objectExtraInfo);
            }*/

            SendMessageToAllOthers(newEnemyDataMessage, message.SenderConnection);
        }

        public void HandleSentEnemyVariableData(NetIncomingMessage message, int sender)
        {
            int objectIndex = message.ReadInt32();
            byte variableIndex = message.ReadByte();
            int value1 = message.ReadInt32();
            int value2 = message.ReadInt32();
            int value3 = message.ReadInt32();

            NetOutgoingMessage enemyDataMessage = mainServer.CreateMessage();
            enemyDataMessage.Write((byte)ServerPacket.ServerPacketType.SendEnemyVariableData);
            enemyDataMessage.Write(sender);
            enemyDataMessage.Write(objectIndex);
            enemyDataMessage.Write(variableIndex);
            enemyDataMessage.Write(value1);
            enemyDataMessage.Write(value2);
            enemyDataMessage.Write(value3);

            SendMessageToAllOthers(enemyDataMessage, message.SenderConnection);
        }

        public void HandleNewProjectileInfo(NetIncomingMessage message, int sender)
        {
            byte type = message.ReadByte();
            int posX = message.ReadInt32();
            int posY = message.ReadInt32();
            float velX = message.ReadInt32();
            float velY = message.ReadInt32();
            byte owner = message.ReadByte();
            int index = message.ReadInt32();
            int info1 = message.ReadInt32();
            int info2 = message.ReadInt32();
            int info3 = message.ReadInt32();

            NetOutgoingMessage newProjectileDataMessage = mainServer.CreateMessage();
            newProjectileDataMessage.Write((byte)ServerPacket.ServerPacketType.SendNewProjectileInfo);
            newProjectileDataMessage.Write(sender);
            newProjectileDataMessage.Write(type);
            newProjectileDataMessage.Write(posX);
            newProjectileDataMessage.Write(posY);
            newProjectileDataMessage.Write(velX);
            newProjectileDataMessage.Write(velY);

            newProjectileDataMessage.Write(owner);
            newProjectileDataMessage.Write(index);
            newProjectileDataMessage.Write(info1);
            newProjectileDataMessage.Write(info2);
            newProjectileDataMessage.Write(info3);

            SendMessageToAllOthers(newProjectileDataMessage, message.SenderConnection);
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

        public void HandleReceivedItemCreation(NetIncomingMessage message, int sender)
        {
            byte itemType = message.ReadByte();
            int xPos = message.ReadInt32();
            int yPos = message.ReadInt32();

            NetOutgoingMessage itemCreationMessage = mainServer.CreateMessage();
            itemCreationMessage.Write((byte)ServerPacket.ServerPacketType.SendNewItemCreation);
            itemCreationMessage.Write(sender);
            itemCreationMessage.Write(itemType);
            itemCreationMessage.Write(xPos);
            itemCreationMessage.Write(yPos);

            SendMessageToAllOthers(itemCreationMessage, message.SenderConnection);
        }

        public void HandleReceivedEnemyListSync(NetIncomingMessage message, int sender)
        {
            NetOutgoingMessage enemySyncMessage = mainServer.CreateMessage();
            enemySyncMessage.Write((byte)ServerPacket.ServerPacketType.ReceiveEnemyListSync);
            enemySyncMessage.Write(sender);

            byte amountOfEnemies = message.ReadByte();
            message.Write(amountOfEnemies);

            dungeonEnemies = new int[amountOfEnemies];
            for (int i = 0; i < amountOfEnemies; i++)
            {
                byte enemyType = message.ReadByte();
                int health = message.ReadInt32();
                int posX = message.ReadInt32();
                int posY = message.ReadInt32();
                dungeonEnemies[i] = enemyType;

                message.Write(enemyType);
                message.Write(health);
                message.Write(posX);
                message.Write(posY);
            }

            SendMessageToAllOthers(enemySyncMessage, message.SenderConnection);
        }

        public static void SendMessageToAllOthers(NetOutgoingMessage message, NetConnection senderConnection, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.ReliableOrdered)       //Data sending
        {
            List<NetConnection> otherConnectionsList = mainServer.Connections;
            otherConnectionsList.Remove(senderConnection);

            if (otherConnectionsList.Count >= 1)
                mainServer.SendMessage(message, otherConnectionsList, deliveryMethod, 0);
        }

        public static void SendMessageBackToSender(NetOutgoingMessage message, NetConnection senderConnection, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.ReliableOrdered)      //Data retrieval
        {
            mainServer.SendMessage(message, senderConnection, deliveryMethod);
        }
    }
}