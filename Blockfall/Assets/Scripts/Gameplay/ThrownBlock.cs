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
		public Player Owner;

		private bool deadYet = false;


		public override void OnHitLeftSide(Vector2i wallPos) { Settle(); }
		public override void OnHitRightSide(Vector2i wallPos) { Settle(); }
		public override void OnHitFloor(Vector2i floorPos) { Settle(); }
		public override void OnHitCeiling(Vector2i ceilingPos) { Settle(); }

		public override void OnHitDynamicObject(DynamicObject other)
		{
			Player p = other as Player;
			if (p != null && !p.IsInvincible)
			{
				if (Owner != p)
					Settle();
			}
			else if (other is ThrownBlock)
			{
				Settle();
			}
		}

		private void Settle()
		{
			Vector2i tilePos = Board.ToTilePos((Vector2)MyTr.position);

			if (deadYet)
				return;
			deadYet = true;

			MySpr.enabled = false;
			MyVelocity = Vector2.zero;
			DoActionAfterTime(() => Destroy(gameObject), 0.001f);

			Board[tilePos] = BlockType;
		}
	}
}