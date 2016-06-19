using System;
using UnityEngine;


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


/// <summary>
/// The result of a raycast hitting a box.
/// </summary>
public struct BoxRayHit
{
	public Walls Wall;
	public float Distance;
	public Vector2 Pos;
	
	public bool IsXFace { get { return Wall == Walls.MinX || Wall == Walls.MaxX; } }
	public bool IsYFace { get { return Wall == Walls.MinY || Wall == Walls.MaxY; } }

	public BoxRayHit(Walls wall, float dist, Vector2 pos) { Wall = wall; Distance = dist; Pos = pos; }


	public override string ToString()
	{
		return "Wall " + Wall.ToString() + ", Dist:" + Distance + "; Pos: " + Pos.ToString(3);
	}
}


public static class MyMath
{
	public static bool IsNearZero(this float f, float error) { return Math.Abs(f) <= error; }
	public static bool IsNearEqual(this float f, float f2, float error) { return Math.Abs(f - f2) <= error; }
	
	public static float Sign(this float f) { return (f > 0.0f ? 1.0f : (f < 0.0f ? -1.0f : 0.0f)); }
	public static int SignI(this float f) { return (f > 0.0f ? 1 : (f < 0.0f ? -1 : 0)); }


	public static Vector2 Mult(this Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }


	public static Rect Union(this Rect r, Rect r2) { return Rect.MinMaxRect(Math.Min(r.xMin, r2.xMin),
																			Math.Min(r.yMin, r2.yMin),
																			Math.Max(r.xMax, r2.xMax),
																			Math.Max(r.yMax, r2.yMax)); }

	public static string ToString(this Vector2 v, int nDecimals)
	{
		return "{" + Math.Round(v.x, nDecimals) + ", " + Math.Round(v.y, nDecimals) + "}";
	}


	/// <summary>
	/// Returns 0 if there were no intersections,
	///     1 if there was one intersection (i.e. the ray started inside the box),
	///     and 2 if there were two intersections.
	/// </summary>
	/// <param name="rayInvDir">
	/// The precomputed reciprocal of the ray's direction.
	/// This is taken as a parameter instead of computed so that it can be computed once for many ray-casts.
	/// </param>
	public static uint RaycastBox(Ray2D ray, Vector2 rayInvDir, Rect box,
								  ref BoxRayHit outHit1, ref BoxRayHit outHit2,
								  float epsilon = 0.00001f)
	{
		Vector2 min = box.min,
				max = box.max;
		

		//Exit early if the ray has no chance of hitting.
		if ((ray.direction.x < 0.0f && ray.origin.x < min.x) ||
			(ray.direction.x > 0.0f && ray.origin.x > max.x) ||
			(ray.direction.y < 0.0f && ray.origin.y < min.y) ||
			(ray.direction.y > 0.0f && ray.origin.y > max.y))
		{
			return 0;
		}


		//Find the closest two intersections out of all four faces of the box.
		//For reasons that become clear at the end of this method,
		//    we actually want to keep track of the *three* closest intersections.
		BoxRayHit h1 = new BoxRayHit(),
			      h2 = new BoxRayHit(),
				  h3 = new BoxRayHit(),
			      temp = new BoxRayHit();
		h1.Distance = float.PositiveInfinity;
		h2.Distance = float.PositiveInfinity;
		h3.Distance = float.PositiveInfinity;

		//X faces.
		if (!ray.direction.x.IsNearZero(epsilon))
		{
			//Min X face.
			temp.Distance = (min.x - ray.origin.x) * rayInvDir.x;
			float y = (ray.direction.y * temp.Distance) + ray.origin.y;
			if (temp.Distance >= 0.0f && (y + epsilon >= min.y && y - epsilon <= max.y))
			{
				temp.Wall = Walls.MinX;
				temp.Pos = new Vector2(min.x, y);
				InsertNewHit(ref h1, ref h2, ref h3, temp);
			}

			//Max X face.
			temp.Distance = (max.x - ray.origin.x) * rayInvDir.x;
			y = (ray.direction.y * temp.Distance) + ray.origin.y;
			if (temp.Distance >= 0.0 && (y + epsilon >= min.y && y - epsilon <= max.y))
			{
				temp.Wall = Walls.MaxX;
				temp.Pos = new Vector2(max.x, y);
				InsertNewHit(ref h1, ref h2, ref h3, temp);
			}
		}
		//Y faces.
		if (!ray.direction.y.IsNearZero(epsilon))
		{
			//Min Y face.
			temp.Distance = (min.y - ray.origin.y) * rayInvDir.y;
			float x = (ray.direction.x * temp.Distance) + ray.origin.x;
			if (temp.Distance >= 0.0f && (x + epsilon >= min.x && x - epsilon <= max.x))
			{
				temp.Wall = Walls.MinY;
				temp.Pos = new Vector2(x, min.y);
				InsertNewHit(ref h1, ref h2, ref h3, temp);
			}

			//Max Y face.
			temp.Distance = (max.y - ray.origin.y) * rayInvDir.y;
			x = (ray.direction.x * temp.Distance) + ray.origin.x;
			if (temp.Distance >= 0.0f && (x + epsilon >= min.x && x - epsilon <= max.x))
			{
				temp.Wall = Walls.MaxY;
				temp.Pos = new Vector2(x, max.y);
				InsertNewHit(ref h1, ref h2, ref h3, temp);
			}
		}


		//Now output the two closest intersections.

		if (h1.Distance == float.PositiveInfinity)
			return 0;

		outHit1 = h1;
		
		//If h1 and h2 are actually the same intersection
		//    (which can happen if the intersection is a corner),
		//    skip over h2.
		if (h1.Distance == h2.Distance)
			h2 = h3;
		
		if (h2.Distance == float.PositiveInfinity)
			return 1;

		outHit2 = h2;
		return 2;
	}
	private static void InsertNewHit(ref BoxRayHit p1, ref BoxRayHit p2,
									 ref BoxRayHit p3, BoxRayHit newP)
	{
		if (p1.Distance > newP.Distance)
		{
			p3 = p2;
			p2 = p1;
			p1 = newP;
		}
		else if (p2.Distance > newP.Distance)
		{
			p3 = p2;
			p2 = newP;
		}
		else if (p3.Distance > newP.Distance)
		{
			p3 = newP;
		}
	}
}