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
			Vector2i tilePos = Board.ToTilePos(nextPos);

			Player p = other as Player;
			if (p != null)
			{
				if (Owner != p)
				{
					SettleAt(tilePos);
				}
			}
			else if (other is ThrownBlock)
			{
				SettleAt(tilePos);
			}
		}

		private void SettleAt(Vector2i tilePos)
		{
			if (deadYet)
				return;
			deadYet = true;

			MySpr.enabled = false;
			MyVelocity = Vector2.zero;
			DoActionAfterTime(() => Destroy(gameObject), 0.001f);

			Board.AddBlockAt(tilePos, BlockType);
			

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