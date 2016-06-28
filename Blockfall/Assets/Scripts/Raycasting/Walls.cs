using System;
using Vector2 = UnityEngine.Vector2;


namespace Raycasting
{
	public enum Walls
	{
		MinX, MaxX,
		MinY, MaxY,
	}


	public struct WallsMap<T>
	{
		private const int B_MinX = 1,
						  B_MinY = 2,
						  B_MaxX = 4,
						  B_MaxY = 8;

		private int keyMask;
		private T vMinX, vMinY, vMaxX, vMaxY;


		public bool Contains(Walls w)
		{
			switch (w)
			{
				case Walls.MinX: return (keyMask & B_MinX) > 0;
				case Walls.MinY: return (keyMask & B_MinY) > 0;
				case Walls.MaxX: return (keyMask & B_MaxX) > 0;
				case Walls.MaxY: return (keyMask & B_MaxY) > 0;
				default: throw new NotImplementedException(w.ToString());
			}
		}
		public T Get(Walls w)
		{
			switch (w)
			{
				case Walls.MinX: return vMinX;
				case Walls.MinY: return vMinY;
				case Walls.MaxX: return vMaxX;
				case Walls.MaxY: return vMaxY;
				default: throw new NotImplementedException(w.ToString());
			}
		}
		public void Set(Walls w, T t)
		{
			switch (w)
			{
				case Walls.MinX: vMinX = t; keyMask |= B_MinX; return;
				case Walls.MinY: vMinY = t; keyMask |= B_MinY; return;
				case Walls.MaxX: vMaxX = t; keyMask |= B_MaxX; return;
				case Walls.MaxY: vMaxY = t; keyMask |= B_MaxY; return;
				default: throw new NotImplementedException(w.ToString());
			}
		}
		public void Remove(Walls w)
		{
			switch (w)
			{
				case Walls.MinX: keyMask &= ~B_MinX; return;
				case Walls.MinY: keyMask &= ~B_MinY; return;
				case Walls.MaxX: keyMask &= ~B_MaxX; return;
				case Walls.MaxY: keyMask &= ~B_MaxY; return;
				default: throw new NotImplementedException(w.ToString());
			}
		}
	}
}