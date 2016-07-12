using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


/// <summary>
/// Provides functionality for raycasting through grids of 2D AABB's.
/// </summary>
namespace GridCasting2D
{
	/// <summary>
	/// Walls of an AABB.
	/// Note that multiple values can be enabled at once, representing a corner.
	/// </summary>
	[Flags]
	public enum Walls
	{
		MinX = 1,
		MaxX = 2,
		MinY = 4,
		MaxY = 8,
	}

	public static class Queries
	{
		public static bool Contains(this Walls w, Walls w2) { return (w & w2) == w2; }
		public static Walls Add(this Walls w, Walls w2) { return (w | w2); }
		public static Walls Remove(this Walls w, Walls w2) { return w & ~w2; }
		
		public static Walls RemoveXs(this Walls w) { return w.Remove(Walls.MinX | Walls.MaxX); }
		public static Walls RemoveYs(this Walls w) { return w.Remove(Walls.MinY | Walls.MaxY); }

		public static IEnumerable<Walls> GetIndividual(this Walls w)
		{
			if (w.Contains(Walls.MinX))
				yield return Walls.MinX;
			if (w.Contains(Walls.MaxX))
				yield return Walls.MaxX;
			if (w.Contains(Walls.MinY))
				yield return Walls.MinY;
			if (w.Contains(Walls.MaxY))
				yield return Walls.MaxY;
		}

		public static bool HasXFace(this Walls w) { return (w & (Walls.MinX | Walls.MaxX)) != 0; }
		public static bool HasYFace(this Walls w) { return (w & (Walls.MinY | Walls.MaxY)) != 0; }

		public static bool IsFace(this Walls w) { return w == Walls.MinX || w == Walls.MinY || w == Walls.MaxX || w == Walls.MaxY; }
		public static bool IsCorner(this Walls w) { return w.HasXFace() && w.HasYFace(); }


		/// <summary>
		/// Casts a ray through this rectangle.
		/// Returns the number of hits:
		/// 0 means the ray didn't hit at all.
		/// 1 means the ray started inside the box, and "outHit1" contains the point where it exited.
		/// 2 means the ray passed through the box;
		///		"outHit1" contains the point where it entered
		///		and "outHit2" contains the point where it exited.
		/// </summary>
		public static uint Cast(this Rect box, Ray2D ray, ref Hit outHit1, ref Hit outHit2, float epsilon)
		{
			return box.Cast(ray, new Vector2(1.0f / ray.direction.x, 1.0f / ray.direction.y),
							ref outHit1, ref outHit2, epsilon);
		}
		/// <summary>
		/// Casts a ray through this rectangle.
		/// Returns the number of hits:
		/// 0 means the ray didn't hit at all.
		/// 1 means the ray started inside the box, and "outHit1" contains the point where it exited.
		/// 2 means the ray passed through the box;
		///		"outHit1" contains the point where it entered
		///		and "outHit2" contains the point where it exited.
		/// </summary>
		/// <param name="invRayDir">
		/// Must be equal to 1.0f / ray.direction.
		/// This can be expensive to constantly recompute,
		/// so this method signature allows it to be computed once then passed in.
		/// </param>
		public static uint Cast(this Rect box, Ray2D ray, Vector2 invRayDir,
								ref Hit outHit1, ref Hit outHit2, float epsilon)
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
			OrderedHitList hits = new OrderedHitList();
			Hit temp = new Hit();
			//Find the hits on the min/max face of each axis.
			if (!ray.direction.x.IsNearZero(epsilon))
			{
				//Min.
				temp.Distance = (min.x - ray.origin.x) * invRayDir.x;
				float y = (ray.direction.y * temp.Distance) + ray.origin.y;
				if (temp.Distance >= 0.0f && (y + epsilon >= min.y && y - epsilon <= max.y))
				{
					temp.HitSides = Walls.MinX;
					temp.Pos = new Vector2(min.x, y);
					hits.TryInsert(temp);
				}
				//Max.
				temp.Distance = (max.x - ray.origin.x) * invRayDir.x;
				y = (ray.direction.y * temp.Distance) + ray.origin.y;
				if (temp.Distance >= 0.0f && (y + epsilon >= min.y && y - epsilon <= max.y))
				{
					temp.HitSides = Walls.MaxX;
					temp.Pos = new Vector2(max.x, y);
					hits.TryInsert(temp);
				}
			}
			if (!ray.direction.y.IsNearZero(epsilon))
			{
				//Min.
				temp.Distance = (min.y - ray.origin.y) * invRayDir.y;
				float x = (ray.direction.x * temp.Distance) + ray.origin.x;
				if (temp.Distance >= 0.0f && (x + epsilon >= min.x && x - epsilon <= max.x))
				{
					temp.HitSides = Walls.MinY;
					temp.Pos = new Vector2(x, min.y);
					hits.TryInsert(temp);
				}
				//Max.
				temp.Distance = (max.y - ray.origin.y) * invRayDir.y;
				x = (ray.direction.x * temp.Distance) + ray.origin.x;
				if (temp.Distance >= 0.0f && (x + epsilon >= min.x && x - epsilon <= max.x))
				{
					temp.HitSides = Walls.MaxY;
					temp.Pos = new Vector2(x, max.y);
					hits.TryInsert(temp);
				}
			} 
			//If any two hits are identical, then a corner was actually hit.
			hits.FoldDuplicates();


