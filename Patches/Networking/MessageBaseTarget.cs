using Unity;
using UnityEngine.Networking;

namespace UltimateOutfitSync
{
    public class MessageBaseTarget : MessageBase
    {
        public override void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32(this.targetNetId);
            writer.WritePackedUInt32(this.sourceNetId);

            if (this.murks != null && this.murks.Length != 0)
            {
                writer.Write(this.murks, this.murks.Length);
            } 
        }

        public override void Deserialize(NetworkReader reader)
        {
            this.targetNetId = reader.ReadPackedUInt32();
            this.sourceNetId = reader.ReadPackedUInt32();
        }

        public uint targetNetId;
        public uint sourceNetId;

        public byte[] murks;
    }
}