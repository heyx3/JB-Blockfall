using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameBoard
{
	/// <summary>
	/// The 2D grid of tiles in a match.
	/// </summary>
	public class Board : Singleton<Board>
	{
		private struct Tile
		{
			public BlockTypes Type;
			public SpriteRenderer Spr;

			public Tile(BlockTypes type, SpriteRenderer spr) { Type = type; Spr = spr; }
		}


		public Sprite Sprite_ImmobileBlock,
					  Sprite_NormalBlock;
		public int SpriteLayer = 1;

        public int NThreads = 1;


		private Tile[,] tiles = null;
		private Transform tileContainer;


		public int Width { get { return tiles.GetLength(0); } }
		public int Height { get { return tiles.GetLength(1); } }
		public Vector2i Size { get { return new Vector2i(Width, Height); } }

		public Vector2 CenterWorldPos { get { return new Vector2(Width * 0.5f, Height * 0.5f); } }

		public float TileSize { get { return 1.0f; } }


		public Vector2 ToWorldPos(Vector2i tilePos)
		{
			return new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
		}
		public Rect ToWorldRect(Vector2i tilePos)
		{
			Rect r = new Rect(Vector2.zero, new Vector2(TileSize, TileSize));
			r.center = ToWorldPos(tilePos);
			return r;
		}

		public Vector2i ToTilePos(Vector2 worldPos)
		{
			return new Vector2i(ToTilePosX(worldPos.x), ToTilePosY(worldPos.y));
		}
		public int ToTilePosX(float x) { return (int)x; }
		public int ToTilePosY(float y) { return (int)y; }
		public void GetTileRange(Rect worldRegion, out Vector2i minCorner, out Vector2i maxCorner)
		{
			minCorner = ToTilePos(worldRegion.min);
			maxCorner = ToTilePos(worldRegion.max);
		}

		public void Reset(BlockTypes[,] _tiles)
		{
			//Remove the old sprites if they exist.
			if (tiles != null)
			{
				List<SpriteRenderer> sprs = new List<SpriteRenderer>(tiles.Length);
				foreach (Tile t in tiles)
					sprs.Add(t.Spr);
				SpritePool.Instance.DeallocateSprites(sprs);
			}
			
			//Create the new sprites.
			tiles = new Tile[_tiles.GetLength(0), _tiles.GetLength(1)];
			List<SpriteRenderer> newSprs = SpritePool.Instance.AllocateSprites(Width * Height, null,
																			   SpriteLayer, tileContainer,
																			   "Tile Sprite");

			//Place the sprites into the tiles.
			int i = 0;
			for (int y = 0; y < Height; ++y)
			{
				for (int x = 0; x < Width; ++x)
				{
					Vector2i pos = new Vector2i(x, y);

					newSprs[i].sprite = GetSpriteForBlock(_tiles[x, y]);
					tiles[x, y] = new Tile(_tiles[x, y], newSprs[i]);

					Transform tr = tiles[x, y].Spr.transform;
					tr.position = ToWorldPos(pos);

					i += 1;
				}
			}
		}
		public void Reset(Vector2i nTiles, Func<Vector2i, BlockTypes> tileFactory)
		{
			BlockTypes[,] tempArray = new BlockTypes[nTiles.x, nTiles.y];
			for (int y = 0; y < tempArray.GetLength(1); ++y)
				for (int x = 0; x < tempArray.GetLength(0); ++x)
					tempArray[x, y] = tileFactory(new Vector2i(x, y));

			Reset(tempArray);
		}

		/// <summary>
		/// Returns {-1, -1} if no position was hit.
		/// </summary>
		/// <param name="isBlockPassable">
		/// Should return "false" if the given block can stop a ray, and "true" if it can't.
		/// </param>
		/// <param name="outHitDir">
		/// If there was a hit, gets set to 0 if the hit was on the min/max X face,
		/// or 1 if on the min/max Y face.
		/// </param>
		/// <param name="outHitT">
		/// If there was a hit, gets set to the distance from the ray to the hit position.
		/// </param>
		/// <param name="outHitPos">
		/// If there was a hit, gets set to the position of the ray's hit.
		/// </param>
		public Vector2i CastRay(Ray2D ray, Func<Vector2i, BlockTypes, bool> isBlockPassable,
								ref Vector2 outHitPos, ref float outHitT, ref int outHitDir)
		{
			float epsilon = 0.001f;
			Vector2 rayDirEpsilon = ray.direction * epsilon;
			Vector2 rayInvDir = new Vector2(1.0f / ray.direction.x, 1.0f / ray.direction.y);

			Vector2i startPosI = ToTilePos(ray.origin);

			//Edge-case: the ray started in a solid block.
			if (startPosI.x >= 0 && startPosI.x < Width &&
				startPosI.y >= 0 && startPosI.y < Height &&
				!isBlockPassable(startPosI, this[startPosI]))
			{
				//If the ray is facing towards the center of the block,
				//    cast backwards to find where it intersects the wall.
				//Otherwise, cast forward.
				float _dir = 1.0f;
				if (Vector2.Dot(ray.direction, ToWorldPos(startPosI) - ray.origin) > 0.0f)
					_dir = -1.0f;

				float dummyT;
				Vector2 dummyP;
				int dummyD;
				uint _nHits = MyMath.RaycastBox(new Ray2D(ray.origin, ray.direction * _dir),
												rayInvDir * _dir,
												ToWorldRect(startPosI),
											    ref outHitT, ref dummyT,
												ref outHitPos, ref dummyP,
											    ref outHitDir, ref dummyD);
				UnityEngine.Assertions.Assert.IsTrue(_nHits == 1, "Expected 1 hit!");
				return startPosI;
			}


			//Get the ray's start and end point in the world box.
			Rect startWB = ToWorldRect(new Vector2i(0, 0)),
				 endWB = ToWorldRect(new Vector2i(Width - 1, Height - 1)),
				 worldBounds = Rect.MinMaxRect(startWB.xMin, startWB.yMin,
											   endWB.xMax, endWB.yMax);
			float startT = 0.0f,
				  endT = 0.0f;
			Vector2 startPos = Vector2.zero,
					endPos = Vector2.zero;
			int startD = -1,
				endD = -1;
			uint nHits = MyMath.RaycastBox(ray, rayInvDir, worldBounds,
										   ref startT, ref endT,
										   ref startPos, ref endPos,
										   ref startD, ref endD);
			//If there were no hits, the ray never touches the tile grid.
			if (nHits == 0)
			{
				return new Vector2i(-1, -1);
			}
			//If there was only one hit, the ray started inside the tile grid.
			else if (nHits == 1)
			{
				//Because there was only one hit, the "startX" variables right now actually store
				//    the *end* of the ray.

				endT = startT;
				endPos = startPos;
				endD = startD;

				startT = 0.0f;
				startPos = ray.origin;
				startD = 0;
			}


			//Now cast through each block until a solid one is hit or we pass through the grid.

			float t1 = float.NaN, t2 = float.NaN;
			Vector2 p1 = Vector2.zero, p2 = Vector2.zero;
			int d1 = -1, d2 = -1;
			nHits = MyMath.RaycastBox(ray, rayInvDir, ToWorldRect(startPosI),
								      ref t1, ref t2, ref p1, ref p2, ref d1, ref d2);
			UnityEngine.Assertions.Assert.IsTrue(nHits > 0, "nHits is 0??");

			//TODO: fix this loop.
			while (startPosI.x >= 0 && startPosI.x < Width &&
				   startPosI.y >= 0 && startPosI.y < Height)
			{
				if (isBlockPassable(currentPosI, this[currentPosI]))
				{
					//Move to the end of the block.
					currentT = endT + epsilon;
					currentPos = endPos + rayDirEpsilon;

					currentPosI = ToTilePos(currentPos);

					startT = endT - epsilon;
					startPos = endPos - rayDirEpsilon;
					startD = endD;
				}
				else
				{
					outHitT = t1;
					outHitPos = p1;
					outHitDir = d1;

					return startPosI;
				}
			}

			//Nothing was hit.
			return new Vector2i(-1, -1);
		}
		

		public BlockTypes this[Vector2i tileIndex]
		{
			get
			{
				if (tileIndex.x < 0 || tileIndex.y < 0 ||
					tileIndex.x >= Width || tileIndex.y >= Height)
				{
					return BlockTypes.Immobile;
				}

				return tiles[tileIndex.x, tileIndex.y].Type;
			}
		}

        
		public void RemoveBlockAt(Vector2i pos)
		{
			if (tiles[pos.x, pos.y].Type == BlockTypes.Empty)
			{
				Debug.LogError("A block doesn't exist at " + pos.ToString());
				return;
			}

			tiles[pos.x, pos.y].Type = BlockTypes.Empty;
			tiles[pos.x, pos.y].Spr.sprite = GetSpriteForBlock(BlockTypes.Empty);

			tiles[pos.x, pos.y].Spr.gameObject.SetActive(tiles[pos.x, pos.y].Spr.sprite != null);
		}
		public void AddBlockAt(Vector2i pos, BlockTypes type)
		{
			if (tiles[pos.x, pos.y].Type != BlockTypes.Empty)
			{
				Debug.LogError("A block already exists at " + pos.ToString());
				return;
			}

			tiles[pos.x, pos.y].Type = type;
			tiles[pos.x, pos.y].Spr.sprite = GetSpriteForBlock(type);

			tiles[pos.x, pos.y].Spr.gameObject.SetActive(tiles[pos.x, pos.y].Spr.sprite != null);
		}

		/// <summary>
		/// Gets the sprite that the tile at the given position should use.
		/// May return "null" if the tile should not be given a sprite.
		/// </summary>
		public Sprite GetSpriteForBlock(BlockTypes t)
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

        /// <summary>
        /// Gets all free spaces at least as lage as the given bounds.
        /// Returns the min corner of each such space.
        /// </summary>
        /// <param name="isSpawnableIn">Optional function that filters out spaces from being spawned in.</param>
        public List<Vector2i> GetSpawnablePositions(Vector2i boundsSize, bool groundOnly,
                                                    Func<Vector2i, BlockTypes, bool> isSpawnableIn = null)
        {
            if (isSpawnableIn == null)
                isSpawnableIn = (v, b) => true;

            List<Vector2i> poses = new List<Vector2i>();
            object listLock = 1754235633;

            //Check evey row for valid spawn places.
            //Split this computation across threads to speed it up.
            ThreadedRunner.Run(4, Height, (startI, endI) =>
            {
                //Cut off any Y values where the given bounds stick out above the map.
                endI = Math.Min(endI, Height - boundsSize.y);
                int endX = Width - boundsSize.x;

                //Go through every block this thread is supposed to cover.
                for (int y = startI; y <= endI; ++y)
                {
                    for (int x = 0; x <= endX; ++x)
                    {
                        Vector2i startBounds = new Vector2i(x, y),
                                 endBounds = startBounds + boundsSize - new Vector2i(1, 1);

                        //See whether all blocks on this row are on the ground.
                        if (groundOnly)
                        {
                            //Player can't be on the ground if he's at the bottom of the map!
                            if (y == 0)
                                break;
                            
                            bool isGood = true;
                            for (Vector2i pos = startBounds.LessY; pos.x <= endBounds.x; ++pos.x)
                            {
                                if (!BlockQueries.IsSolid(this[pos]))
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
                                    BlockTypes b = this[pos];
                                    if (BlockQueries.IsSolid(b) || !isSpawnableIn(pos, b))
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

   
		protected override void Awake()
		{
			base.Awake();

			tileContainer = new GameObject("Tile Sprites").transform;
		}
	}
}
