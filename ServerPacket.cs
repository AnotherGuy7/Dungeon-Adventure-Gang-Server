﻿namespace DAGServer
{
    public class ServerPacket
    {
        public enum ServerPacketType        //Packets that the server sends
        {
            GivePing,
            GiveID,
            GiveClientInfo,
            GiveClientCharacterType,
            GiveAllClientData,
            GiveAllPlayerData,
            GivePlayerDataDeletion,
            GiveClientMovementInformation,
            SendPlayerVariableData,
            PlaySound,
            GiveStringMessageToOtherPlayers,
            SendWorldArrayToAll,
            SendUpdatedMapObject,
            SendNewEnemyInfo,
            SendNewProjectileInfo,
            SendEnemyVariableData,
            SendEnemyDeath,
            SendProjectileVariableData,
            SendPlayerUsedItem,
            SendOtherPlayerDoneLoading,
            SendAllPlayerSpawnDataToOthers,
            SendNewItemCreation,
            SendItemDeletion,
            ReceiveEnemyListSync
        }

        public enum ClientPacketType        //Packets that the clients send
        {
            SendPing,
            RequestID,
            SendClientInfo,
            SendClientCharacterType,
            RequestAllClientData,
            RequestAllPlayerData,
            RequestPlayerDataDeletion,
            SendMovementInformation,
            SendPlayerVariableData,
            SendSound,
            SendStringMessageToOtherPlayers,
            SendWorldArray,
            SendUpdatedMapObject,
            SendNewEnemyInfo,
            SendEnemyVariableData,
            SendEnemyDeletion,
            SendNewProjectileInfo,
            SendProjectileVariableData,
            SendPlayerUsedItem,
            SendDoneLoading,
            SendAllPlayerSpawnData,
            SendNewItemCreation,
            SendItemDeletion,
            SendEnemyListForSync
        }
    }
}
