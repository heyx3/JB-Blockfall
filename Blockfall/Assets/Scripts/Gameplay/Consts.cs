using System;
using UnityEngine;


namespace Gameplay
{
	public class Consts : Singleton<Consts>
	{
		public float Gravity = -9.8f;
		public float FastFallAccel = 2.0f;

		public float PlayerSpeed = 5.0f;
		public float JumpSpeed = 5.0f;

		public int NJumps = 2;
	}
}