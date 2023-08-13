using Unity;
using UnityEngine.Networking;

namespace UltimateOutfitSync
{
    public class MsgRequestNextPNGChunk : MessageBaseTarget
    {
        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.WritePackedUInt32((uint)this.offset);
            GeneratedNetworkCode._WriteArrayInt32_None(writer, this.hash);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            this.offset = (int)reader.ReadPackedUInt32();
            this.hash = GeneratedNetworkCode._ReadArrayInt32_None(reader);
        }

        public int offset;

        public int[] hash;
    }
}