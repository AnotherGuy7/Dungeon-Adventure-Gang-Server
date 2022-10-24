namespace DAGServer
{
    public class ServerData
    {
        public struct ClientData
        {
            public byte clientID;
            public string clientName;
            public int chosenCharacterType;
        }

        public struct PlayerData
        {
            public string name;
            public int health;
            public byte playerID;
            public byte chosenCharacterType;
            public int posX;
            public int posY;
        }

        public struct TileData
        {
            public string tileTexturePath;
            public float tilePositionX;
            public float tilePositionY;
            public CollisionStyle tileCollisionStyle;

            public enum CollisionStyle
            {
                None,
                Solid
            }
        }
    }
}
