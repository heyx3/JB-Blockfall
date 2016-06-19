using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Gameplay
{
	/// <summary>
	/// An object that collides with the game board and with other DynamicObjects.
	/// </summary>
	public abstract class DynamicObject : MonoBehaviour
	{
		private static bool AllowsMovement(Vector2i tilePos, GameBoard.BlockTypes block)
		{
			return !GameBoard.BlockQueries.IsSolid(block);
		}

		private static List<DynamicObject> objectsInWorld = new List<DynamicObject>();

		public IEnumerable<DynamicObject> ObjectsInWorld { get { return objectsInWorld; } }

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
				WallsMap<BoxRayHit> hitWalls = new WallsMap<BoxRayHit>();

				//Get the x and y face to cast from given the velocity.
				Vector2 fromCorner = collRect.center +
									 (collRect.size * 0.5f).Mult(new Vector2(MyVelocity.x.Sign(),
																			 MyVelocity.y.Sign()));

				//Cast out from the corner along the velocity direction.
				{
					BoxRayHit hit = new BoxRayHit();
					Vector2i tileHit = Board.CastRay(new Ray2D(fromCorner, velocityNorm),
													 AllowsMovement, ref hit, delta + 0.001f);
					if (tileHit != new Vector2i(-1, -1))
					{
						didHit = true;

						if (hit.IsXFace)
							actualMovement.x = hit.Pos.x - fromCorner.x - (MyVelocity.x.Sign() * 0.0001f);
						else
							actualMovement.y = hit.Pos.y - fromCorner.y - (MyVelocity.y.Sign() * 0.0001f);

						hitWalls.Set(hit.Wall, hit);
					}
				}

				//Cast out from the X face along the velocity's X direction.
				if (MyVelocity.x != 0.0f)
				{
					float velDir = MyVelocity.x.Sign();
					float yMin = collRect.yMin,
						  yMax = collRect.yMax;

					float yIncrement = 1.0f / NCollisionRaysPerUnit;
					BoxRayHit hit = new BoxRayHit();
					for (float y = yMin + yIncrement; y <= yMax; y += yIncrement)
					{
						Vector2 rayStart = new Vector2(fromCorner.x, y);
						Vector2i hitPos = Board.CastRay(new Ray2D(rayStart, new Vector2(velDir, 0.0f)),
														AllowsMovement, ref hit,
														delta + 0.001f);
						if (hitPos != new Vector2i(-1, -1))
						{
							if (!hitWalls.Contains(hit.Wall) ||
								hit.Distance < hitWalls.Get(hit.Wall).Distance)
							{
								didHit = true;
								actualMovement.x = hit.Pos.x - rayStart.x - (velDir * 0.0001f);
								hitWalls.Set(hit.Wall, hit);
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
					BoxRayHit hit = new BoxRayHit();
					for (float x = xMin + xIncrement; x <= xMax; x += xIncrement)
					{
						Vector2 rayStart = new Vector2(x, fromCorner.y);
						Vector2i hitPos = Board.CastRay(new Ray2D(rayStart, new Vector2(0.0f, velDir)),
														AllowsMovement, ref hit,
														delta + 0.001f);
						if (hitPos != new Vector2i(-1, -1))
						{
							if (!hitWalls.Contains(hit.Wall) ||
								hit.Distance < hitWalls.Get(hit.Wall).Distance)
							{
								didHit = true;
								actualMovement.y = hit.Pos.y - rayStart.y - (velDir * 0.0001f);
								hitWalls.Set(hit.Wall, hit);
							}
						}
					}
				}


				MyTr.position = (Vector3)((Vector2)MyTr.position + actualMovement);

				//If there was a collision with a surface, stop the velocity in that direction.
				if (didHit)
				{
					if (hitWalls.Contains(Walls.MinX))
					{
						MyVelocity = new Vector2(0.0f, MyVelocity.y);
						OnHitRightSide(Board.ToTilePos(hitWalls.Get(Walls.MinX).Pos));
					}
					else if (hitWalls.Contains(Walls.MaxX))
					{
						MyVelocity = new Vector2(0.0f, MyVelocity.y);
						OnHitLeftSide(Board.ToTilePos(hitWalls.Get(Walls.MaxX).Pos));
					}

					if (hitWalls.Contains(Walls.MinY))
					{
						MyVelocity = new Vector2(MyVelocity.x, 0.0f);
						OnHitCeiling(Board.ToTilePos(hitWalls.Get(Walls.MinY).Pos));
					}
					else if (hitWalls.Contains(Walls.MaxY))
					{
						MyVelocity = new Vector2(MyVelocity.x, 0.0f);
						OnHitFloor(Board.ToTilePos(hitWalls.Get(Walls.MaxY).Pos));
					}
				}
			}


			//Check for collisions with other DynamicObjects.
			Rect collBnds = MyCollRect;
			for (int i = 0; i < objectsInWorld.Count; ++i)
				if (this != objectsInWorld[i] && collBnds.Overlaps(objectsInWorld[i].MyCollRect))
					OnHitDynamicObject(objectsInWorld[i]);
		}

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


		protected void DoActionAfterTime(Action toDo, float time)
		{
			StartCoroutine(DoActionCoroutine(toDo, time));
		}
		private System.Collections.IEnumerator DoActionCoroutine(Action toDo, float time)
		{
			yield return new WaitForSeconds(time);
			toDo();
		}
	}
}