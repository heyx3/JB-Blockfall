using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assert = UnityEngine.Assertions.Assert;
using BoxRayHit = Raycasting.BoxRayHit;


namespace GameBoard
{
	[DisallowMultipleComponent]
	public abstract class BoardGenerator_Base : MonoBehaviour
	{
		/// <summary>
		/// Generates tiles into the given board for the given range, inclusive.
		/// </summary>
		public abstract void Generate(Board b, Vector2i minCorner, Vector2i maxCorner);
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


		public List<Vector2i> GetSpawnablePositions(Vector2i toSearchMin, Vector2i toSearchMax,
													Vector2i objectSize, bool mustSpawnOnGround,
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


		public struct RaycastResult { public Vector2i Pos; public GridCasting2D.Hit Hit; }

		public abstract bool CastRay(Ray2D ray, Func<Vector2i, BlockTypes, bool> isBlockPassable,
									 ref RaycastResult hitTileData, float maxDist);


		public abstract BlockTypes this[Vector2i tilePos] { get; set; }


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