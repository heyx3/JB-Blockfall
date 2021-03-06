using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using GridCasting2D;

using CastRegion = GridCasting2D.Region;
using NullableCastRegion = GridCasting2D.MyNullable<GridCasting2D.Region>;


namespace GameBoard
{
	/// <summary>
	/// The 2D grid of tiles in a match.
	/// </summary>
	public class Board_Finite : Board
	{
		private struct Tile
		{
			public BlockTypes Type;
			public SpriteRenderer Spr;

			public Tile(BlockTypes type, SpriteRenderer spr) { Type = type; Spr = spr; }
		}


		public int SpriteLayer = 1;

		public int WidthToGenerate = 20,
				   HeightToGenerate = 35;

		private Tile[,] tiles = null;
		private Transform tileContainer;



		public int Width { get { return tiles.GetLength(0); } }
		public int Height { get { return tiles.GetLength(1); } }
		public Vector2i Size { get { return new Vector2i(Width, Height); } }

		public Vector2 CenterWorldPos { get { return new Vector2(Width * 0.5f, Height * 0.5f); } }


		public bool IsInBoard(Vector2i tilePos)
		{
			return tilePos.IsWithin(Vector2i.Zero,
									new Vector2i(Width - 1, Height - 1));
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
			foreach (Vector2i pos in tiles.AllIndices())
			{
				newSprs[i].sprite = GetSpriteFor(_tiles.Get(pos));
				tiles.Set(pos, new Tile(_tiles.Get(pos), newSprs[i]));

				Transform tr = tiles.Get(pos).Spr.transform;
				tr.position = ToWorldPos(pos);

				i += 1;
			}
		}
		public void Reset(Vector2i nTiles, Func<Vector2i, BlockTypes> tileFactory)
		{
			BlockTypes[,] tempArray = new BlockTypes[nTiles.x, nTiles.y];
			foreach (Vector2i pos in tempArray.AllIndices())
				tempArray.Set(pos, tileFactory(pos));

			Reset(tempArray);
		}


		public override bool CastRay(Ray2D ray, Func<Vector2i, BlockTypes, bool> isBlockPassable,
									 ref RaycastResult hitTileData, float maxDist)
		{
			RayHelper.IsBlockPassable = isBlockPassable;
			return RayHelper.Cast(ray, ref hitTileData, maxDist,
								  0.00001f, new NullableCastRegion());
		}
		#region RaycastHelper class
		private class RaycastHelper : GridCasting2D.GridCaster<RaycastResult>
		{
			public Board_Finite Owner;
			public Func<Vector2i, BlockTypes, bool> IsBlockPassable = null;

			public RaycastHelper(Board_Finite owner) { Owner = owner; }

			protected override Vector2i ToGridCellIndex(Vector2 worldPos) { return Owner.ToTilePos(worldPos); }
			protected override Rect ToWorldBounds(Vector2i gridCellIndex) { return Owner.ToWorldRect(gridCellIndex); }

			protected override bool CastInitialCell(Vector2i gridCellIndex, Ray2D ray, Vector2 invRayDir,
													ref RaycastResult outDataIfHit, float epsilon)
			{
				if (!IsBlockPassable(gridCellIndex, Owner[gridCellIndex]))
				{
					outDataIfHit.Pos = gridCellIndex;

					GridCasting2D.Hit hit1 = new GridCasting2D.Hit(),
									  hit2 = new GridCasting2D.Hit();
					uint nHits = Owner.ToWorldRect(gridCellIndex).Cast(ray, invRayDir, ref hit1, ref hit2, epsilon);
					if (nHits == 1)
						outDataIfHit.Hit = hit1;
					else if (nHits == 2)
						outDataIfHit.Hit = hit2;
					else
						Assert.IsTrue(false, nHits.ToString());

					return true;
				}

				return false;
			}
			protected override bool CastCell(Vector2i gridCellIndex, Rect cellBnds, Ray2D ray,
											  GridCasting2D.Hit gridCellStart,
											  GridCasting2D.Hit gridCellEnd,
											  ref RaycastResult outDataIfHit)
			{
				if (!IsBlockPassable(gridCellIndex, Owner[gridCellIndex]))
				{
					outDataIfHit.Hit = gridCellStart;
					outDataIfHit.Pos = gridCellIndex;
					return true;
				}

				return false;
			}
		}
		#endregion
		private RaycastHelper raycastHelper = null;
		private RaycastHelper RayHelper { get { if (raycastHelper == null) raycastHelper = new RaycastHelper(this); return raycastHelper; } }


		public override BlockTypes this[Vector2i tileIndex]
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
			set
			{
				Assert.IsTrue(IsInBoard(tileIndex), "Trying to use non-existent tile " + tileIndex);

				if (value == this[tileIndex])
					return;

				Tile currentT = tiles[tileIndex.x, tileIndex.y];
				tiles[tileIndex.x, tileIndex.y] = new Tile(value, currentT.Spr);

				Sprite spr = GetSpriteFor(value);
				if (spr == null)
				{
					currentT.Spr.gameObject.SetActive(false);
				}
				else
				{
					currentT.Spr.gameObject.SetActive(true);
					currentT.Spr.sprite = spr;
				}
			}
		}


		protected override void Awake()
		{
			base.Awake();

			tileContainer = new GameObject("Tile Sprites").transform;

			tiles = new Tile[WidthToGenerate, HeightToGenerate];
			foreach (Vector2i pos in tiles.AllIndices())
			{
				GameObject go = new GameObject(pos.ToString());
				go.transform.position = ToWorldPos(pos);
				go.transform.parent = tileContainer;

				SpriteRenderer spr = go.AddComponent<SpriteRenderer>();

				tiles.Set(pos, new Tile(BlockTypes.Empty, spr));
				this[pos] = BlockTypes.Empty;
			}

			Generator.Generate(this, new Vector2i(), new Vector2i(Width - 1, Height - 1));

			Rect areaBnds = ToWorldRect(Vector2i.Zero).Union(ToWorldRect(new Vector2i(Width - 1, Height - 1)));
			GameCam.transform.position = areaBnds.center;
			GameCam.orthographicSize = areaBnds.height * 0.5f;
		}
	}
}