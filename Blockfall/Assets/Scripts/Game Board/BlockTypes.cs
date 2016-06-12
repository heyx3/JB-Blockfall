using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameBoard
{
	public enum BlockTypes
	{
		Empty = 0,
		Normal,
		Immobile,
	}

    public static class BlockQueries
    {
        /// <summary>
        /// Whether movement is blocked by the given block type.
        /// </summary>
        public static bool IsSolid(BlockTypes b)
        {
            switch (b)
            {
                case BlockTypes.Empty:
                    return false;
                case BlockTypes.Immobile:
                case BlockTypes.Normal:
                    return true;
                default:
                    throw new NotImplementedException(b.ToString());
            }
        }
        /// <summary>
        /// Whether the given block type can be picked up by a player.
        /// </summary>
        public static bool CanPickUp(BlockTypes b)
        {
            switch (b)
            {
                case BlockTypes.Empty:
                case BlockTypes.Immobile:
                    return false;
                case BlockTypes.Normal:
                    return true;
                default:
                    throw new NotImplementedException(b.ToString());
            }
        }
    }
}
