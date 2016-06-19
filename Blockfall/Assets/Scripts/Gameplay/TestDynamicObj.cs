using System;
using UnityEngine;


namespace Gameplay
{
	public class TestDynamicObj : DynamicObject
	{
		public Vector2 ConstantVelocity = Vector2.zero;


		void Update()
		{
			MyVelocity = ConstantVelocity;
		}

		public override void OnHitCeiling(Vector2i ceilingPos)
		{
			Debug.Log("Hit ceiling");
		}
		public override void OnHitFloor(Vector2i floorPos)
		{
			Debug.Log("Hit floor");
		}
		public override void OnHitLeftSide(Vector2i wallPos)
		{
			Debug.Log("Hit left side");
		}
		public override void OnHitRightSide(Vector2i wallPos)
		{
			Debug.Log("Hit right side");
		}

		public override void OnHitDynamicObject(DynamicObject other)
		{
			Debug.Log(gameObject.name + " hit " + other.gameObject.name);
		}
	}
}