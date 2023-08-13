using Unity;
using UnityEngine.Networking;

namespace UltimateOutfitSync
{
    public class MsgRequestCustomSkin : MessageBaseTarget
    {
        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);
            GeneratedNetworkCode._WriteArrayInt32_None(writer, this.hash);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);
            this.hash = GeneratedNetworkCode._ReadArrayInt32_None(reader);
        }

        public int[] hash;
    }
}