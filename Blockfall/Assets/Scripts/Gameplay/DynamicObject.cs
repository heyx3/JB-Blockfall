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
		private static List<DynamicObject> objectsInWorld = new List<DynamicObject>();

		public IEnumerable<DynamicObject> ObjectsInWorld { get { return objectsInWorld; } }

		protected static GameBoard.Board Board { get { return GameBoard.Board.Instance; } }


		public Vector2 CollisionBoxSize;
		public bool ClampXEnds = true,
					ClampYEnds = true;


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
			//Move.

			Vector2 oldPos = (Vector2)MyTr.position;
			Vector2 newPos = oldPos;

			Rect collRect = new Rect(Vector2.zero, CollisionBoxSize);
			collRect.center = oldPos;


			//Check for collisions with tiles.
			Vector2i tileMin, tileMax;
			GameBoard.Board.Instance.GetTileRange(collRect, out tileMin, out tileMax);

			//Along the X.
			if (true || MyVelocity.x != 0.0f)
			{
				float deltaX = MyVelocity.x * Time.deltaTime;
				Rect newCollRect = new Rect(Vector2.zero, CollisionBoxSize);
				newCollRect.center = new Vector2(newPos.x + deltaX, newPos.y);

				//Get the forward edge of the bounding box before and after movement.
				int forwardEdge, nextEdge, moveDir;
				if (MyVelocity.x < 0.0f)
				{
					forwardEdge = Board.ToTilePosX(collRect.xMin);
					nextEdge = Board.ToTilePosX(newCollRect.xMin);
					moveDir = -1;
				}
				else
				{
					forwardEdge = Board.ToTilePosX(collRect.xMax);
					nextEdge = Board.ToTilePosX(newCollRect.xMax);
					moveDir = 1;
				}

				//Get the closest block that is blocking the player's way along each row.
				Dictionary<int, int> ysToClosestXs = new Dictionary<int,int>(tileMax.y - tileMin.y + 1);
				for (int y = tileMin.y; y <= tileMax.y; ++y)
				{
					for (int x = forwardEdge; x != nextEdge + moveDir; x += moveDir)
					{
						Vector2i tilePos = new Vector2i(x, y);

						GameBoard.BlockTypes bType = Board[tilePos];
						bool shouldBreak = false;
						switch (bType)
						{
							case GameBoard.BlockTypes.Empty:
								break;

							case GameBoard.BlockTypes.Immobile:
							case GameBoard.BlockTypes.Normal:

								Rect tileRect = Board.ToWorldRect(tilePos);
								if (tileRect.Overlaps(newCollRect))
								{
									ysToClosestXs.Add(y, x);
									shouldBreak = true;
								}

								break;

							default: throw new NotImplementedException(bType.ToString() + ": {" + x + ", " + y + "}");
						}

						if (shouldBreak)
							break;
					}
				}

				if (ysToClosestXs.Count > 0)
				{
					//Get the closest single tile and collide with it.
					Vector2i closestTile = new Vector2i(int.MinValue, int.MinValue);
					foreach (KeyValuePair<int, int> yThenX in ysToClosestXs)
					{
						if (closestTile.x == int.MinValue ||
							(moveDir == 1 && closestTile.x > yThenX.Value) ||
							(moveDir == -1 && closestTile.x < yThenX.Value))
						{
							closestTile = new Vector2i(yThenX.Value, yThenX.Key);
						}
					}

					Rect tileRect = Board.ToWorldRect(closestTile);

					if (moveDir == -1)
					{
						newPos.x = tileRect.xMax + (CollisionBoxSize.x * 0.5f); //+ 0.0001f;
						OnHitLeftSide(closestTile, ref newPos);
					}
					else
					{
						newPos.x = tileRect.xMin - (CollisionBoxSize.x * 0.5f); // - 0.0001f;
						OnHitRightSide(closestTile, ref newPos);
					}
				}
				else
				{
					newPos.x += deltaX;
				}


				//Update the collision rectangle.
				collRect = new Rect(Vector2.zero, CollisionBoxSize);
				collRect.center = newPos;
			}

			//Along the Y.
			if (true || MyVelocity.y != 0.0f)
			{
				float deltaY = MyVelocity.y * Time.deltaTime;
				Rect newCollRect = new Rect(Vector2.zero, CollisionBoxSize);
				newCollRect.center = new Vector2(newPos.x, newPos.y + deltaY);

				//Get the forward edge of the bounding box before and after movement.
				int forwardEdge, nextEdge, moveDir;
				if (MyVelocity.y < 0.0f)
				{
					forwardEdge = Board.ToTilePosY(collRect.yMin);
					nextEdge = Board.ToTilePosY(newCollRect.yMin);
					moveDir = -1;
				}
				else
				{
					forwardEdge = Board.ToTilePosY(collRect.yMax);
					nextEdge = Board.ToTilePosY(newCollRect.yMax);
					moveDir = 1;
				}

				//Get the closest block that is blocking the player's way along each row.
				Dictionary<int, int> xsToClosestYs = new Dictionary<int,int>(tileMax.x - tileMin.x + 1);
				for (int x = tileMin.x; x <= tileMax.x; ++x)
				{
					for (int y = forwardEdge; y != nextEdge + moveDir; y += moveDir)
					{
						Vector2i tilePos = new Vector2i(x, y);

						GameBoard.BlockTypes bType = Board[tilePos];
						bool shouldBreak = false;
						switch (bType)
						{
							case GameBoard.BlockTypes.Empty:
								break;

							case GameBoard.BlockTypes.Immobile:
							case GameBoard.BlockTypes.Normal:

								Rect tileRect = Board.ToWorldRect(tilePos);
								if (tileRect.Overlaps(newCollRect))
								{
									xsToClosestYs.Add(x, y);
									shouldBreak = true;
								}

								break;

							default: throw new NotImplementedException(bType.ToString() + ": {" + x + ", " + y + "}");
						}

						if (shouldBreak)
							break;
					}
				}

				if (xsToClosestYs.Count > 0)
				{
					//Get the closest single tile and collide with it.
					Vector2i closestTile = new Vector2i(int.MinValue, int.MinValue);
					foreach (KeyValuePair<int, int> xThenY in xsToClosestYs)
					{
						if (closestTile.y == int.MinValue ||
							(moveDir == 1 && closestTile.y > xThenY.Value) ||
							(moveDir == -1 && closestTile.y < xThenY.Value))
						{
							closestTile = new Vector2i(xThenY.Key, xThenY.Value);
						}
					}

					Rect tileRect = Board.ToWorldRect(closestTile);

					if (moveDir == -1)
					{
						newPos.y = tileRect.yMax + (CollisionBoxSize.y * 0.5f); //+ 0.0001f;
						OnHitFloor(closestTile, ref newPos);
					}
					else
					{
						newPos.y = tileRect.yMin - (CollisionBoxSize.y * 0.5f); // - 0.0001f;
						OnHitCeiling(closestTile, ref newPos);
					}
				}
				else
				{
					newPos.y += deltaY;
				}


				//Update the collision rectangle.
				collRect = new Rect(Vector2.zero, CollisionBoxSize);
				collRect.center = newPos;
			}


			//Check for collisions with other DynamicObjects.
			for (int i = 0; i < objectsInWorld.Count; ++i)
			{
				if (this != objectsInWorld[i] && collRect.Overlaps(objectsInWorld[i].MyCollRect))
				{
					OnHitDynamicObject(objectsInWorld[i], ref newPos);
				}
			}


			collRect = MyCollRect;
			if (ClampXEnds)
			{
				Rect minBlock = Board.ToWorldRect(new Vector2i()),
					 maxBlock = Board.ToWorldRect(new Vector2i(Board.Width - 1, Board.Height - 1));

				if (collRect.xMin < minBlock.xMax)
				{
					float deltaX = minBlock.xMax - collRect.xMin;
					MyTr.position += new Vector3(deltaX, 0.0f, 0.0f);
					collRect = new Rect(collRect.x + deltaX, collRect.y, collRect.width, collRect.height);
				}
				if (collRect.xMax > maxBlock.xMin)
				{
					float deltaX = maxBlock.xMin - collRect.xMax;
					MyTr.position += new Vector3(deltaX, 0.0f, 0.0f);
					collRect = new Rect(collRect.x + deltaX, collRect.y, collRect.width, collRect.height);
				}
			}
			if (ClampYEnds)
			{
				Rect minBlock = Board.ToWorldRect(new Vector2i()),
					 maxBlock = Board.ToWorldRect(new Vector2i(Board.Width - 1, Board.Height - 1));

				if (collRect.yMin < minBlock.yMax)
				{
					float deltaY = minBlock.yMax - collRect.yMin;
					MyTr.position += new Vector3(0.0f, deltaY, 0.0f);
					collRect = new Rect(collRect.x, collRect.y + deltaY, collRect.width, collRect.height);
				}
				if (collRect.yMax > maxBlock.yMin)
				{
					float deltaX = maxBlock.xMin - collRect.xMax;
					MyTr.position += new Vector3(deltaX, 0.0f, 0.0f);
					collRect = new Rect(collRect.x + deltaX, collRect.y, collRect.width, collRect.height);
				}
			}


			MyTr.position = newPos;
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
		/// <param name="nextPos">
		/// The object's new position after movement/collision.
		/// By default, the object is moved so that its bounding box is flush with the wall that was hit.
		/// You can optionally change it by setting the value of this parameter.
		/// </param>
		public virtual void OnHitLeftSide(Vector2i wallPos, ref Vector2 nextPos) { }
		/// <summary>
		/// Called when this object hits a wall on its right.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		/// <param name="nextPos">
		/// The object's new position after movement/collision.
		/// By default, the object is moved so that its bounding box is flush with the wall that was hit.
		/// You can optionally change it by setting the value of this parameter.
		/// </param>
		public virtual void OnHitRightSide(Vector2i wallPos, ref Vector2 nextPos) { }
		/// <summary>
		/// Called when this object hits a wall above it.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		/// <param name="nextPos">
		/// The object's new position after movement/collision.
		/// By default, the object is moved so that its bounding box is flush with the tile that was hit.
		/// You can optionally change it by setting the value of this parameter.
		/// </param>
		public virtual void OnHitCeiling(Vector2i ceilingPos, ref Vector2 nextPos) { }
		/// <summary>
		/// Called when this object hits a wall below it.
		/// </summary>
		/// <param name="wallPos">The tile that was hit.</param>
		/// <param name="nextPos">
		/// The object's new position after movement/collision.
		/// By default, the object is moved so that its bounding box is flush with the tile that was hit.
		/// You can optionally change it by modifgyin the value of this parameter.
		/// </param>
		public virtual void OnHitFloor(Vector2i floorPos, ref Vector2 nextPos) { }

		/// <summary>
		/// Called when this object hits another one.
		/// Note that the other object will also have this method called on it,
		///     and the order is nondeterministic.
		/// </summary>
		/// <param name="nextPos">
		/// The object's current position after movement/collision.
		/// By default, nothing is done to it.
		/// You can optionally change it by modifying the value of this parameter.
		/// </param>
		public virtual void OnHitDynamicObject(DynamicObject other, ref Vector2 nextPos) { }


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