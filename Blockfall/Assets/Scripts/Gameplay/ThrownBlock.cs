using System;
using System.Collections.Generic;
using UnityEngine;


namespace Gameplay
{
	/// <summary>
	/// Turns into a solid block as soon as it hits something.
	/// </summary>
	public class ThrownBlock : DynamicObject
	{
		public GameBoard.BlockTypes BlockType;


		public override void OnHitLeftSide(Vector2i wallPos, ref Vector2 nextPos)
		{
			Vector2i tilePos = Board.ToTilePos(nextPos);
			SettleAt(tilePos);
		}
		public override void OnHitRightSide(Vector2i wallPos, ref Vector2 nextPos)
		{
			Vector2i tilePos = Board.ToTilePos(nextPos);
			SettleAt(tilePos);
		}
		public override void OnHitFloor(Vector2i floorPos, ref Vector2 nextPos)
		{
			Vector2i tilePos = Board.ToTilePos(nextPos);
			SettleAt(tilePos);
		}
		public override void OnHitCeiling(Vector2i ceilingPos, ref Vector2 nextPos)
		{
			Vector2i tilePos = Board.ToTilePos(nextPos);
			SettleAt(tilePos);
		}

		public override void OnHitDynamicObject(DynamicObject other, ref Vector2 nextPos)
		{
			if (other is ThrownBlock || other is Player)
			{
				Vector2i tilePos = Board.ToTilePos(nextPos);
				SettleAt(tilePos);
			}
		}

		private void SettleAt(Vector2i tilePos)
		{
			MyVelocity = Vector2.zero;
			DoActionAfterTime(() => Destroy(gameObject), 0.001f);

			Board.AddBlockAt(tilePos, BlockType);
		}
	}
}