using Lidgren.Network;

namespace DAGServer
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
            SendNewEnemyInfo,
            SendNewProjectileInfo,
            SendEnemyVariableData,
            SendProjectileVariableData,
            SendPlayerUsedItem,
            SendOtherPlayerDoneLoading,
            SendAllPlayerSpawnDataToOthers,
            SendOtherPlayerState,
            SendNewItemCreation,
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
            SendNewEnemyInfo,
            SendNewProjectileInfo,
            SendEnemyVariableData,
            SendProjectileVariableData,
            SendPlayerUsedItem,
            SendDoneLoading,
            SendAllPlayerSpawnData,
            SendPlayerState,
            SendNewItemCreation,
            SendEnemyListForSync
        }
    }
}
