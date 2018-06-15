using Mpb.Model;

namespace Mpb.Shared.Events
{
    public class BlockCreatedEventArgs
    {
        public BlockCreatedEventArgs(Block createdBlock)
        {
            Block = createdBlock;
        }

        public Block Block { get; }
    }
}