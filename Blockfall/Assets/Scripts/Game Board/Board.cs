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
		

		public BlockTypes this[Vector2i tileIndex]
		{
			get { return tiles[tileIndex.x, tileIndex.y].Type; }
		}

		
		/// <summary>
		/// This operation fails if a block doesn't exist at the given position.
		/// </summary>
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
		/// <summary>
		/// This operation fails if a block already exists at the given position.
		/// </summary>
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


		protected override void Awake()
		{
			base.Awake();

			tileContainer = new GameObject("Tile Sprites").transform;
		}
	}
}
