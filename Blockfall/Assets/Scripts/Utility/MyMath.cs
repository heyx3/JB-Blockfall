using System;
using UnityEngine;


public static class MyMath
{
	public static bool IsNearZero(this float f, float error) { return Math.Abs(f) <= error; }
	public static bool IsNearEqual(this float f, float f2, float error) { return Math.Abs(f - f2) <= error; }


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
	/// The "outHitD" output parameters are either 0 or 1 --
	///     0 means the intersection was along the X face; 1 along the Y face.
	/// </summary>
	/// <param name="rayInvDir">The precomputed reciprocal of the ray's direction.</param>
	public static uint RaycastBox(Ray2D ray, Vector2 rayInvDir, Rect box,
								  ref float outHitT1, ref float outHitT2,
								  ref Vector2 outHitP1, ref Vector2 outHitP2,
								  ref int outHitD1, ref int outHitD2)
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


		//Find the closest two intersections.
		RBStruct p1 = new RBStruct(),
				 p2 = new RBStruct();
		p1.T = float.PositiveInfinity;
		p2.T = float.PositiveInfinity;
		RBStruct tempP = new RBStruct();

		const float epsilon = 0.0001f;

		//X faces.
		if (!ray.direction.x.IsNearZero(epsilon))
		{
			tempP.D = 0;

			//Min X face.
			tempP.T = (min.x - ray.origin.x) * rayInvDir.x;
			float y = (ray.direction.y * tempP.T) + ray.origin.y;
			if (tempP.T >= 0.0f && (y >= min.y && y <= max.y))
			{
				tempP.Pos = new Vector2(min.x, y);
				RaycastBoxHelper(ref p1, ref p2, tempP);
			}

			//Max X face.
			tempP.T = (max.x - ray.origin.x) * rayInvDir.x;
			y = (ray.direction.y * tempP.T) + ray.origin.y;
			if (tempP.T >= 0.0 && (y >= min.y && y <= max.y))
			{
				tempP.Pos = new Vector2(max.x, y);
				RaycastBoxHelper(ref p1, ref p2, tempP);
			}
		}
		//Y faces.
		if (!ray.direction.y.IsNearZero(epsilon))
		{
			tempP.D = 1;

			//Min Y face.
			tempP.T = (min.y - ray.origin.y) * rayInvDir.y;
			float x = (ray.direction.x * tempP.T) + ray.origin.x;
			if (tempP.T >= 0.0f && (x >= min.x && x <= max.x))
			{
				tempP.Pos = new Vector2(x, min.y);
				RaycastBoxHelper(ref p1, ref p2, tempP);
			}

			//Max Y face.
			tempP.T = (max.y - ray.origin.y) * rayInvDir.y;
			x = (ray.direction.x * tempP.T) + ray.origin.x;
			if (tempP.T >= 0.0f && (x >= min.x && x <= max.x))
			{
				tempP.Pos = new Vector2(x, max.y);
				RaycastBoxHelper(ref p1, ref p2, tempP);
			}
		}

		if (p1.T == float.PositiveInfinity)
		{
			return 0;
		}
		else
		{
			outHitT1 = p1.T;
			outHitP1 = p1.Pos;
			outHitD1 = p1.D;

			if (p2.T == float.PositiveInfinity || p2.T == p1.T)
			{
				return 1;
			}
			else
			{
				outHitT2 = p2.T;
				outHitP2 = p2.Pos;
				outHitD2 = p2.D;

				return 2;
			}
		}
	}
	private struct RBStruct { public float T; public int D;  public Vector2 Pos; }
	private static void RaycastBoxHelper(ref RBStruct p1, ref RBStruct p2, RBStruct newP)
	{
		if (p1.T > newP.T)
		{
			p2 = p1;
			p1 = newP;
		}
		else if (p2.T > newP.T)
		{
			p2 = newP;
		}
	}
}