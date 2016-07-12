using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GridCasting2D;


namespace GameObjects
{
	/// <summary>
	/// An object that collides with the game board and with other DynamicObjects.
	/// </summary>
	public abstract class DynamicObject : MonoBehaviour
	{
		private static bool AllowsMovement(Vector2i tilePos, GameBoard.BlockTypes block)
		{
			return !Board.IsSolid(tilePos);
		}

		private static List<DynamicObject> objectsInWorld = new List<DynamicObject>();

		public IEnumerable<DynamicObject> ActiveObjectsInWorld { get { return objectsInWorld; } }

		protected static GameBoard.Board Board { get { return GameBoard.Board.Instance; } }


		public Vector2 CollisionBoxSize;
		public bool ClampXEnds = true,
					ClampYEnds = true;

		public uint NCollisionRaysPerUnit = 5;


		public Vector2 MyVelocity { get; set; }
		public Rect MyCollRect { get { Rect r = new Rect(Vector2.zero, CollisionBoxSize); r.center = MyTr.position; return r; } }

		public Transform MyTr { get; private set; }
		public SpriteRenderer MySpr { get; protected set; }
		

		protected virtual void Awake()
		{
			MyTr = transform;
			MySpr = GetComponent<SpriteRenderer>();
			MyVelocity = Vector2.zero;
		}
		protected virtual void OnEnable()
		{
			objectsInWorld.Add(this);
		}
		protected virtual void OnDisable()
		{
			objectsInWorld.Remove(this);
		}
		protected virtual void FixedUpdate()
		{
			//TODO: Only do the raycasting stuff if the player actually hit something at his new position, or if "Sweep" field is enabled.

			//Move and do collision detection.
			//Collision detection is done by casting several rays forward from the collision box
			//    along the player's velocity, and finding the closest collision.
			if (MyVelocity.x != 0.0f || MyVelocity.y != 0.0f)
			{
				Rect collRect = MyCollRect;

				float speed = MyVelocity.magnitude,
					  delta = speed * Time.deltaTime;
				Vector2 velocityNorm = MyVelocity / speed;
				Vector2 actualMovement = velocityNorm * delta;

				bool didHit = false;
				RaycastHelper closestHits = new RaycastHelper();

				//Get the x and y face to cast from given the velocity.
				Vector2 fromCorner = collRect.center +
									 (collRect.size * 0.5f).Mult(new Vector2(MyVelocity.x.Sign(),
																			 MyVelocity.y.Sign()));

				//Cast out from the corner along the velocity direction.
				{
					GameBoard.Board.RaycastResult result = new GameBoard.Board.RaycastResult();
					if (Board.CastRay(new Ray2D(fromCorner, velocityNorm), AllowsMovement,
									  ref result, delta + 0.001f))
					{
						didHit = true;

						if (result.Hit.HitSides.HasXFace())
							actualMovement.x = result.Hit.Pos.x - fromCorner.x - (MyVelocity.x.Sign() * 0.0001f);
						else
							actualMovement.y = result.Hit.Pos.y - fromCorner.y - (MyVelocity.y.Sign() * 0.0001f);

						closestHits.Add(result.Hit);
					}
				}

				//Cast out from the X face along the velocity's X direction.
				if (MyVelocity.x != 0.0f)
				{
					float velDir = MyVelocity.x.Sign();
					float yMin = collRect.yMin,
						  yMax = collRect.yMax;

					float yIncrement = 1.0f / NCollisionRaysPerUnit;
					GameBoard.Board.RaycastResult result = new GameBoard.Board.RaycastResult();
					for (float y = yMin + yIncrement; y <= yMax; y += yIncrement)
					{
						Vector2 rayStart = new Vector2(fromCorner.x, y);
						if (Board.CastRay(new Ray2D(rayStart, new Vector2(velDir, 0.0f)), AllowsMovement,
										  ref result, delta + 0.001f))
						{
							if (closestHits.Add(result.Hit))
							{
								didHit = true;
								actualMovement.x = result.Hit.Pos.x - rayStart.x - (velDir * 0.0001f);
							}
						}
					}
				}
				//Cast out from the Y face.
				if (MyVelocity.y != 0.0f)
				{
					//Pick the face whose normal points in the same direction as the velocity.
					float velDir = MyVelocity.y.Sign();
					float xMin = collRect.xMin,
						  xMax = collRect.xMax;

					float xIncrement = 1.0f / NCollisionRaysPerUnit;
					GameBoard.Board.RaycastResult result = new GameBoard.Board.RaycastResult();
					for (float x = xMin + xIncrement; x <= xMax; x += xIncrement)
					{
						Vector2 rayStart = new Vector2(x, fromCorner.y);
						if (Board.CastRay(new Ray2D(rayStart, new Vector2(0.0f, velDir)), AllowsMovement,
													ref result, delta + 0.001f))
						{
							if (closestHits.Add(result.Hit))
							{
								didHit = true;
								actualMovement.y = result.Hit.Pos.y - rayStart.y - (velDir * 0.0001f);
							}
						}
					}
				}


				MyTr.position = (Vector3)((Vector2)MyTr.position + actualMovement);

				//If there was a collision with a surface, stop the velocity in that direction.
				if (didHit)
				{
					if (closestHits.MinX.HasValue)
					{
						MyVelocity = new Vector2(0.0f, MyVelocity.y);
						OnHitRightSide(Board.ToTilePos(closestHits.MinX.Value.Pos));
					}
					else if (closestHits.MaxX.HasValue)
					{
						MyVelocity = new Vector2(0.0f, MyVelocity.y);
						OnHitLeftSide(Board.ToTilePos(closestHits.MaxX.Value.Pos));
					}

					if (closestHits.MinY.HasValue)
					{
						MyVelocity = new Vector2(MyVelocity.x, 0.0f);
						OnHitCeiling(Board.ToTilePos(closestHits.MinY.Value.Pos));
					}
					else if (closestHits.MaxY.HasValue)
					{
						MyVelocity = new Vector2(MyVelocity.x, 0.0f);
						OnHitFloor(Board.ToTilePos(closestHits.MaxY.Value.Pos));
					}
				}
			}


			//Check for collisions with other DynamicObjects.
			Rect collBnds = MyCollRect;
			for (int i = 0; i < objectsInWorld.Count; ++i)
				if (this != objectsInWorld[i] && collBnds.Overlaps(objectsInWorld[i].MyCollRect))
					OnHitDynamicObject(objectsInWorld[i]);
		}
		
