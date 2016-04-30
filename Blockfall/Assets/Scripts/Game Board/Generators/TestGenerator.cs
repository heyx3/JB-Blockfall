using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameBoard.Generators
{
	/// <summary>
	/// Generates a flat ground of normal blocks and a ring of immobile blocks keeping players in the map.
	/// </summary>
	public class TestGenerator : BaseGenerator
	{
		public int NTilesX = 10,
				   NTilesY = 15;

		public int BlockHeight = 6;


		protected override void DoGeneration()
		{
			Board.Instance.Reset(new Vector2i(NTilesX, NTilesY),
				(v) =>
				{
					if (v.x == 0 || v.x == NTilesX - 1 ||
						v.y == 0 || v.y == NTilesY - 1)
					{
						return BlockTypes.Immobile;
					}
					else if (v.y > BlockHeight)
					{
						return BlockTypes.Empty;
					}
					else
					{
						return BlockTypes.Normal;
					}
				});
		}
	}
}