using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using static DAGServer.ServerData;

namespace DAGServer
{
    public class Server
    {
        public const string ConfigurationApplicationName = "Dungeon Adventure Gang";
        public const string NetworkIP = "127.0.0.1";
        public const int NetworkPort = 11223;
        public const int MaximumLobbySize = 4;
        public const bool DebugMode = false;     //Writes out all incoming and outgoing packets.
        public const bool ReadablePacketInfo = true;       //Writes out messages that normal people can understand.
        public const bool PacketLogs = true;

        public static NetPeer serverPeer;
        public static NetManager serverManager;
        public static EventBasedNetListener serverListener;
        public static Dictionary<int, ClientData> clientData = new Dictionary<int, ClientData>();
        public static Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();

        public static int amountOfConnectedPlayers = 0;
        public static int[] dungeonEnemies = new int[255];
        public static int[] gameProjectileExists = new int[1000];

        public void CreateNewServer()
        {
            Logger.Info("Creating Server...");

            serverListener = new EventBasedNetListener();
            serverManager = new NetManager(serverListener);
            serverManager.UpdateTime = 15;
            serverManager.NatPunchEnabled = true;

            serverManager.Start(NetworkPort);

            serverListener.NetworkReceiveEvent += HandleDataMessages;
            serverListener.ConnectionRequestEvent += ClientConnectRequest;
            serverListener.PeerConnectedEvent += ClientConnected;
            serverListener.PeerDisconnectedEvent += ClientDisconnected;

            Logger.Info("Server created at: " + GetPublicIp() + " (Port: " + NetworkPort + ")");
        }

        private void ClientDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Logger.Error("Client Disconnected. (Disconnection caused by " + disconnectInfo.Reason + ")");
        }

        public static string GetPublicIp(string serviceUrl = "https://ipinfo.io/ip")            //https://stackoverflow.com/questions/3253701/get-public-external-ip-address 
        {
            string IP = "{ No IP }";
            try
            {
                IP = new WebClient().DownloadString(serviceUrl);
                return IPAddress.Parse(IP).ToString();
            }
            catch
            {
                return NetworkIP;
            }
        }

        public void SearchForMessages()
        {
            serverManager.PollEvents();
        }

        public void ClientConnectRequest(ConnectionRequest request)
        {
            Logger.Info("New Client Connection Request received.");
            if (serverManager.ConnectedPeersCount >= 4)
            {
                Logger.Error("Client Connection Request denied.\n      Reason: Too many clients.");
                request.Reject();
                return;
            }

            request.Accept();
            amountOfConnectedPlayers++;
            Logger.Info("Client Connection Request Accepted.");
        }

        public void ClientConnected(NetPeer peer)       //Upon client connections, we send back lobby data and new client info.
        {
            /*NetDataWriter newClientIDAndLobbyInfo = new NetDataWriter();
            newClientIDAndLobbyInfo.Put((byte)ServerPacket.ServerPacketType.GiveLobbyData);
            newClientIDAndLobbyInfo.Put(0);

            byte newClientID = (byte)amountOfConnectedPlayers;
            newClientIDAndLobbyInfo.Put(newClientID);

            List<ClientData> otherClientData = clientData.Values.ToList();
            if (amountOfConnectedPlayers > 1)
            {
                NetDataWriter clientInfoMessage = new NetDataWriter();
                clientInfoMessage.Put((byte)ServerPacket.ServerPacketType.GiveClientInfo);
                clientInfoMessage.Put(0);
                clientInfoMessage.Put(newClientID);
                clientInfoMessage.Put(clientData[newClientID].clientName);

                SendMessageToAllOthers(clientInfoMessage);
            }
            otherClientData.RemoveAt(newClientID - 1);     //Removing yourself from the lobby stuff

            newClientIDAndLobbyInfo.Put((byte)otherClientData.Count);
            for (int i = 0; i < otherClientData.Count; i++)
            {
                newClientIDAndLobbyInfo.Put((byte)otherClientData[i].clientID);
                newClientIDAndLobbyInfo.Put(otherClientData[i].clientName);
                newClientIDAndLobbyInfo.Put((byte)otherClientData[i].chosenCharacterType);
            }

            SendMessageBackToSender(newClientIDAndLobbyInfo, peer);
            Logger.Info("New Client connected. Assigned ID of: " + newClientID);*/
            Logger.Info("New Client connected.");
        }

