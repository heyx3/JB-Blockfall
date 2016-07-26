using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameObjects
{
	public abstract class ControllableObject : DynamicObject
	{
		public float GravityMultiplier = 1.0f;
		public MovementTypes MovementType = MovementTypes.Normal;

		[NonSerialized]
		public bool IsOnFloor = false;
		[NonSerialized]
		public bool IsOnCeiling = false;
		[NonSerialized]
		public bool IsOnLeftWall = false;
		[NonSerialized]
		public bool IsOnRightWall = false;

		[NonSerialized]
		public float VerticalSpeed = 0.0f;

		public float LastMoveSpeedX { get; private set; }

		
		public override void OnHitFloor(Vector2i floorPos)
		{
			base.OnHitFloor(floorPos);
			VerticalSpeed = Math.Max(0.0f, VerticalSpeed);
			IsOnFloor = true;
		}
		public override void OnHitCeiling(Vector2i ceilingPos)
		{
			base.OnHitCeiling(ceilingPos);
			VerticalSpeed = Math.Min(0.0f, VerticalSpeed);
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

		protected override void Awake()
		{
			base.Awake();

			LastMoveSpeedX = 0.0f;
		}
		protected override void FixedUpdate()
		{
			base.FixedUpdate();


			//Update movement.
			
			Vector2 movement = new Vector2(0.0f, VerticalSpeed);

			//Falling:
			if (!IsOnFloor)
				VerticalSpeed += Consts.Instance.Gravity * GravityMultiplier * Time.deltaTime;
			movement.y = VerticalSpeed;

			//Moving left/right:
			LastMoveSpeedX = GetLeftRightMoveInput();
			if ((!IsOnLeftWall || LastMoveSpeedX > 0.0f) &&
				(!IsOnRightWall || LastMoveSpeedX < 0.0f))
			{
				movement.x = LastMoveSpeedX;
			}

			//Apply the movement:
			movement *= Time.deltaTime;
			HitsPerFace outHits;
			Move(ref movement, out outHits, MovementType);

			//Any actual movement will remove this object from a surface.
			if (movement.x < -0.0f)
				IsOnRightWall = false;
			else if (movement.x > 0.0f)
				IsOnLeftWall = false;
			if (movement.y < -0.0f)
				IsOnCeiling = false;
			else if (movement.y > 0.0f)
				IsOnFloor = false;


			//Mirror horizontally based on movement input.
			if (LastMoveSpeedX < 0.0f)
			{
				MyTr.localScale = new Vector3(-Math.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}
			else if (LastMoveSpeedX > 0.0f)
			{
				MyTr.localScale = new Vector3(Math.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}


			//Check whether we're still on any surfaces.
			const float epsilon = 0.001f;
			if (IsOnFloor)
			{
				Rect myBounds = MyCollRect;
				IsOnFloor = IsOnSurfaceY(myBounds, myBounds.yMin, -1, epsilon);

				if (!IsOnFloor)
					OnNoFloor();
			}

			if (IsOnCeiling)
			{
				Rect myBounds = MyCollRect;
				IsOnCeiling = IsOnSurfaceY(myBounds, myBounds.yMax, 1, epsilon);

				if (!IsOnCeiling)
					OnNoCeiling();
			}
			
			if (IsOnLeftWall)
			{
				Rect myBounds = MyCollRect;
				IsOnLeftWall = IsOnSurfaceX(myBounds, myBounds.xMin, -1, epsilon);

				if (!IsOnLeftWall)
					OnNoLeftWall();
			}

			if (IsOnRightWall)
			{
				Rect myBounds = MyCollRect;
				IsOnRightWall = IsOnSurfaceX(myBounds, myBounds.xMax, 1, epsilon);

				if (!IsOnRightWall)
					OnNoRightWall();
			}
		}
		protected abstract float GetLeftRightMoveInput();
		
		private bool IsOnSurfaceX(Rect myBounds, float boundsEdgeX, int dir, float epsilon)
		{
			if (Math.Abs(boundsEdgeX - Mathf.RoundToInt(boundsEdgeX)) < epsilon)
			{
				int x = Board.ToTilePosX(boundsEdgeX);
				int startY = Board.ToTilePosY(myBounds.yMin),
					endY = Board.ToTilePosY(myBounds.yMax);
				for (int y = startY; y <= endY; ++y)
					if (Board.IsSolid(new Vector2i(x + dir, y)))
						return true;
			}
			return false;
		}
		private bool IsOnSurfaceY(Rect myBounds, float boundsEdgeY, int dir, float epsilon)
		{
			if (Math.Abs(boundsEdgeY - Mathf.RoundToInt(boundsEdgeY)) < epsilon)
			{
				int y = Board.ToTilePosY(boundsEdgeY);
				int startX = Board.ToTilePosX(myBounds.xMin),
					endX = Board.ToTilePosX(myBounds.xMax);
				for (int x = startX; x <= endX; ++x)
					if (Board.IsSolid(new Vector2i(x, y + dir)))
						return true;
			}
			return false;
		}
	}
}