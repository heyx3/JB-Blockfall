using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Gameplay
{
	public abstract class ControllableObject : DynamicObject
	{
		[NonSerialized]
		public bool IsOnFloor = false;
		[NonSerialized]
		public bool IsOnCeiling = false;
		[NonSerialized]
		public bool IsOnLeftWall = false;
		[NonSerialized]
		public bool IsOnRightWall = false;

		
		public override void OnHitFloor(Vector2i floorPos)
		{
			base.OnHitFloor(floorPos);
			IsOnFloor = true;
		}
		public override void OnHitCeiling(Vector2i ceilingPos)
		{
			base.OnHitCeiling(ceilingPos);
			IsOnCeiling = true;
		}
		public override void OnHitLeftSide(Vector2i wallPos)
		{
			base.OnHitLeftSide(wallPos);
			IsOnLeftWall = true;
		}
		public override void OnHitRightSide(Vector2i wallPos)
		{
			base.OnHitRightSide(wallPos);
			IsOnRightWall = true;
		}

		public virtual void OnNoFloor() { }
		public virtual void OnNoCeiling() { }
		public virtual void OnNoLeftWall() { }
		public virtual void OnNoRightWall() { }

		protected virtual void Update()
		{
			//Update movement.
			float moveX = GetLeftRightMoveInput();
			if ((IsOnLeftWall && moveX < 0.0f) ||
				(IsOnRightWall && moveX > 0.0f))
			{
				moveX = 0.0f;
			}
			MyVelocity = new Vector2(moveX, MyVelocity.y);

			//Mirror the object based on movement input:
			if (moveX < 0.0f)
			{
				MyTr.localScale = new Vector3(-Math.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}
			else if (moveX > 0.0f)
			{
				MyTr.localScale = new Vector3(Math.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}


			//Check whether we're still on any surfaces.

			if (IsOnFloor)
			{
				IsOnFloor = false;

				Rect collBnds = MyCollRect;
				int y = Board.ToTilePosY(collBnds.yMin);
				int startX = Board.ToTilePosX(collBnds.xMin),
					endX = Board.ToTilePosX(collBnds.xMax);
				for (int x = startX; x <= endX; ++x)
				{
					if (Board.IsSolid(new Vector2i(x, y - 1)))
					{
						IsOnFloor = true;
						break;
					}
				}

				if (!IsOnFloor)
					OnNoFloor();
			}

			if (IsOnCeiling)
			{
				IsOnCeiling = false;

				Rect collBnds = MyCollRect;
				int y = Board.ToTilePosY(collBnds.yMax);
				int startX = Board.ToTilePosX(collBnds.xMin),
					endX = Board.ToTilePosX(collBnds.xMax);
				for (int x = startX; x <= endX; ++x)
				{
					if (Board.IsSolid(new Vector2i(x, y + 1)))
					{
						IsOnCeiling = true;
						break;
					}
				}

				if (!IsOnCeiling)
					OnNoCeiling();
			}
			
			if (IsOnLeftWall)
			{
				IsOnLeftWall = false;

				Rect collBnds = MyCollRect;
				int x = Board.ToTilePosX(collBnds.xMin);
				int startY = Board.ToTilePosY(collBnds.yMin),
					endY = Board.ToTilePosY(collBnds.yMax);
				for (int y = startY; y <= endY; ++y)
				{
					if (Board.IsSolid(new Vector2i(x - 1, y)))
					{
						IsOnLeftWall = true;
						break;
					}
				}

				if (!IsOnLeftWall)
					OnNoLeftWall();
			}

			if (IsOnRightWall)
			{
				IsOnRightWall = false;

				Rect collBnds = MyCollRect;
				int x = Board.ToTilePosX(collBnds.xMax);
				int startY = Board.ToTilePosY(collBnds.yMin),
					endY = Board.ToTilePosY(collBnds.yMax);
				for (int y = startY; y <= endY; ++y)
				{
					if (Board.IsSolid(new Vector2i(x + 1, y)))
					{
						IsOnRightWall = true;
						break;
					}
				}

				if (!IsOnRightWall)
					OnNoRightWall();
			}
		}
		protected abstract float GetLeftRightMoveInput();
	}
}