using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameBoard.Generators
{
	/// <summary>
	/// Uses two 1D perlin noise arrays -- 1 for each axis -- to generate clumps of blocks.
	/// </summary>
	public class BoardGenerator_Perlin1D : BoardGenerator_Base
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
		public float NormalThreshold = 0.3f,
					 ImmobileThreshold = 0.6f;

		public bool MirrorX;

		public float Seed = 5123.0f;


		public void GenerateNoise(out float[] horzLine, out float[] vertLine,
								  Vector2i startInclusive, Vector2i endInclusive)
		{
			Vector2i range = endInclusive - startInclusive + new Vector2i(1, 1);

			horzLine = new float[range.x];
			vertLine = new float[range.y];


			int width = (MirrorX ? range.x / 2 : range.x);
			for (int x = 0; x < width; ++x)
			{
				horzLine[x] = 0.0f;
				for (int i = 0; i < HorizontalNoise.Count; ++i)
				{
					float xPos = HorizontalNoise[i].Scale * (float)(x + startInclusive.x);
					horzLine[x] += HorizontalNoise[i].Weight *
								   NoiseAlgos2D.SmootherNoise(new Vector2(xPos, Seed));
				}
			}
			if (MirrorX)
			{
				for (int x = 0; x < width; ++x)
					horzLine[width + x] = horzLine[x];
			}


			vertLine = new float[range.y];
			for (int y = 0; y < range.y; ++y)
			{
				vertLine[y] = 0.0f;
				for (int i = 0; i < VerticalNoise.Count; ++i)
				{
					float yPos = VerticalNoise[i].Scale * (float)(y + startInclusive.y);
					vertLine[y] += VerticalNoise[i].Weight *
								   NoiseAlgos2D.SmootherNoise(new Vector2(Seed, yPos));
				}
			}
		}

		public override void Generate(Board b, Vector2i minCorner, Vector2i maxCorner)
		{
			float[] horzLine, vertLine;
			GenerateNoise(out horzLine, out vertLine, minCorner, maxCorner);
			Vector2i range = maxCorner - minCorner + new Vector2i(1, 1);

			foreach (Vector2i posI in new Vector2i.Iterator(minCorner, maxCorner + 1))
			{
				if (posI.IsWithin(Vector2i.Zero, range - 1))
				{
					float value = Mathf.Pow(horzLine[posI.x],
											HorizontalPower.Evaluate((float)posI.x /
																	 (float)(range.x - 1))) *
								  Mathf.Pow(vertLine[posI.y],
											VerticalPower.Evaluate((float)posI.y /
																   (float)(range.y - 1)));
					b[posI] = (value > ImmobileThreshold ?
								   BlockTypes.Immobile :
								   (value > NormalThreshold ?
										BlockTypes.Normal :
										BlockTypes.Empty));
				}
				else
				{
					b[posI] = BlockTypes.Immobile;
				}
			}
		}
	}
}