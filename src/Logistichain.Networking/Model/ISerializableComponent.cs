using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Logistichain.Networking.Model
{
    public interface ISerializableComponent
    {
        void Deserialize(BinaryReader reader);
        void Serialize(BinaryWriter writer);
        byte[] ToByteArray();
    }
}