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
			Vector2i tilePos = Board.ToTilePos(MyTr.position);

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
			

			return;

			//Push people away from this block.
			Rect blockBnds = Board.ToWorldRect(tilePos);
			foreach (DynamicObject obj in ObjectsInWorld)
			{
				if (obj != this)
				{
					Vector2 center = obj.MyTr.position,
							minCorner = center - (obj.CollisionBoxSize * 0.5f);

					Rect theirBnds = new Rect(minCorner, obj.CollisionBoxSize);

					if (blockBnds.Overlaps(theirBnds))
					{
						Vector2 push = theirBnds.center - blockBnds.center;
						push.x /= theirBnds.width;
						push.y /= theirBnds.height;

						Vector2 pushScaleAbs = new Vector2(Mathf.Abs(push.x / theirBnds.width),
														   Mathf.Abs(push.y / theirBnds.height));

						if (pushScaleAbs.x > pushScaleAbs.y)
						{
							if (push.x > 0.0f)
							{

							}
						}
					}
				}
			}
		}
	}
}