		#region RaycastHelper class
		private struct RaycastHelper
		{
			public Hit? MinX, MinY, MaxX, MaxY;
			
			public bool Add(Hit h)
			{
				bool added = false;
				if (h.HitSides.Contains(Walls.MinX) &&
					(!MinX.HasValue || MinX.Value.Distance > h.Distance))
				{
					MinX = h;
					added = true;
				}
				else if (h.HitSides.Contains(Walls.MaxX) &&
						 (!MaxX.HasValue || MaxX.Value.Distance > h.Distance))
				{
					MaxX = h;
					added = true;
				}
				
				if (h.HitSides.Contains(Walls.MinY) &&
					(!MinY.HasValue || MinY.Value.Distance > h.Distance))
				{
					MinY = h;
					added = true;
				}
				else if (h.HitSides.Contains(Walls.MaxY) &&
						 (!MaxY.HasValue || MaxY.Value.Distance > h.Distance))
				{
					MaxY = h;
					added = true;
				}

				return added;
			}
			public Hit? Get(Walls w)
			{
				switch (w)
				{
					case Walls.MinX: return MinX;
					case Walls.MinY: return MinY;
					case Walls.MaxX: return MaxX;
					case Walls.MaxY: return MaxY;
					default: throw new ArgumentException(w.ToString());
				}
			}
		}
		#endregion

		protected virtual void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(transform.position, (Vector3)CollisionBoxSize);
		}


		/// <summary>
		/// Called when this object hits a wall on its left.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		public virtual void OnHitLeftSide(Vector2i wallPos) { }
		/// <summary>
		/// Called when this object hits a wall on its right.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		public virtual void OnHitRightSide(Vector2i wallPos) { }
		/// <summary>
		/// Called when this object hits a wall above it.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		public virtual void OnHitCeiling(Vector2i ceilingPos) { }
		/// <summary>
		/// Called when this object hits a wall below it.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		public virtual void OnHitFloor(Vector2i floorPos) { }

		/// <summary>
		/// Called when this object hits another one.
		/// Note that the other object will also have this method called on it,
		///     and the order is nondeterministic.
		/// </summary>
		public virtual void OnHitDynamicObject(DynamicObject other) { }
	}
}