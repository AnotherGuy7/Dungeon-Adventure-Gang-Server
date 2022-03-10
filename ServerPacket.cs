namespace DAGServer
{
    public class ServerPacket
    {
        public enum ServerPacketType        //Packets that the server sends
        {
            GivePing,
            GiveClientInfo,
            GiveClientCharacterType,
            GiveAllClientData,
            GiveLobbyData,
            GivePlayerDataDeletion,
            GiveClientMovementInformation,
            SendPlayerVariableData,
            PlaySound,
            GiveStringMessageToOtherPlayers,
            SendWorldArrayToAll,
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
            SendClientInfo,
            SendClientCharacterType,
            RequestAllClientData,
            RequestLobbyData,
            RequestPlayerDataDeletion,
            SendMovementInformation,
            SendPlayerVariableData,
            SendSound,
            SendStringMessageToOtherPlayers,
            SendWorldArray,
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
