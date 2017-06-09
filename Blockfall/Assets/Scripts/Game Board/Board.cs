using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assert = UnityEngine.Assertions.Assert;


namespace GameBoard
{
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
		public Vector2 ToMinCorner(Vector2i tilePos)
		{
			return new Vector2(tilePos.x, tilePos.y);
		}

		public int ToTilePosX(float x) { return Mathf.FloorToInt(x); }
		public int ToTilePosY(float y) { return Mathf.FloorToInt(y); }
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


		public bool Any(Vector2i regionMin, Vector2i regionMax,
						Func<Vector2i, BlockTypes, bool> predicate)
		{
			foreach (Vector2i pos in new Vector2i.Iterator(regionMin, regionMax + 1))
				if (predicate(pos, this[pos]))
					return true;
			return false;
		}
		public bool All(Vector2i regionMin, Vector2i regionMax,
						Func<Vector2i, BlockTypes, bool> predicate)
		{
			foreach (Vector2i pos in new Vector2i.Iterator(regionMin, regionMax + 1))
				if (!predicate(pos, this[pos]))
					return false;
			return true;
		}

		/// <summary>
		/// Finds all potential spawn positions for an object of the given size
		///     in the given inclusive range.
		/// The returned positions are the min corners (i.e. bottom-left)
		///     of each area that the given object can spawn at.
		/// </summary>
		public List<Vector2i> GetSpawnablePositions(Vector2i toSearchMin, Vector2i toSearchMax,
													Vector2i objectSize, bool mustSpawnOnGround,
													Func<Vector2i, BlockTypes, bool> isSpawnableIn)
		{
			Func<Vector2i, BlockTypes, bool> isBlockSolid = (posI, bType) => IsSolid(bType);

			Assert.IsTrue(toSearchMax.x > toSearchMin.x, "Xs are bad");
			Assert.IsTrue(toSearchMax.y > toSearchMin.y, "Ys are bad");

			Vector2i toSearchSize = toSearchMax - toSearchMin + new Vector2i(1, 1);

			List<Vector2i> poses = new List<Vector2i>();

			Vector2i inclusiveSearchMax = toSearchMax - objectSize + 1;
			foreach (Vector2i startObject in new Vector2i.Iterator(toSearchMin, inclusiveSearchMax + 1))
			{
				Vector2i endObject = startObject + objectSize - 1;

				//If this region is on solid ground (or doesn't have to be),
				//    and it doesn't touch any solid blocks itself,
				//    then it is a valid position.
				if ((!mustSpawnOnGround || All(startObject.LessY, endObject.LessY, isBlockSolid)) &&
					All(startObject, endObject, isSpawnableIn))
				{
					poses.Add(startObject);
				}
			}

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