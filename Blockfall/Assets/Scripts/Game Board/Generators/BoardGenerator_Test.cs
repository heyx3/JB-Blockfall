using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameBoard.Generators
{
	public class BoardGenerator_Test : BoardGenerator_Base
	{
		public int BlockHeight = 6;

		public override void Generate(Board b, Vector2i minCorner, Vector2i maxCorner)
		{
			foreach (Vector2i posI in new Vector2i.Iterator(minCorner, maxCorner + 1))
				b[posI] = (posI.y < BlockHeight ? BlockTypes.Empty : BlockTypes.Normal);
		}
	}
}