using Unity;
using UnityEngine.Networking;

namespace UltimateOutfitSync
{
    public class MsgSendPNGChunk : MessageBaseTarget
    {
        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            writer.WritePackedUInt32((uint)this.offset);
            writer.WritePackedUInt32((uint)this.size);
            GeneratedNetworkCode._WriteArrayInt32_None(writer, this.hash);
            writer.WriteBytesAndSize(this.data, this.size);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            this.offset = (int)reader.ReadPackedUInt32();
            this.size = (int)reader.ReadPackedUInt32();
            this.hash = GeneratedNetworkCode._ReadArrayInt32_None(reader);
            this.data = reader.ReadBytesAndSize();
        }

        public int offset;

        public int size;

        public int[] hash;

        public byte[] data;
    }
}