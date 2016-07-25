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
		protected static GameBoard.Board Board { get { return GameBoard.Board.Instance; } }


		protected static bool AllowsMovement(Vector2i tilePos, GameBoard.BlockTypes block)
		{
			return !Board.IsSolid(tilePos);
		}

		/// <summary>
		/// Tries to move the given collision bounds by the given amount.
		/// Modifies the value of "moveAmount" so that movement doesn't leave the bounds in a tile.
		/// Returns whether a tile was hit.
		/// </summary>
		/// <param name="nCastsPerSide">
		/// The number of raycasts to do along each axis.
		/// *Not including* the casts that are done at each corner of the box.
		/// </param>
		/// <param name="hitsPerFace">
		/// The faces that were hit.
		/// </param>
		public static bool TrySweep(Rect currentBounds, Vector2i nCastsPerSide,
									ref Vector2 moveAmount, out HitsPerFace hitsPerFace)
		{
			hitsPerFace = new HitsPerFace();

			Vector2 movementSign = new Vector2(moveAmount.x.Sign(), moveAmount.y.Sign());

			if (movementSign.x == 0.0f && movementSign.y == 0.0f)
				return false;


			Vector2 fromCorner = currentBounds.center +
							     (currentBounds.size * 0.5f).Mult(movementSign);
			float dist = moveAmount.magnitude;
			Vector2 moveAmountN = moveAmount / dist;

			bool wasHit = false;

			//Cast out from a specific corner based on the X and Y direction of velocity.
			if (movementSign.x != 0.0f || movementSign.y != 0.0f)
			{
				//Cast out from the corner along the velocity direction.
				GameBoard.Board.RaycastResult result = new GameBoard.Board.RaycastResult();
				if (Board.CastRay(new Ray2D(fromCorner, moveAmountN), AllowsMovement,
								  ref result, dist + 0.001f))
				{
					hitsPerFace.Add(result);
					wasHit = true;

					if (result.Hit.HitSides.HasXFace())
					{
						moveAmount.x = result.Hit.Pos.x - fromCorner.x - (movementSign.x * 0.0001f);

						dist = moveAmount.magnitude;
						moveAmountN = moveAmount / dist;
						movementSign.x = moveAmount.x.Sign();
					}
					if (result.Hit.HitSides.HasYFace())
					{
						moveAmount.y = result.Hit.Pos.y - fromCorner.y - (movementSign.y * 0.0001f);

						dist = moveAmount.magnitude;
						moveAmountN = moveAmount / dist;
						movementSign.y = moveAmount.y.Sign();
					}
				}
			}

			//Cast out from various points on the X face.
			if (movementSign.x != 0.0f)
			{
				float yMin = currentBounds.yMin,
					  yMax = currentBounds.yMax;
				float yIncrement = 1.0f / (nCastsPerSide.x + 1);

				GameBoard.Board.RaycastResult result = new GameBoard.Board.RaycastResult();
				for (float y = yMin + yIncrement; y <= yMax + 0.001f; y += yIncrement)
				{
					Vector2 rayStart = new Vector2(fromCorner.x, y);
					if (Board.CastRay(new Ray2D(rayStart, new Vector2(movementSign.x, 0.0f)),
									  AllowsMovement, ref result, dist + 0.001f))
					{
						if (hitsPerFace.Add(result))
						{
							wasHit = true;

							UnityEngine.Assertions.Assert.IsTrue(result.Hit.HitSides.HasXFace());
							UnityEngine.Assertions.Assert.IsFalse(result.Hit.HitSides.HasYFace());

							moveAmount.x = result.Hit.Pos.x - rayStart.x - (movementSign.x * 0.0001f);

							dist = moveAmount.magnitude;
							moveAmountN = moveAmount / dist;
							movementSign.x = moveAmount.x.Sign();
						}
					}
				}
			}

			//Cast out from various points on the Y face.
			if (movementSign.y != 0.0f)
			{
				float xMin = currentBounds.xMin,
					  xMax = currentBounds.xMax;
				float xIncrement = 1.0f / (nCastsPerSide.y + 1);

				GameBoard.Board.RaycastResult result = new GameBoard.Board.RaycastResult();
				for (float x = xMin + xIncrement; x <= xMax + 0.001f; x += xIncrement)
				{
					Vector2 rayStart = new Vector2(x, fromCorner.y);
					if (Board.CastRay(new Ray2D(rayStart, new Vector2(0.0f, movementSign.y)),
									  AllowsMovement, ref result, dist + 0.001f))
					{
						if (hitsPerFace.Add(result))
						{
							wasHit = true;

							UnityEngine.Assertions.Assert.IsTrue(result.Hit.HitSides.HasYFace());
							UnityEngine.Assertions.Assert.IsFalse(result.Hit.HitSides.HasXFace());

							moveAmount.y = result.Hit.Pos.y - rayStart.y - (movementSign.y * 0.0001f);

							dist = moveAmount.magnitude;
							moveAmountN = moveAmount / dist;
							movementSign.y = moveAmountN.y.Sign();
						}
					}
				}
			}

			return wasHit;
		}
		
		public struct HitsPerFace
		{
			public GameBoard.Board.RaycastResult? MinX, MinY, MaxX, MaxY;

			public bool Add(GameBoard.Board.RaycastResult h)
			{
				bool added = false;
				if (h.Hit.HitSides.Contains(Walls.MinX) &&
					(!MinX.HasValue || MinX.Value.Hit.Distance > h.Hit.Distance))
				{
					MinX = h;
					added = true;
				}
				else if (h.Hit.HitSides.Contains(Walls.MaxX) &&
						 (!MaxX.HasValue || MaxX.Value.Hit.Distance > h.Hit.Distance))
				{
					MaxX = h;
					added = true;
				}

				if (h.Hit.HitSides.Contains(Walls.MinY) &&
					(!MinY.HasValue || MinY.Value.Hit.Distance > h.Hit.Distance))
				{
					MinY = h;
					added = true;
				}
				else if (h.Hit.HitSides.Contains(Walls.MaxY) &&
						 (!MaxY.HasValue || MaxY.Value.Hit.Distance > h.Hit.Distance))
				{
					MaxY = h;
					added = true;
				}

				return added;
			}
			public GameBoard.Board.RaycastResult? Get(Walls w)
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


		private static List<DynamicObject> objectsInWorld = new List<DynamicObject>();
		public IEnumerable<DynamicObject> ActiveObjectsInWorld { get { return objectsInWorld; } }


		public Vector2 CollisionBoxSize;
		public bool ClampXEnds = true,
					ClampYEnds = true;

		public int NCollisionRaysXFace = 3,
				   NCollisionRaysYFace = 3;

		
		public Rect MyCollRect { get { Rect r = new Rect(Vector2.zero, CollisionBoxSize); r.center = MyTr.position; return r; } }

		public Transform MyTr { get; private set; }
		public SpriteRenderer MySpr { get; protected set; }
		

		protected virtual void Awake()
		{
			MyTr = transform;
			MySpr = GetComponent<SpriteRenderer>();
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
			//TODO: Replace with Unity's own collision system. Why did I even try to roll my own for this, lol
			//Check for collisions with other DynamicObjects.
			Rect collBnds = MyCollRect;
			for (int i = 0; i < objectsInWorld.Count; ++i)
				if (this != objectsInWorld[i] && collBnds.Overlaps(objectsInWorld[i].MyCollRect))
					OnHitDynamicObject(objectsInWorld[i]);
		}


		public enum MovementTypes
		{
			/// <summary>
			/// Apply the movement, then see if the object is intersecting with any solid tiles.
			/// If it is, use raycasts to constrain the movement so it doesn't pass into any solid objects.
			/// </summary>
			Normal,
			/// <summary>
			/// Always do raycasts to check for collision as this object moves.
			/// Useful for larger movements that may skip over a solid tile.
			/// </summary>
			Sweep,
			/// <summary>
			/// Don't check for collisions as this object moves.
			/// </summary>
			Teleport,
		}

		/// <summary>
		/// Tries to move this object the given amount.
		/// Returns whether a tile was hit during this movement.
		/// </summary>
		/// <param name="moveAmount">Constrains the value</param>
		/// <param name="hitsPerFace">The collision information for each face that this object collided with.</param>
		/// <param name="movementType">How the movement works.</param>
		public bool Move(ref Vector2 moveAmount, out HitsPerFace hitsPerFace, MovementTypes movementType)
		{
			bool didHit = false;

			switch (movementType)
			{
				case MovementTypes.Normal: {

					hitsPerFace = new HitsPerFace();

					Rect currentBnds = MyCollRect;
					
					//Get the position after the movement.
					Rect newBnds = new Rect(currentBnds.min + moveAmount, currentBnds.size);

					//If the new position touches any solid tiles, sweep.
					Vector2i boundsMinI = Board.ToTilePos(newBnds.min),
							 boundsMaxI = Board.ToTilePos(newBnds.max);
					for (Vector2i posI = boundsMinI; !didHit && posI.y <= boundsMaxI.y; ++posI.y)
						for (posI.x = boundsMinI.x; posI.x <= boundsMaxI.x; ++posI.x)
							if (Board.IsSolid(posI))
							{
								//Sweep.
								HitsPerFace faceHits = new HitsPerFace();
								if (TrySweep(currentBnds,
											 new Vector2i(NCollisionRaysXFace,
														  NCollisionRaysYFace),
											 ref moveAmount, out hitsPerFace))
								{
									//Raise events for the various walls that were hit.
									if (faceHits.MinX.HasValue)
										OnHitRightSide(faceHits.MinX.Value.Pos);
									if (faceHits.MaxX.HasValue)
										OnHitLeftSide(faceHits.MaxX.Value.Pos);
									if (faceHits.MinY.HasValue)
										OnHitCeiling(faceHits.MinY.Value.Pos);
									if (faceHits.MaxY.HasValue)
										OnHitFloor(faceHits.MaxY.Value.Pos);

									didHit = true;
									break;
								}
							}
				} break;
				case MovementTypes.Sweep: {
					if (TrySweep(MyCollRect, new Vector2i(NCollisionRaysXFace,
														  NCollisionRaysYFace),
								 ref moveAmount, out hitsPerFace))
					{
						//Raise events for the various walls that were hit.
						if (hitsPerFace.MinX.HasValue)
							OnHitRightSide(hitsPerFace.MinX.Value.Pos);
						if (hitsPerFace.MaxX.HasValue)
							OnHitLeftSide(hitsPerFace.MaxX.Value.Pos);
						if (hitsPerFace.MinY.HasValue)
							OnHitCeiling(hitsPerFace.MinY.Value.Pos);
						if (hitsPerFace.MaxY.HasValue)
							OnHitFloor(hitsPerFace.MaxY.Value.Pos);

						didHit = true;
					}
				} break;
				case MovementTypes.Teleport: {
					hitsPerFace = new HitsPerFace();
					didHit = false;
				} break;

				default:
					throw new NotImplementedException(movementType.ToString());
			}

			MyTr.position += (Vector3)moveAmount;
			return didHit;
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
	}
}