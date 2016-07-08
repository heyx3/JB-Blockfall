using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


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
		
		[SerializeField]
		private int WidthToGenerate = 20,
					HeightToGenerate = 35;

		private Tile[,] tiles = null;
		private Transform tileContainer;



		public int Width { get { return tiles.GetLength(0); } }
		public int Height { get { return tiles.GetLength(1); } }
		public Vector2i Size { get { return new Vector2i(Width, Height); } }

		public Vector2 CenterWorldPos { get { return new Vector2(Width * 0.5f, Height * 0.5f); } }


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

					newSprs[i].sprite = GetSpriteFor(_tiles[x, y]);
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


		public override bool CastRay(Ray2D ray, Func<Vector2i, BlockTypes, bool> isBlockPassable,
									 ref Board.RaycastResult hitTileData, float maxDist)
		{
			//TODO: Implement.
		}

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

		public override bool IsInBoard(Vector2i tilePos)
		{
			return tilePos.IsWithin(Vector2i.Zero,
									new Vector2i(Width - 1, Height - 1));
		}


		protected override void Awake()
		{
			base.Awake();

			tileContainer = new GameObject("Tile Sprites").transform;

			tiles = new Tile[WidthToGenerate, HeightToGenerate];
			for (int y = 0; y < Height; ++y)
			{
				for (int x = 0; x < Width; ++x)
				{
					GameObject go = new GameObject(x.ToString() + " " + y);
					go.transform.position = ToWorldPos(new Vector2i(x, y));
					go.transform.parent = tileContainer;

					SpriteRenderer spr = go.AddComponent<SpriteRenderer>();

					tiles[x, y] = new Tile(BlockTypes.Empty, spr);
					this[new Vector2i(x, y)] = BlockTypes.Empty;
				}
			}

			Generator.Generate(this, new Vector2i(), new Vector2i(Width - 1, Height - 1));

			Rect areaBnds = ToWorldRect(Vector2i.Zero).Union(ToWorldRect(new Vector2i(Width - 1, Height - 1)));
			GameCam.transform.position = areaBnds.center;
			GameCam.orthographicSize = areaBnds.height * 0.5f;
		}
	}
}