using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameBoard.Generators
{
	public class BoardGenerator_Test : BoardGenerator_Base
	{
		public int BlockHeight = 6;

		public override void Generate(Board b, Vector2i minCorner, Vector2 maxCorner)
		{
			for (Vector2i posI = minCorner; posI.y <= maxCorner.y; ++posI.y)
				for (posI.x = minCorner.x; posI.x <= maxCorner.x; ++posI.x)
					b[posI] = (posI.y < BlockHeight ? BlockTypes.Empty : BlockTypes.Normal);
		}
	}
}