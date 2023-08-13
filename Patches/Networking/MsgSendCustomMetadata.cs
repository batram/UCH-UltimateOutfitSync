using SevenZip.Compression.LZMA;
using Unity;
using UnityEngine.Networking;

namespace UltimateOutfitSync
{
    public class MsgSendCustomMetadata : MessageBaseTarget
    {
        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            GeneratedNetworkCode._WriteArrayInt32_None(writer, this.hash);
            byte[] comp = SevenZipHelper.Compress(this.metadata);
            writer.WriteBytesAndSize(comp, comp.Length);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            this.hash = GeneratedNetworkCode._ReadArrayInt32_None(reader);
            this.metadata = SevenZipHelper.Decompress(reader.ReadBytesAndSize());
        }

        public int[] hash;

        public byte[] metadata;
    }
}