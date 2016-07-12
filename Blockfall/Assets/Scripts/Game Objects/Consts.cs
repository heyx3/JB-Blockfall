using System;
using UnityEngine;


namespace GameObjects
{
	public class Consts : Singleton<Consts>
	{
		public float Gravity = -9.8f;
		public float FastFallAccel = 50.0f,
                     SlowFallAccel = -50.0f;

		public float PlayerSpeed = 5.0f;
		public float JumpSpeed = 5.0f;

		public int NJumps = 2;

		public float BlockThrowSpeed = 15.0f;


		public GameObject ThrownBlockPrefab;

		public Sprite Sprite_Player_Stand, Sprite_Player_Walk, Sprite_Player_Jump, Sprite_Player_Fall,
					  Sprite_Player_StandBlock, Sprite_Player_WalkBlock, Sprite_Player_JumpBlock, Sprite_Player_FallBlock;
	}
}