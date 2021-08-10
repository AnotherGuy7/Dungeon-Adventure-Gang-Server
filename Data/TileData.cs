namespace DAGServer.Data
{
    public class TileData
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