			//Now find the closest intersections.

			if (hits.Count < 1)
				return 0;

			outHit1 = hits.H1;
			if (hits.Count < 2)
				return 1;

			outHit2 = hits.H2;
			return 2;
		}
		#region Helper struct for Cast()
		private struct OrderedHitList
		{
			public Hit H1, H2, H3, H4;
			public int Count { get; private set; }

			public Hit Get(int index)
			{
				Assert.IsTrue(index >= 0 && index < Count,
							  index.ToString() + "/" + Count.ToString());
				switch (index)
				{
					case 0: return H1;
					case 1: return H2;
					case 2: return H3;

					case 3:
					default:
						return H3;
				}
			}
			public void Remove(int index)
			{
				Assert.IsTrue(index >= 0 && index < Count,
							  index.ToString() + "/" + Count.ToString());
				Count -= 1;
				switch (index)
				{
					case 0: H1 = H2; H2 = H3; H3 = H4; break;
					case 1: H2 = H3; H3 = H4; break;
					case 2: H3 = H4; break;
					case 3:
					default:
						break;
				}
			}
			public void Change(int index, Hit newVal)
			{
				Assert.IsTrue(index >= 0 && index < Count,
							  index.ToString() + "/" + Count.ToString());
				switch (index)
				{
					case 0: H1 = newVal; break;
					case 1: H2 = newVal; break;
					case 2: H3 = newVal; break;
					case 3:
					default:
						H4 = newVal; break;
				}
			}
			public void TryInsert(Hit h)
			{
				if (Count < 1 || h.Distance < H1.Distance)
				{
					H4 = H3;
					H3 = H2;
					H2 = H1;
					H1 = h;
					Count += 1;
				}
				else if (Count < 2 || h.Distance < H2.Distance)
				{
					H4 = H3;
					H3 = H2;
					H2 = h;
					Count += 1;
				}
				else if (Count < 3 || h.Distance < H3.Distance)
				{
					H4 = H3;
					H3 = h;
					Count += 1;
				}
				else if (Count < 4 || h.Distance < H4.Distance)
				{
					H4 = h;
					Count += 1;
				}
			}
			public void FoldDuplicates()
			{
				for (int i = 0; i < Count - 1; ++i)
				{
					Hit hThis = Get(i),
						hNext = Get(i + 1);
					if (hThis.Distance == hNext.Distance)
					{
						Change(i, new Hit(hThis.HitSides.Add(hNext.HitSides),
										  hThis.Distance, hThis.Pos));
						Remove(i + 1);
						i -= 1;
					}
				}
			}
		}
		#endregion
	}


	public struct Hit
	{
		public Walls HitSides;
		public float Distance;
		public Vector2 Pos;


		public Hit(Walls hitSides, float dist, Vector2 pos) { HitSides = hitSides; Distance = dist; Pos = pos; }


		public override string ToString()
		{
			string str = "";
			if ((int)HitSides != 0)
			{
				str = "Walls: [";
				foreach (Walls w in HitSides.GetIndividual())
					str += w.ToString() + ", ";
				str = str.Substring(0, str.Length - 2) + "] ";
			}

			str += "Dist: " + Distance;
			str += " Pos: " + Pos;

			return str;
		}
	}

	
	/// <summary>
	/// Handles raycasting against a uniform axis-aliged grid of squares.
	/// </summary>
	/// <typeparam name="ExtraData">
	/// Extra data to go along with a successful raycast against a grid cell.
	/// </typeparam>
	public abstract class GridCaster<ExtraData>
		where ExtraData : struct
	{
		/// <summary>
		/// Casts the given ray through this grid.
		/// Returns whether a block was hit.
		/// </summary>
		/// <param name="outDataIfHit">Data specific to the grid cell that was hit.</param>
		/// <param name="limitRange">Optional region to constrain the ray to.</param>
		/// <param name="maxDist">
		/// Maximum distance the ray can travel.
		/// IMPORTANT NOTE: If this is PositiveInfinity and limitRange is null,
		///		there will be an infinite loop if the ray never collides!
		/// </param>
		public bool Cast(Ray2D ray, ref ExtraData outDataIfHit,
						 float maxDist, float epsilon, MyNullable<Region> limitRange)
		{
			return Cast(ray, new Vector2(1.0f / ray.direction.x, 1.0f / ray.direction.y),
						ref outDataIfHit, maxDist, epsilon, limitRange);
		}

		/// <summary>
		/// Casts the given ray through this grid.
		/// Returns whether a block was hit.
		/// </summary>
		/// <param name="outDataIfHit">Data specific to the grid cell that was hit.</param>
		/// <param name="limitRange">Optional region to constrain the ray to.</param>
		/// <param name="maxDist">
		/// Maximum distance the ray can travel.
		/// IMPORTANT NOTE: If this is PositiveInfinity and limitRange is null,
		///		there will be an infinite loop if the ray never collides!
		/// </param>
		/// <param name="rayInvDir">
		/// Must be equal to 1.0f / ray.direction.
		/// This can be expensive to constantly recompute,
		/// so this method signature allows it to be computed once then passed in.
		/// </param>
		public bool Cast(Ray2D ray, Vector2 rayInvDir, ref ExtraData outDataIfHit,
						 float maxDist, float epsilon, MyNullable<Region> limitRange)
		{
			Vector2i posI = ToGridCellIndex(ray.origin);

			//Edge-case: the ray started in a solid cell.
			if (!limitRange.HasValue || posI.IsWithin(limitRange.Value.Min, limitRange.Value.Max))
			{
				if (CastInitialCell(posI, ray, rayInvDir, ref outDataIfHit, epsilon))
					return true;
			}


			//Cast through each cell until a solid one is hit or we get too far away.

			Hit startH = new Hit(),
				endH = new Hit();
			uint nHits;

			//If we are casting in a limited region, limit "maxDist".
			if (limitRange.HasValue)
			{
				Region range = limitRange.Value;
				Rect minBnds = ToWorldBounds(range.Min),
					 maxBnds = ToWorldBounds(range.Max);
				Rect rangeBnds = Rect.MinMaxRect(minBnds.xMin, minBnds.yMin,
												 maxBnds.xMax, maxBnds.yMax);

				nHits = rangeBnds.Cast(ray, rayInvDir, ref startH, ref endH, epsilon);
				if (nHits == 0)
				{
					return false;
				}
				else if (nHits == 1)
				{
					maxDist = Math.Min(maxDist, startH.Distance);

					startH.Pos = ray.origin;
					startH.Distance = 0.0f;
				}
				else
				{
					Assert.AreEqual(2U, nHits, "nHits");
					maxDist = Math.Min(maxDist, endH.Distance);
				}
			}
			

			//Raycast the first cell.
			Rect cellBnds = ToWorldBounds(posI);
			nHits = cellBnds.Cast(ray, rayInvDir, ref startH, ref endH, epsilon);
			if (nHits == 1)
			{
				endH = startH;
				startH = new Hit(Walls.MinX, 0.0f, ray.origin);
			}

			//If this block was solid, exit. Otherwise, move to the next one.
			int count = 0;
			while (startH.Distance < maxDist)
			{
				count += 1;
				if (count > 500)
				{
					Assert.IsTrue(false, "Infinite loop in grid raycast");
					return false;
				}
				if (CastCell(posI, cellBnds, ray, startH, endH, ref outDataIfHit))
				{
					return true;
				}
				else
				{
					//Move to the next block.
					if (endH.HitSides.Contains(Walls.MinX))
						posI = posI.LessX;
					else if (endH.HitSides.Contains(Walls.MaxX))
						posI = posI.MoreX;
					if (endH.HitSides.Contains(Walls.MinY))
						posI = posI.LessY;
					else if (endH.HitSides.Contains(Walls.MaxY))
						posI = posI.MoreY;

					cellBnds = ToWorldBounds(posI);
					nHits = cellBnds.Cast(ray, rayInvDir, ref startH, ref endH, epsilon);
					Assert.AreEqual(2U, nHits,
									posI.ToString() + "; " + startH + "; " + endH + "; " +
										ray.origin.ToString(4) + "; " + ray.direction.ToString(4));
				}
			}


			//Nothing was hit.
			return false;
		}


		protected abstract Vector2i ToGridCellIndex(Vector2 worldPos);
		protected abstract Rect ToWorldBounds(Vector2i gridCellIndex);

		/// <summary>
		/// Performs a cast on the given grid cell that was hit by the given ray.
		/// Returns whether this cell was solid.
		/// </summary>
		/// <param name="cellBnds">The world bounds of this cell.</param>
		/// <param name="gridCellStart">The position where the ray enters the cell.</param>
		/// <param name="gridCellEnd">The position where the ray exits the cell.</param>
		/// <param name="outDataIfHit">Data to output to the caller about this cell, if it was hit.</param>
		protected abstract bool CastCell(Vector2i gridCellIndex, Rect cellBnds, Ray2D ray,
										 Hit gridCellStart, Hit gridCellEnd,
										 ref ExtraData outDataIfHit);
		/// <summary>
		/// A simpler version of "CastCell()" that is called on the cell containing the ray's origin.
		/// </summary>
		protected abstract bool CastInitialCell(Vector2i gridCellIndex, Ray2D ray, Vector2 invRayDir,
												ref ExtraData outDataIfHit, float epsilon);
	}


	/// <summary>
	/// Needed because of a Unity compiler bug with System.Nullable.
	/// </summary>
	public struct MyNullable<T>
	{
		public bool HasValue { get { return hasValue; } }
		public T Value { get { Assert.IsTrue(HasValue); return val; } }

		private bool hasValue;
		private T val;

		public MyNullable(T value) { hasValue = true; val = value; }
	}
	
	public struct Region
	{
		public Vector2i Min, Max;
		public Region(Vector2i min, Vector2i max) { Min = min; Max = max; }
	}
}