        public void HandleDataMessages(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            ServerPacket.ClientPacketType messageDataType = (ServerPacket.ClientPacketType)reader.GetByte();
            int senderID = reader.GetInt();
            Logger.LogPacketReceieve(messageDataType);

            switch (messageDataType)
            {
                case ServerPacket.ClientPacketType.SendPing:
                    HandleReceivedPingPacket(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.RequestID:       //Gives the peer who requested it an ID
                    HandleIDRequest(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendClientInfo:
                    HandleNewClientInfo(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendClientCharacterType:
                    HandleClientCharacterType(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.RequestAllClientData:
                    HandleAllClientsDataRequest(peer, reader, senderID);
                    break;

                /*case ServerPacket.ClientPacketType.RequestAllPlayerData:        //Returns the data of all current players in the game.
                    HandleAllPlayersDataRequest(peer, reader, senderID);
                    break;*/

                case ServerPacket.ClientPacketType.RequestPlayerDataDeletion:
                    HandlePlayerDataDeletionRequest(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendMovementInformation:      //Sends the peer's position to all peers that aren't the sender
                    HandleClientMovementInformation(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendPlayerVariableData:
                    HandleReceivedPlayerVariableData(peer, reader, senderID);
                    break;

                /*case ServerPacket.ClientPacketType.SendPlayerInfo:      //Send the peer's player information to all peers that aren't the sender
                    HandlePlayerInfo(peer, reader, senderID);
                    break;*/

                case ServerPacket.ClientPacketType.SendSound:
                    HandleSentSoundData(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendStringMessageToOtherPlayers:
                    HandleSentMessage(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendWorldArray:
                    HandleWorldData(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendNewEnemyInfo:
                    HandleNewEnemyInfo(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendEnemyVariableData:
                    HandleSentEnemyVariableData(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendEnemyDeletion:
                    HandleSentEnemyDeletion(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendNewProjectileInfo:
                    HandleNewProjectileInfo(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendProjectileVariableData:
                    HandleSentProjectileVariableData(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendPlayerUsedItem:
                    HandlePlayerItemUsage(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendDoneLoading:
                    HandleReceivedDoneLoading(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendAllPlayerSpawnData:
                    HandleReceivedPlayerSpawnData(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendNewItemCreation:
                    HandleReceivedItemCreation(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendItemDeletion:
                    HandleReceivedItemDeletion(peer, reader, senderID);
                    break;

                case ServerPacket.ClientPacketType.SendEnemyListForSync:
                    HandleReceivedEnemyListSync(peer, reader, senderID);
                    break;
            }
        }

        public void HandleReceivedPingPacket(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter pingMessage = new NetDataWriter();
            pingMessage.Put((byte)ServerPacket.ServerPacketType.GivePing);
            pingMessage.Put(senderID);
            pingMessage.Put(0);

            SendMessageBackToSender(pingMessage, sender);
        }

        public void HandleIDRequest(NetPeer sender, NetDataReader reader, int senderID)
        {
            int givenID = clientData.Count + 1;

            NetDataWriter clientIDMessage = new NetDataWriter();
            clientIDMessage.Put((byte)ServerPacket.ServerPacketType.GiveID);
            clientIDMessage.Put(senderID);
            clientIDMessage.Put(givenID);

            SendMessageBackToSender(clientIDMessage, sender);
            Logger.UserFriendlyInfo("Assigned ID of " + givenID + " to a newly connected client.");
        }

        public void HandleNewClientInfo(NetPeer sender, NetDataReader reader, int senderID)
        {
            int clientID = senderID;
            string clientName = reader.GetString();

            ClientData newClientData = new ClientData();
            newClientData.clientID = clientID;
            newClientData.clientName = clientName;
            clientData.Add(clientID, newClientData);

            if (serverManager.ConnectedPeersCount < 2)
                return;

            NetDataWriter clientInfoMessage = new NetDataWriter();
            clientInfoMessage.Put((byte)ServerPacket.ServerPacketType.GiveClientInfo);
            clientInfoMessage.Put(senderID);
            clientInfoMessage.Put(clientID);
            clientInfoMessage.Put(clientName);

            SendMessageToAllOthers(clientInfoMessage, sender);
        }

        public void HandleClientCharacterType(NetPeer sender, NetDataReader reader, int senderID)
        {
            int clientID = senderID;
            int clientCharacterType = reader.GetInt();

            ClientData clientDataClone = clientData[clientID];
            clientDataClone.chosenCharacterType = clientCharacterType;
            clientData[clientID] = clientDataClone;

            if (serverManager.ConnectedPeersCount < 2)
                return;


            NetDataWriter clientCharacterTypeMessage = new NetDataWriter();
            clientCharacterTypeMessage.Put((byte)ServerPacket.ServerPacketType.GiveClientCharacterType);
            clientCharacterTypeMessage.Put(senderID);
            clientCharacterTypeMessage.Put(clientID);
            clientCharacterTypeMessage.Put(clientCharacterType);

            SendMessageToAllOthers(clientCharacterTypeMessage, sender);
        }

        public void HandleAllClientsDataRequest(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter clientDataMessage = new NetDataWriter();
            clientDataMessage.Put((byte)ServerPacket.ServerPacketType.GiveAllClientData);
            clientDataMessage.Put(senderID);
            clientDataMessage.Put(clientData.Count);

            ClientData[] clientDataArray = clientData.Values.ToArray();
            for (int i = 0; i < clientDataArray.Length; i++)        //The data has to be read by index cause we don't know how many players there are
            {
                clientDataMessage.Put((byte)clientDataArray[i].clientID);
                clientDataMessage.Put(clientDataArray[i].clientName);
                clientDataMessage.Put((byte)clientDataArray[i].chosenCharacterType);
            }

            SendMessageBackToSender(clientDataMessage, sender);
        }

        /*public void HandleAllPlayersDataRequest(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter playerDataMessage = new NetDataWriter();
            playerDataMessage.Put((byte)ServerPacket.ServerPacketType.GiveAllPlayerData);
            playerDataMessage.Put(senderID);
            playerDataMessage.Put(playerData.Count);

            PlayerData[] playerDataArray = playerData.Values.ToArray();
            for (int i = 0; i < playerDataArray.Length; i++)        //The data has to be read by index cause we don't know how many players there are
            {
                playerDataMessage.Put(playerDataArray[i].playerID);
                playerDataMessage.Put(playerDataArray[i].name);
                playerDataMessage.Put(playerDataArray[i].health);
            }

            SendMessageBackToSender(playerDataMessage, sender);
        }*/

        public void HandlePlayerDataDeletionRequest(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter playerDataDeletionMessage = new NetDataWriter();
            playerDataDeletionMessage.Put((byte)ServerPacket.ServerPacketType.GivePlayerDataDeletion);
            playerDataDeletionMessage.Put(senderID);

            playerData.Remove(senderID);
            Dictionary<int, PlayerData> temporaryPlayersDict = playerData;
            playerData.Clear();
            for (int i = 0; i < temporaryPlayersDict.Count; i++)
            {
                if (i >= senderID)
                {
                    playerData.Add(i, temporaryPlayersDict[i + 1]);
                }
                else
                {
                    playerData.Add(i, temporaryPlayersDict[i]);
                }
            }

            SendMessageToAllOthers(playerDataDeletionMessage, sender);
            Logger.UserFriendlyInfo("A client has been removed from the game.");
        }

        public void HandleClientMovementInformation(NetPeer sender, NetDataReader reader, int senderID)
        {
            float x = reader.GetFloat();
            float y = reader.GetFloat();
            float velX = reader.GetFloat();
            float velY = reader.GetFloat();
            int direction = reader.GetInt();

            NetDataWriter playerMovementMessage = new NetDataWriter();
            playerMovementMessage.Put((byte)ServerPacket.ServerPacketType.GiveClientMovementInformation);
            playerMovementMessage.Put(senderID);
            playerMovementMessage.Put(x);
            playerMovementMessage.Put(y);
            playerMovementMessage.Put(velX);
            playerMovementMessage.Put(velY);
            playerMovementMessage.Put(direction);

            SendMessageToAllOthers(playerMovementMessage, sender, DeliveryMethod.Unreliable);
        }

        public void HandleReceivedPlayerVariableData(NetPeer sender, NetDataReader reader, int senderID)
        {
            byte variableIndex = reader.GetByte();
            int value1 = reader.GetInt();
            int value2 = reader.GetInt();
            int value3 = reader.GetInt();

            NetDataWriter enemyDataMessage = new NetDataWriter();
            enemyDataMessage.Put((byte)ServerPacket.ServerPacketType.SendPlayerVariableData);
            enemyDataMessage.Put(senderID);
            enemyDataMessage.Put(variableIndex);
            enemyDataMessage.Put(value1);
            enemyDataMessage.Put(value2);
            enemyDataMessage.Put(value3);

            SendMessageToAllOthers(enemyDataMessage, sender);
        }

        /*public void HandlePlayerInfo(NetPeer sender, NetDataReader reader, int senderID)
        {
            PlayerData newPlayerData = new PlayerData();
            string playerName = reader.GetString();
            int playerHealth = reader.GetInt();
            int playerID = reader.GetInt();

            playerData.Add(playerID, newPlayerData);
            playerData[playerID].name = playerName;
            playerData[playerID].health = playerHealth;
            playerData[playerID].playerID = playerID;


            if (serverManager.Connections.Count < 2)
                return;


            NetDataWriter playerInfoMessage = new NetDataWriter();
            playerInfoMessage.Put((byte)ServerPacket.ServerPacketType.GivePlayerInfo);
            playerInfoMessage.Put(senderID);
            playerInfoMessage.Put(playerName);
            playerInfoMessage.Put(playerHealth);
            playerInfoMessage.Put(playerID);

            SendMessageToAllOthers(playerInfoMessage);
        }*/

        public void HandleSentSoundData(NetPeer sender, NetDataReader reader, int senderID)
        {
            int soundType = reader.GetInt();
            float soundPosX = reader.GetFloat();
            float soundPosY = reader.GetFloat();
            float soundTravelDistance = reader.GetFloat();
            float soundPitch = reader.GetFloat();
            float soundVolume = reader.GetFloat();

            NetDataWriter soundInfoMessage = new NetDataWriter();
            soundInfoMessage.Put((byte)ServerPacket.ServerPacketType.PlaySound);
            soundInfoMessage.Put(senderID);
            soundInfoMessage.Put(soundType);
            soundInfoMessage.Put(soundPosX);
            soundInfoMessage.Put(soundPosY);
            soundInfoMessage.Put(soundPitch);
            soundInfoMessage.Put(soundVolume);
            soundInfoMessage.Put(soundTravelDistance);


            SendMessageToAllOthers(soundInfoMessage, sender);
        }

        public void HandleSentMessage(NetPeer sender, NetDataReader reader, int senderID)
        {
            string playerMessage = reader.GetString();

            NetDataWriter stringMessage = new NetDataWriter();
            stringMessage.Put((byte)ServerPacket.ServerPacketType.GiveStringMessageToOtherPlayers);
            stringMessage.Put(senderID);
            stringMessage.Put(playerMessage);

            SendMessageToAllOthers(stringMessage, sender);
            Logger.UserFriendlyInfo("Player " + sender + ": " + playerMessage);
        }

        public void HandleWorldData(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter worldDataMessage = new NetDataWriter();
            /*worldDataMessage.Data = reader.Data;
            worldDataMessage.Data[0] = (byte)ServerPacket.ServerPacketType.SendWorldArrayToAll;*/       //Doesn't work for some reason

            /*worldDataMessage.Data = new byte[reader.Data.Length];
            reader.Data.CopyTo(worldDataMessage.Data, 0);
            worldDataMessage.Data[0] = (byte)ServerPacket.ServerPacketType.SendWorldArrayToAll;
            worldDataMessage.Data[1] = Convert.ToByte(sender); */

            worldDataMessage.Put((byte)ServerPacket.ServerPacketType.SendWorldArrayToAll);
            worldDataMessage.Put(senderID);

            int width = reader.GetInt();
            int height = reader.GetInt();
            worldDataMessage.Put(width);
            worldDataMessage.Put(height);

             for (int x = 0; x < width; x++)
             {
                 for (int y = 0; y < height; y++)
                 {
                     byte tileType = reader.GetByte();
                     byte textureType = reader.GetByte();

                     worldDataMessage.Put(tileType);
                     worldDataMessage.Put(textureType);
                 }
             }
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte objType = reader.GetByte();
                    worldDataMessage.Put(objType);
                    if (objType == 0)
                        continue;

                    int posX = reader.GetInt();
                    int posY = reader.GetInt();
                    byte info1 = reader.GetByte();
                    byte info2 = reader.GetByte();

                    worldDataMessage.Put(posX);
                    worldDataMessage.Put(posY);
                    worldDataMessage.Put(info1);
                    worldDataMessage.Put(info2);
                }
            }

            SendMessageToAllOthers(worldDataMessage, sender);
            Logger.UserFriendlyInfo("Created World of [" + width + ", " + height + "].");
        }

        public void HandleNewEnemyInfo(NetPeer sender, NetDataReader reader, int senderID)
        {
            byte enemyType = reader.GetByte();
            float posX = reader.GetFloat();
            float posY = reader.GetFloat();
            //byte enemyIndex = reader.GetByte();

            /*int objectInfoLength = 0;
            byte[] objectInfo = null;
            int objectExtraInfoLength = 0;
            byte[] objectExtraInfo = null;
            if (bodyType == 1)
            {
                objectInfoLength = reader.GetInt();
                objectInfo = reader.ReadBytes(objectInfoLength);
                objectExtraInfoLength = reader.GetInt();
                objectExtraInfo = reader.ReadBytes(objectExtraInfoLength);
            }*/

            NetDataWriter newEnemyDataMessage = new NetDataWriter();
            newEnemyDataMessage.Put((byte)ServerPacket.ServerPacketType.SendNewEnemyInfo);
            newEnemyDataMessage.Put(senderID);
            newEnemyDataMessage.Put(enemyType);
            newEnemyDataMessage.Put(posX);
            newEnemyDataMessage.Put(posY);
            //newEnemyDataMessage.Put(enemyIndex);
            /*if (bodyType == 1)
            {
                reader.Put(objectInfoLength);
                reader.Put(objectInfo);
                reader.Put(objectExtraInfoLength);
                reader.Put(objectExtraInfo);
            }*/

            SendMessageToAllOthers(newEnemyDataMessage, sender);
        }

        public void HandleSentEnemyVariableData(NetPeer sender, NetDataReader reader, int senderID)
        {
            int objectIndex = reader.GetInt();
            byte variableIndex = reader.GetByte();
            int value1 = reader.GetInt();
            int value2 = reader.GetInt();
            int value3 = reader.GetInt();
            if (variableIndex == 0 && value1 > 4)
                value1 = 1;

            NetDataWriter enemyDataMessage = new NetDataWriter();
            enemyDataMessage.Put((byte)ServerPacket.ServerPacketType.SendEnemyVariableData);
            enemyDataMessage.Put(senderID);
            enemyDataMessage.Put(objectIndex);
            enemyDataMessage.Put(variableIndex);
            enemyDataMessage.Put(value1);
            enemyDataMessage.Put(value2);
            enemyDataMessage.Put(value3);

            SendMessageToAllOthers(enemyDataMessage, sender);
        }

        public void HandleSentEnemyDeletion(NetPeer sender, NetDataReader reader, int senderID)
        {
            int enemyIndex = reader.GetInt();
            int posX = reader.GetInt();
            int posY = reader.GetInt();

            NetDataWriter enemyDeletionMessage = new NetDataWriter();
            enemyDeletionMessage.Put((byte)ServerPacket.ServerPacketType.SendEnemyDeath);
            enemyDeletionMessage.Put(senderID);
            enemyDeletionMessage.Put(enemyIndex);
            enemyDeletionMessage.Put(posX);
            enemyDeletionMessage.Put(posY);

            SendMessageToAllOthers(enemyDeletionMessage, sender);
        }

        public void HandleNewProjectileInfo(NetPeer sender, NetDataReader reader, int senderID)
        {
            byte type = reader.GetByte();
            int posX = reader.GetInt();
            int posY = reader.GetInt();
            float velX = reader.GetInt();
            float velY = reader.GetInt();
            byte owner = reader.GetByte();
            int index = reader.GetInt();
            int info1 = reader.GetInt();
            int info2 = reader.GetInt();
            int info3 = reader.GetInt();

            NetDataWriter newProjectileDataMessage = new NetDataWriter();
            newProjectileDataMessage.Put((byte)ServerPacket.ServerPacketType.SendNewProjectileInfo);
            newProjectileDataMessage.Put(senderID);
            newProjectileDataMessage.Put(type);
            newProjectileDataMessage.Put(posX);
            newProjectileDataMessage.Put(posY);
            newProjectileDataMessage.Put(velX);
            newProjectileDataMessage.Put(velY);

            newProjectileDataMessage.Put(owner);
            newProjectileDataMessage.Put(index);
            newProjectileDataMessage.Put(info1);
            newProjectileDataMessage.Put(info2);
            newProjectileDataMessage.Put(info3);

            SendMessageToAllOthers(newProjectileDataMessage, sender);
        }

        public void HandleSentProjectileVariableData(NetPeer sender, NetDataReader reader, int senderID)
        {
            int objectIndex = reader.GetInt();
            byte variableIndex = reader.GetByte();
            int value1 = reader.GetInt();
            int value2 = reader.GetInt();
            int value3 = reader.GetInt();

            NetDataWriter projectileDataMessage = new NetDataWriter();
            projectileDataMessage.Put((byte)ServerPacket.ServerPacketType.SendProjectileVariableData);
            projectileDataMessage.Put(senderID);
            projectileDataMessage.Put(objectIndex);
            projectileDataMessage.Put(variableIndex);
            projectileDataMessage.Put(value1);
            projectileDataMessage.Put(value2);
            projectileDataMessage.Put(value3);

            SendMessageToAllOthers(projectileDataMessage, sender);
        }

        public void HandlePlayerItemUsage(NetPeer sender, NetDataReader reader, int senderID)
        {
            byte itemType = reader.GetByte();
            int emulatedMousePosX = reader.GetInt();
            int emulatedMousePosY = reader.GetInt();
            bool secondaryUse = reader.GetBool();

            NetDataWriter itemUsageMessage = new NetDataWriter();
            itemUsageMessage.Put((byte)ServerPacket.ServerPacketType.SendPlayerUsedItem);
            itemUsageMessage.Put(senderID);
            itemUsageMessage.Put(itemType);
            itemUsageMessage.Put(emulatedMousePosX);
            itemUsageMessage.Put(emulatedMousePosY);
            itemUsageMessage.Put(secondaryUse);

            SendMessageToAllOthers(itemUsageMessage, sender);
        }

        public void HandleReceivedDoneLoading(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter doneLoadingMessage = new NetDataWriter();
            doneLoadingMessage.Put((byte)ServerPacket.ServerPacketType.SendOtherPlayerDoneLoading);
            doneLoadingMessage.Put(senderID);

            SendMessageToAllOthers(doneLoadingMessage, sender);
        }

        public void HandleReceivedPlayerSpawnData(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter spawnDataMessage = new NetDataWriter();
            spawnDataMessage.Put((byte)ServerPacket.ServerPacketType.SendAllPlayerSpawnDataToOthers);
            spawnDataMessage.Put(senderID);

            int amountOfPlayers = reader.GetInt();
            spawnDataMessage.Put(amountOfPlayers);

            for (int i = 0; i < amountOfPlayers; i++)
            {
                float playerPosX = reader.GetFloat();
                float playerPosY = reader.GetFloat();

                spawnDataMessage.Put(playerPosX);
                spawnDataMessage.Put(playerPosY);
            }

            SendMessageToAllOthers(spawnDataMessage, sender);
        }

        public void HandleReceivedItemCreation(NetPeer sender, NetDataReader reader, int senderID)
        {
            byte itemType = reader.GetByte();
            int xPos = reader.GetInt();
            int yPos = reader.GetInt();

            NetDataWriter itemCreationMessage = new NetDataWriter();
            itemCreationMessage.Put((byte)ServerPacket.ServerPacketType.SendNewItemCreation);
            itemCreationMessage.Put(senderID);
            itemCreationMessage.Put(itemType);
            itemCreationMessage.Put(xPos);
            itemCreationMessage.Put(yPos);

            SendMessageToAllOthers(itemCreationMessage, sender);
        }

        public void HandleReceivedItemDeletion(NetPeer sender, NetDataReader reader, int senderID)
        {
            byte index = reader.GetByte();

            NetDataWriter itemDeletionMessage = new NetDataWriter();
            itemDeletionMessage.Put((byte)ServerPacket.ServerPacketType.SendItemDeletion);
            itemDeletionMessage.Put(senderID);
            itemDeletionMessage.Put(index);

            SendMessageToAllOthers(itemDeletionMessage, sender);
        }

        public void HandleReceivedEnemyListSync(NetPeer sender, NetDataReader reader, int senderID)
        {
            NetDataWriter enemySyncMessage = new NetDataWriter();
            enemySyncMessage.Put((byte)ServerPacket.ServerPacketType.ReceiveEnemyListSync);
            enemySyncMessage.Put(senderID);

            byte amountOfEnemies = reader.GetByte();
            enemySyncMessage.Put(amountOfEnemies);

            dungeonEnemies = new int[amountOfEnemies];
            for (int i = 0; i < amountOfEnemies; i++)
            {
                byte enemyType = reader.GetByte();
                int health = reader.GetInt();
                int posX = reader.GetInt();
                int posY = reader.GetInt();
                dungeonEnemies[i] = enemyType;

                enemySyncMessage.Put(enemyType);
                enemySyncMessage.Put(health);
                enemySyncMessage.Put(posX);
                enemySyncMessage.Put(posY);
            }

            SendMessageToAllOthers(enemySyncMessage, sender);
        }

        public static void SendMessageToAllOthers(NetDataWriter writer, NetPeer senderConnection, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)       //Data sending
        {
            Logger.DebugInfo("Sent a " + (ServerPacket.ServerPacketType)writer.Data[0] + " packet");
            serverManager.SendToAll(writer, deliveryMethod, senderConnection);
        }

        public static void SendMessageBackToSender(NetDataWriter writer, NetPeer sender, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)      //Data retrieval
        {
            Logger.DebugInfo("Reflected a " + (ServerPacket.ServerPacketType)writer.Data[0] + " packet");
            sender.Send(writer, deliveryMethod);
        }

        /*public static void SendMessageToAllOthersOnInterval(NetDataWriter writer, NetPeer senderConnection, int interval, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)       //Data sending
        {
            List<NetPeer> otherConnectionsList = serverManager.Connections;
            otherConnectionsList.Remove(senderConnection);

            int[] clientDataKeys = clientData.Keys.ToArray();
            if (otherConnectionsList.Count >= 1)
            {
                int timer = 0;
                int senderIndex = 0;
                while (senderIndex != otherConnectionsList.Count)
                {
                    timer++;
                    if (timer >= interval)
                    {
                        timer = 0;
                        serverManager.Send(writer, otherConnectionsList[senderIndex], deliveryMethod, 0);
                        Logger.Info("Shot packet to " + clientData[clientDataKeys[senderIndex]].clientName);
                        senderIndex++;
                    }
                }
            }
        }*/
    }
}