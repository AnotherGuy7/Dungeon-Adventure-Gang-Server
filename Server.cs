using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
        public const bool PacketLogs = false;

        public static NetPeer serverPeer;
        public static NetManager serverManager;
        public static EventBasedNetListener serverListener;
        public static Dictionary<int, ClientData> clientData = new Dictionary<int, ClientData>();
        //public static Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();
        public static bool gameCurrentlyActive = false;
        public static bool clientConnecting = false;

        public static int amountOfConnectedPlayers = 0;
        public static int[] dungeonEnemies = new int[255];
        public static int[] gameProjectileExists = new int[1000];

        public void CreateNewServer()
        {
            //UPnPManager.InitializeUPnP();
            Logger.Info("Creating Server...");

            serverListener = new EventBasedNetListener();
            serverManager = new NetManager(serverListener);
            serverManager.UpdateTime = 15;
            serverManager.AutoRecycle = true;

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
            amountOfConnectedPlayers--;
            if (amountOfConnectedPlayers <= 0)
            {
                Logger.Error("All Clients disconnected. Server shutting down.");
                Thread.Sleep(1000);

                Program.serverShutDown = true;
            }
        }

        public static string GetPublicIp(string serviceUrl = "https://ipinfo.io/ip")            //https://stackoverflow.com/questions/3253701/get-public-external-ip-address 
        {
            try
            {
                string IP = new WebClient().DownloadString(serviceUrl);
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
            if (serverManager.ConnectedPeersCount >= MaximumLobbySize)
            {
                Logger.Error("Client Connection Request denied.\n      Reason: Too many clients.");
                request.Reject();
                return;
            }
            if (gameCurrentlyActive)
            {
                Logger.Error("Client Connection Request denied.\n      Reason: Game currently active.");
                request.Reject();
                return;
            }
            if (clientConnecting)
            {
                Logger.Error("Client Connection Request denied.\n      Reason: A different client is already attempting to connect to the server.");
                request.Reject();
                return;
            }

            request.Accept();
            amountOfConnectedPlayers++;
            //clientConnecting = true;
            Logger.Info("Client Connection Request Accepted.");
        }

        public void ClientConnected(NetPeer peer)       //Upon client connections, we send back lobby data and new client info.
        {
            clientConnecting = false;
            Logger.Info("New Client connected.");
        }

        public void HandleDataMessages(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            try
            {
                ServerPacket.ClientPacketType messageDataType = (ServerPacket.ClientPacketType)reader.GetByte();
                byte senderID = reader.GetByte();
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

                    case ServerPacket.ClientPacketType.RequestPlayerDataDeletion:
                        HandlePlayerDataDeletionRequest(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendMovementInformation:      //Sends the peer's position to all peers that aren't the sender
                        HandleClientMovementInformation(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendPlayerVariableData:
                        HandleReceivedPlayerVariableData(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendSound:
                        HandleSentSoundData(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendStringMessageToOtherPlayers:
                        HandleSentMessage(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendWorldArray:
                        HandleWorldData(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendUpdatedMapObject:
                        HandleUpdatedMapObject(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendMapObjectDestruction:
                        HandleDestroyedMapObject(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendNewEnemyInfo:
                        HandleNewEnemyInfo(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendEnemyVariableData:
                        HandleSentEnemyVariableData(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendEnemyAilment:
                        HandleSentEnemyAilmentUpdate(peer, reader, senderID);
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

                    case ServerPacket.ClientPacketType.RequestEnemyData:
                        HandleEnemyDataRequest(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendEnemyData:
                        HandleReceivedEnemyData(peer, reader, senderID);
                        break;

                    case ServerPacket.ClientPacketType.SendEnemyDamage:
                        HandleReceivedEnemyDamage(peer, reader, senderID);
                        break;
                }
            }
            catch (Exception exception)
            {
                Logger.CreateFileLogs(exception);
                Logger.Error("Error at:" + exception.StackTrace + "\nMessage: " + exception.Message + "\nSource: " + exception.Source);
            }
        }

        public void HandleReceivedPingPacket(NetPeer sender, NetDataReader reader, byte senderID)
        {
            NetDataWriter pingMessage = new NetDataWriter();
            pingMessage.Put((byte)ServerPacket.ServerPacketType.GivePing);
            pingMessage.Put(senderID);

            SendMessageBackToSender(pingMessage, sender);
        }

        public void HandleIDRequest(NetPeer sender, NetDataReader reader, byte senderID)
        {
            int givenID = clientData.Count + 1;

            NetDataWriter clientIDMessage = new NetDataWriter();
            clientIDMessage.Put((byte)ServerPacket.ServerPacketType.GiveID);
            clientIDMessage.Put(senderID);
            clientIDMessage.Put(givenID);

            SendMessageBackToSender(clientIDMessage, sender);
            Logger.UserFriendlyInfo("Assigned ID of " + givenID + " to a newly connected client.");
        }

        public void HandleNewClientInfo(NetPeer sender, NetDataReader reader, byte senderID)
        {
            byte clientID = senderID;
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
            clientInfoMessage.Put(clientName);

            SendMessageToAllOthers(clientInfoMessage, sender);
        }

        public void HandleClientCharacterType(NetPeer sender, NetDataReader reader, byte senderID)
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
            clientCharacterTypeMessage.Put(clientCharacterType);

            SendMessageToAllOthers(clientCharacterTypeMessage, sender);
        }

        public void HandleAllClientsDataRequest(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandlePlayerDataDeletionRequest(NetPeer sender, NetDataReader reader, byte senderID)
        {
            NetDataWriter playerDataDeletionMessage = new NetDataWriter();
            playerDataDeletionMessage.Put((byte)ServerPacket.ServerPacketType.GivePlayerDataDeletion);
            playerDataDeletionMessage.Put(senderID);

            if(!clientData.ContainsKey(senderID))
            {
                return;
            }

            string playerName = clientData[senderID].clientName;

            Dictionary<int, ClientData> newClientData = new Dictionary<int, ClientData>();
            int[] clientDataKeys = clientData.Keys.ToArray();

            foreach(ClientData client in clientData.Values)
            {
                if (client.clientID < senderID)
                {
                    if (clientData.ContainsKey(client.clientID) && clientDataKeys.Length > client.clientID)
                    {
                        newClientData.Add(client.clientID, client);
                    }
                }
                else if (client.clientID > senderID)
                {
                    ClientData transferredClientData = client;
                    if(client.clientID > 1)
                    {
                        transferredClientData.clientID = (byte)(client.clientID - 1);
                        newClientData.Add(client.clientID - 1, transferredClientData);
                    }
                }
            }

            clientData = newClientData;

            SendMessageToAllOthers(playerDataDeletionMessage, sender);
            Logger.UserFriendlyInfo(playerName + " has been removed from the game." + clientData.Count);
            serverManager.DisconnectPeer(sender);
        }

        public void HandleClientMovementInformation(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandleReceivedPlayerVariableData(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandleSentSoundData(NetPeer sender, NetDataReader reader, byte senderID)
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
            soundInfoMessage.Put(soundTravelDistance);
            soundInfoMessage.Put(soundPitch);
            soundInfoMessage.Put(soundVolume);


            SendMessageToAllOthers(soundInfoMessage, sender);
        }

        public void HandleSentMessage(NetPeer sender, NetDataReader reader, byte senderID)
        {
            string playerMessage = reader.GetString();

            NetDataWriter stringMessage = new NetDataWriter();
            stringMessage.Put((byte)ServerPacket.ServerPacketType.GiveStringMessageToOtherPlayers);
            stringMessage.Put(senderID);
            stringMessage.Put(playerMessage);

            SendMessageToAllOthers(stringMessage, sender);
            Logger.UserFriendlyInfo("Player " + clientData[senderID].clientName + ": " + playerMessage);
        }

        public void HandleWorldData(NetPeer sender, NetDataReader reader, byte senderID)
        {
            NetDataWriter worldDataMessage = new NetDataWriter();
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

                    int index = reader.GetInt();
                    int posX = reader.GetInt();
                    int posY = reader.GetInt();
                    byte info1 = reader.GetByte();
                    byte info2 = reader.GetByte();

                    worldDataMessage.Put(index);
                    worldDataMessage.Put(posX);
                    worldDataMessage.Put(posY);
                    worldDataMessage.Put(info1);
                    worldDataMessage.Put(info2);
                }
            }

            gameCurrentlyActive = true;
            SendMessageToAllOthers(worldDataMessage, sender);
            Logger.UserFriendlyInfo("Created World of [" + width + ", " + height + "].");
        }

        public void HandleUpdatedMapObject(NetPeer sender, NetDataReader reader, byte senderID)
        {
            int objectIndex = reader.GetInt();
            int posX = reader.GetInt();
            int posY = reader.GetInt();
            byte info1 = reader.GetByte();
            byte info2 = reader.GetByte();

            NetDataWriter mapObjectMessage = new NetDataWriter();
            mapObjectMessage.Put((byte)ServerPacket.ServerPacketType.SendUpdatedMapObject);
            mapObjectMessage.Put(senderID);
            mapObjectMessage.Put(objectIndex);
            mapObjectMessage.Put(posX);
            mapObjectMessage.Put(posY);
            mapObjectMessage.Put(info1);
            mapObjectMessage.Put(info2);
            SendMessageToAllOthers(mapObjectMessage, sender);
        }

        public void HandleDestroyedMapObject(NetPeer sender, NetDataReader reader, byte senderID)
        {
            int objectIndex = reader.GetInt();

            NetDataWriter mapObjectDestructionMessage = new NetDataWriter();
            mapObjectDestructionMessage.Put((byte)ServerPacket.ServerPacketType.SendMapObjectDestruction);
            mapObjectDestructionMessage.Put(senderID);
            mapObjectDestructionMessage.Put(objectIndex);
            SendMessageToAllOthers(mapObjectDestructionMessage, sender);
        }

        public void HandleNewEnemyInfo(NetPeer sender, NetDataReader reader, byte senderID)
        {
            byte enemyType = reader.GetByte();
            int id = reader.GetInt();
            float posX = reader.GetFloat();
            float posY = reader.GetFloat();

            NetDataWriter newEnemyDataMessage = new NetDataWriter();
            newEnemyDataMessage.Put((byte)ServerPacket.ServerPacketType.SendNewEnemyInfo);
            newEnemyDataMessage.Put(senderID);
            newEnemyDataMessage.Put(enemyType);
            newEnemyDataMessage.Put(id);
            newEnemyDataMessage.Put(posX);
            newEnemyDataMessage.Put(posY);

            SendMessageToAllOthers(newEnemyDataMessage, sender);
        }

        public void HandleSentEnemyVariableData(NetPeer sender, NetDataReader reader, byte senderID)
        {
            int id = reader.GetInt();
            byte enemyType = reader.GetByte();
            byte variableIndex = reader.GetByte();
            int value1 = reader.GetInt();
            int value2 = reader.GetInt();
            int value3 = reader.GetInt();
            if (variableIndex == 0 && value1 > 4)
                value1 = 1;

            NetDataWriter enemyDataMessage = new NetDataWriter();
            enemyDataMessage.Put((byte)ServerPacket.ServerPacketType.SendEnemyVariableData);
            enemyDataMessage.Put(senderID);
            enemyDataMessage.Put(id);
            enemyDataMessage.Put(enemyType);
            enemyDataMessage.Put(variableIndex);
            enemyDataMessage.Put(value1);
            enemyDataMessage.Put(value2);
            enemyDataMessage.Put(value3);

            SendMessageToAllOthers(enemyDataMessage, sender);
        }

        public void HandleSentEnemyAilmentUpdate(NetPeer sender, NetDataReader reader, byte senderID)
        {
            int id = reader.GetInt();
            byte ailmentIndex = reader.GetByte();
            byte ailmentType = reader.GetByte();
            byte ailmentStage = reader.GetByte();

            NetDataWriter enemyAilmentMessage = new NetDataWriter();
            enemyAilmentMessage.Put((byte)ServerPacket.ServerPacketType.SendEnemyAilment);
            enemyAilmentMessage.Put(senderID);
            enemyAilmentMessage.Put(id);
            enemyAilmentMessage.Put(ailmentIndex);
            enemyAilmentMessage.Put(ailmentType);
            enemyAilmentMessage.Put(ailmentStage);

            SendMessageToAllOthers(enemyAilmentMessage, sender);
        }

        public void HandleSentEnemyDeletion(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandleNewProjectileInfo(NetPeer sender, NetDataReader reader, byte senderID)
        {
            byte type = reader.GetByte();
            int posX = reader.GetInt();
            int posY = reader.GetInt();
            float velX = reader.GetFloat();
            float velY = reader.GetFloat();
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

        public void HandleSentProjectileVariableData(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandlePlayerItemUsage(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandleReceivedDoneLoading(NetPeer sender, NetDataReader reader, byte senderID)
        {
            NetDataWriter doneLoadingMessage = new NetDataWriter();
            doneLoadingMessage.Put((byte)ServerPacket.ServerPacketType.SendOtherPlayerDoneLoading);
            doneLoadingMessage.Put(senderID);

            SendMessageToAllOthers(doneLoadingMessage, sender);
        }

        public void HandleReceivedPlayerSpawnData(NetPeer sender, NetDataReader reader, byte senderID)
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

        public void HandleReceivedItemCreation(NetPeer sender, NetDataReader reader, byte senderID)
        {
            byte itemType = reader.GetByte();
            int id = reader.GetInt();
            int xPos = reader.GetInt();
            int yPos = reader.GetInt();
            float xVel = reader.GetFloat();
            float yVel = reader.GetFloat();

            NetDataWriter itemCreationMessage = new NetDataWriter();
            itemCreationMessage.Put((byte)ServerPacket.ServerPacketType.SendNewItemCreation);
            itemCreationMessage.Put(senderID);
            itemCreationMessage.Put(itemType);
            itemCreationMessage.Put(id);
            itemCreationMessage.Put(xPos);
            itemCreationMessage.Put(yPos);
            itemCreationMessage.Put(xVel);
            itemCreationMessage.Put(yVel);

            SendMessageToAllOthers(itemCreationMessage, sender);
        }

        public void HandleReceivedItemDeletion(NetPeer sender, NetDataReader reader, byte senderID)
        {
            int id = reader.GetInt();

            NetDataWriter itemDeletionMessage = new NetDataWriter();
            itemDeletionMessage.Put((byte)ServerPacket.ServerPacketType.SendItemDeletion);
            itemDeletionMessage.Put(senderID);
            itemDeletionMessage.Put(id);

            SendMessageToAllOthers(itemDeletionMessage, sender);
        }

        public void HandleReceivedEnemyListSync(NetPeer sender, NetDataReader reader, byte senderID)
        {
            NetDataWriter enemySyncMessage = new NetDataWriter();
            enemySyncMessage.Put((byte)ServerPacket.ServerPacketType.ReceiveEnemyListSync);
            enemySyncMessage.Put(senderID);

            byte amountOfEnemies = reader.GetByte();
            enemySyncMessage.Put(amountOfEnemies);

            dungeonEnemies = new int[amountOfEnemies];

            /*for (int i = 0; i < amountOfEnemies; i++)
            {
                byte enemyType = reader.GetByte();
                int id = reader.GetInt();
                int health = reader.GetInt();
                int posX = reader.GetInt();
                int posY = reader.GetInt();
                dungeonEnemies[i] = enemyType;

                enemySyncMessage.Put(enemyType);
                enemySyncMessage.Put(id);
                enemySyncMessage.Put(health);
                enemySyncMessage.Put(posX);
                enemySyncMessage.Put(posY);
            }*/

            for (var i = 0; i < amountOfEnemies; i++)
            {
                int id = reader.GetInt();
                enemySyncMessage.Put(id);
            }

            SendMessageToAllOthers(enemySyncMessage, sender);
        }

        //Only requests the new data... If we have a network bottle neck we could look here
        public void HandleEnemyDataRequest(NetPeer sender, NetDataReader reader, byte senderid)
        {
            int id = reader.GetInt();

            NetDataWriter enemydatamessage = new NetDataWriter();
            enemydatamessage.Put((byte)ServerPacket.ServerPacketType.SendRequestedEnemyData);
            enemydatamessage.Put(senderid);
            enemydatamessage.Put(id);

            SendMessageToAllOthers(enemydatamessage, sender);
        }

        //Only sends the new data... If we have a network bottle neck we could look here
        public void HandleReceivedEnemyData(NetPeer sender, NetDataReader reader, byte senderid)
        {
            byte receiverid = reader.GetByte();
            byte enemyType = reader.GetByte();
            int id = reader.GetInt();
            int health = reader.GetInt();
            float posX = reader.GetFloat();
            float posY = reader.GetFloat();

            NetDataWriter enemydatamessage = new NetDataWriter();
            enemydatamessage.Put((byte)ServerPacket.ServerPacketType.SendEnemyData);
            enemydatamessage.Put(senderid);
            enemydatamessage.Put(receiverid);
            enemydatamessage.Put(enemyType);
            enemydatamessage.Put(id);
            enemydatamessage.Put(health);
            enemydatamessage.Put(posX);
            enemydatamessage.Put(posY);

            SendMessageToAllOthers(enemydatamessage, sender);
        }

        public void HandleReceivedEnemyDamage(NetPeer sender, NetDataReader reader, byte senderid)
        {
            int id = reader.GetInt();
            int damage = reader.GetInt();
            float x = reader.GetFloat();
            float y = reader.GetFloat();
            float knockback = reader.GetFloat();
            int immunitytimer = reader.GetInt();

            NetDataWriter enemydamagemessage = new NetDataWriter();
            enemydamagemessage.Put((byte)ServerPacket.ServerPacketType.SendEnemyData);
            enemydamagemessage.Put(senderid);
            enemydamagemessage.Put(id);
            enemydamagemessage.Put(damage);
            enemydamagemessage.Put(x);
            enemydamagemessage.Put(y);
            enemydamagemessage.Put(knockback);
            enemydamagemessage.Put(immunitytimer);

            SendMessageToAllOthers(enemydamagemessage, sender);
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
    }
}