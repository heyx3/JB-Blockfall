using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameBoard.Generators
{
	/// <summary>
	/// Uses two 1D perlin noise arrays -- 1 for each axis -- to generate clumps of blocks.
	/// </summary>
	public class PerlinGenerator : BaseGenerator
	{
		[Serializable]
		public class PerlinOctave
		{
			public float Scale = 1.0f;
			public float Weight = 1.0f;
		}


		public List<PerlinOctave> HorizontalNoise = new List<PerlinOctave>(),
								  VerticalNoise = new List<PerlinOctave>();
		public AnimationCurve HorizontalPower = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f),
							  VerticalPower = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);
		public float Threshold = 0.3f;

		public int Width = 45,
				   Height = 60;

		public bool WrapX;

		public float Seed = 5123.0f;


		public void GenerateNoise(out float[] horzLine, out float[] vertLine)
		{
			int width = (WrapX ? Width / 2 : Width);
			horzLine = new float[Width];
			for (int x = 0; x < width; ++x)
			{
				horzLine[x] = 0.0f;
				for (int i = 0; i < HorizontalNoise.Count; ++i)
				{
					float xPos = HorizontalNoise[i].Scale * (float)x;
					horzLine[x] += HorizontalNoise[i].Weight *
								   NoiseAlgos2D.SmootherNoise(new Vector2(xPos, Seed));
				}

				if (WrapX)
				{
					horzLine[width + x] = horzLine[x];
				}
			}

			vertLine = new float[Height];
			for (int y = 0; y < Height; ++y)
			{
				vertLine[y] = 0.0f;
				for (int i = 0; i < VerticalNoise.Count; ++i)
				{
					float yPos = VerticalNoise[i].Scale * (float)y;
					vertLine[y] += VerticalNoise[i].Weight *
								   NoiseAlgos2D.SmootherNoise(new Vector2(Seed, yPos));
				}
			}
		}

		protected override void DoGeneration()
		{
			float[] horzLine, vertLine;
			GenerateNoise(out horzLine, out vertLine);

			BlockTypes[,] blocks = new BlockTypes[Width, Height];

			for (int y = 0; y < Height; ++y)
			{
				for (int x = 0; x < Width; ++x)
				{
					if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
					{
						blocks[x, y] = BlockTypes.Immobile;
					}
					else
					{
						float value = Mathf.Pow(horzLine[x],
												HorizontalPower.Evaluate((float)x /
																		 (float)(Width - 1))) *
									  Mathf.Pow(vertLine[y],
												VerticalPower.Evaluate((float)y /
																	   (float)(Height - 1)));
						blocks[x, y] = (value > Threshold ?
											BlockTypes.Normal :
											BlockTypes.Empty);
					}
				}
			}

			Board.Instance.Reset(blocks);
		}
	}
}