using Lidgren.Network;

namespace DAGServer
{
    public class ServerPacket
    {
        public enum ServerPacketType        //Packets that the server sends
        {
            GiveID,
            GiveClientInfo,
            GiveClientCharacterType,
            GiveAllClientData,
            GiveAllPlayerData,
            GivePlayerDataDeletion,
            GiveClientPositon,
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
            SendNewItemCreation
        }

        public enum ClientPacketType        //Packets that the clients send
        {
            RequestID,
            SendClientInfo,
            SendClientCharacterType,
            RequestAllClientData,
            RequestAllPlayerData,
            RequestPlayerDataDeletion,
            SendClientPosition,
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
            SendNewItemCreation
        }
    }
}
