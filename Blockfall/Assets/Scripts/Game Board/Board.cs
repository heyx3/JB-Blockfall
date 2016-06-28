using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assert = UnityEngine.Assertions.Assert;
using BoxRayHit = Raycasting.BoxRayHit;


namespace GameBoard
{
	[DisallowMultipleComponent]
	public abstract class BoardGenerator_Base
	{
		/// <summary>
		/// Generates tiles into the given board for the given range, inclusive.
		/// </summary>
		public abstract void Generate(Board b, Vector2i minCorner, Vector2 maxCorner);
	}


	public enum BlockTypes
	{
		Empty = 0,
		Normal,
		Immobile,

		Number_Of_Types
	}


	[RequireComponent(typeof(BoardGenerator_Base))]
	public abstract class Board : Singleton<Board>
	{
		/// <summary>
        /// Whether movement is blocked by the given block type.
        /// </summary>
        public static bool IsSolid(BlockTypes b)
        {
            switch (b)
            {
                case BlockTypes.Empty:
                    return false;
                case BlockTypes.Immobile:
                case BlockTypes.Normal:
                    return true;
                default:
                    throw new NotImplementedException(b.ToString());
            }
        }
        /// <summary>
        /// Whether the given block type can be picked up by a player.
        /// </summary>
        public static bool CanPickUp(BlockTypes b)
        {
            switch (b)
            {
                case BlockTypes.Empty:
                case BlockTypes.Immobile:
                    return false;
                case BlockTypes.Normal:
                    return true;
                default:
                    throw new NotImplementedException(b.ToString());
            }
        }


		public bool IsSolid(Vector2i tile) { return IsSolid(this[tile]); }
		public bool CanPickUp(Vector2i tile) { return CanPickUp(this[tile]); }

		
		public Sprite Sprite_ImmobileBlock,
					  Sprite_NormalBlock;

		public Camera GameCam;
		protected BoardGenerator_Base Generator { get; private set; }


		public Sprite GetSpriteFor(BlockTypes t)
		{
			switch (t)
			{
				case BlockTypes.Empty:
					return null;
				case BlockTypes.Immobile:
					return Sprite_ImmobileBlock;
				case BlockTypes.Normal:
					return Sprite_NormalBlock;
				default: throw new NotImplementedException(t.ToString());
			}
		}

		
		public Vector2 ToWorldPos(Vector2i tilePos)
		{
			return new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
		}

		public int ToTilePosX(float x) { return (int)x; }
		public int ToTilePosY(float y) { return (int)y; }
		public Vector2i ToTilePos(Vector2 worldPos)
		{
			return new Vector2i(ToTilePosX(worldPos.x), ToTilePosY(worldPos.y));
		}

		public float TileSize { get { return 1.0f; } }
		public Rect ToWorldRect(Vector2i tilePos)
		{
			Rect r = new Rect(Vector2.zero, new Vector2(TileSize, TileSize));
			r.center = ToWorldPos(tilePos);
			return r;
		}

		public void GetTileRange(Rect worldRegion, out Vector2i minCorner, out Vector2i maxCorner)
		{
			minCorner = ToTilePos(worldRegion.min);
			maxCorner = ToTilePos(worldRegion.max);
		}


		public List<Vector2i> GetSpawnablePositions(Vector2i toSearchMin, Vector2i toSearchMax, Vector2i objectSize,
													bool mustSpawnOnGround,
													Func<Vector2i, BlockTypes, bool> isSpawnableIn)
		{
			UnityEngine.Assertions.Assert.IsTrue(toSearchMax.x > toSearchMin.x, "Xs are bad");
			UnityEngine.Assertions.Assert.IsTrue(toSearchMax.y > toSearchMin.y, "Ys are bad");

			Vector2i toSearchSize = toSearchMax - toSearchMin + new Vector2i(1, 1);

			List<Vector2i> poses = new List<Vector2i>();
            object listLock = 1754235633;

            //Check evey row for valid spawn places.
            //Split this computation across threads to speed it up.
            ThreadedRunner.Run(4, toSearchSize.y, (startI, endI) =>
            {
                //Cut off any Y values where the given bounds stick out above the search area.
                endI = Math.Min(endI, toSearchMax.y - objectSize.y + 1);
				int endX = toSearchMax.x - objectSize.x + 1;

                //Go through every block this thread is supposed to cover.
                for (int j = startI; j <= endI; ++j)
                {
					int _y = j + toSearchMin.y;

                    for (int x = toSearchMin.x; x <= endX; ++x)
                    {
                        Vector2i startBounds = new Vector2i(x, _y),
                                 endBounds = startBounds + objectSize - new Vector2i(1, 1);

                        //See whether all blocks on this row are on the ground.
                        if (mustSpawnOnGround)
                        {
                            //Player can't be on the ground if he's at the bottom of the map!
                            if (_y == 0)
                                break;
                            
                            bool isGood = true;
                            for (Vector2i pos = startBounds.LessY; pos.x <= endBounds.x; ++pos.x)
                            {
                                if (!IsSolid(pos))
                                {
                                    isGood = false;
                                    break;
                                }
                            }
                            if (!isGood)
                                continue;
                        }

                        //See whether all blocks touched by the bounds are spawnable.
                        {
                            bool isGood = true;
                            for (Vector2i pos = startBounds; isGood && pos.y <= endBounds.y; ++pos.y)
                            {
                                for (pos.x = startBounds.x; pos.x <= endBounds.x; ++pos.x)
                                {
                                    if (IsSolid(pos) || !isSpawnableIn(pos, this[pos]))
                                    {
                                        isGood = false;
                                        break;
                                    }
                                }
                            }
                            if (!isGood)
                                continue;
                        }

                        //This is a valid place to spawn!
                        lock (listLock)
                        {
                            poses.Add(startBounds);
                        }
                    }
                }
            });

            return poses;
		}

