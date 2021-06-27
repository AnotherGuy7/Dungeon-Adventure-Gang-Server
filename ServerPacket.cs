using Lidgren.Network;

namespace DAGServer
{
    public class ServerPacket
    {
        public enum ServerPacketType        //Packets that the server sends
        {
            GiveID,
            GiveAllPlayerData,
            GivePlayerDataDeletion,
            GiveClientPositon,
            GivePlayerInfo,
            PlaySound,
            GiveStringMessageToOtherPlayers,
            SendWorldArrayToAll
        }

        public enum ClientPacketType        //Packets that the clients send
        {
            RequestID,
            RequestAllPlayerData,
            RequestPlayerDataDeletion,
            SendClientPosition,
            SendPlayerInfo,
            SendSound,
            SendStringMessageToOtherPlayers,
            SendWorldArray
        }
    }
}