		/// <summary>
		/// Returns whether there was a hit.
		/// </summary>
		public bool CastRay(Ray2D ray, Func<Vector2i, BlockTypes, bool> isBlockPassable,
							ref Vector2i outHitTile, ref Raycasting.BoxRayHit outHit, float maxDist)
		
		{
			float epsilon = 0.00001f;
			Vector2 rayInvDir = new Vector2(1.0f / ray.direction.x, 1.0f / ray.direction.y);

			Vector2i posI = ToTilePos(ray.origin);

			//Edge-case: the ray started in a solid block.
			if (IsInBoard(posI) && !isBlockPassable(posI, this[posI]))
			{
				//If the ray is facing towards the center of the block,
				//    cast backwards to find where it intersects the wall.
				//Otherwise, cast forward.
				float _dir = 1.0f;
				if (Vector2.Dot(ray.direction, ToWorldPos(posI) - ray.origin) > 0.0f)
					_dir = -1.0f;

				BoxRayHit dummyHit = new BoxRayHit();
				uint _nHits = Raycasting.BoxRayHit.Cast(new Ray2D(ray.origin, ray.direction * _dir),
														rayInvDir * _dir, ToWorldRect(posI),
														ref outHit, ref dummyHit);
				Assert.IsTrue(_nHits == 1, "Expected 1 hit instead of " + _nHits);

				outHit.Distance *= _dir;
				outHitTile = posI;
				return true;
			}

			BoxRayHit startHit = new BoxRayHit(),
					  endHit = new BoxRayHit();
			uint nHits;

			startHit.Pos = ray.origin;
			startHit.Distance = 0.0f;

			posI = ToTilePos(startHit.Pos);

			//Cast through each block until a solid one is hit or we pass through the grid.
			Rect tileBounds = ToWorldRect(posI);
			nHits = Raycasting.BoxRayHit.Cast(ray, rayInvDir, tileBounds, ref startHit, ref endHit);
			
			if (nHits == 1)
			{
				//Right now "startHit" contains the single hit, where the ray exited the tile.
				endHit = startHit;
				startHit = new BoxRayHit(Raycasting.Walls.MinX, 0.0f, ray.origin);
			}
			else
			{
				Assert.IsTrue(nHits == 2, "nHits is " + nHits.ToString());
			}

			while (startHit.Distance < maxDist)
			{
				if (IsInBoard(posI) && isBlockPassable(posI, this[posI]))
				{
					//Edge-case: the ray passes exactly through a corner.
					//In this case, the next block to cast through is *diagonal* to this one.
					bool edgeCase = false;
					if (endHit.Pos.x.IsNearEqual(tileBounds.xMin, epsilon))
					{
						if (endHit.Pos.y.IsNearEqual(tileBounds.yMin, epsilon))
						{
							posI = posI.LessX.LessY;
							edgeCase = true;
						}
						else if (endHit.Pos.y.IsNearEqual(tileBounds.yMax, epsilon))
						{
							posI = posI.LessX.MoreY;
							edgeCase = true;
						}
					}
					else if (endHit.Pos.x.IsNearEqual(tileBounds.xMax, epsilon))
					{
						if (endHit.Pos.y.IsNearEqual(tileBounds.yMin, epsilon))
						{
							posI = posI.MoreX.LessY;
							edgeCase = true;
						}
						else if (endHit.Pos.y.IsNearEqual(tileBounds.yMax, epsilon))
						{
							posI = posI.MoreX.MoreY;
							edgeCase = true;
						}
					}
					
					//Otherwise, move to an adjacent orthogonal block based on the edge that was hit.
					if (!edgeCase)
					{
						if (endHit.IsXFace)
						{
							if (ray.direction.x > 0.0f)
								posI = posI.MoreX;
							else
							{
								Assert.IsTrue(ray.direction.x < 0.0f, "Ray X dir is 0??");
								posI = posI.LessX;
							}
						}
						else
						{
							if (ray.direction.y > 0.0f)
								posI = posI.MoreY;
							else
							{
								Assert.IsTrue(ray.direction.y < 0.0f, "Ray Y dir is 0??");
								posI = posI.LessY;
							}
						}
					}
					tileBounds = ToWorldRect(posI);
					nHits = Raycasting.BoxRayHit.Cast(ray, rayInvDir, tileBounds, ref startHit, ref endHit);
					Assert.IsTrue(nHits > 0, "nHits is " + nHits);
				}
				else
				{
					outHit = startHit;
					outHitTile = posI;
					return true;
				}
			}
			
			//Nothing was hit.
			return false;
		}
		
		
		public abstract BlockTypes this[Vector2i tilePos] { get; set; }

		public abstract bool IsInBoard(Vector2i tilePos);

		/// <summary>
		/// Allows this board to constrain the given position of an object.
		/// For example, can be used to make objects wrap around the sides of the board.
		/// Default behavior: does not change the position at all.
		/// </summary>
		public virtual Vector2 ModifyPos(Vector2 inPos) { return inPos; }


		protected override void Awake()
		{
			base.Awake();

			Generator = GetComponent<BoardGenerator_Base>();
		}
	}
}