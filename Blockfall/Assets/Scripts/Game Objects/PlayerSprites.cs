using System;
using System.Collections;
using UnityEngine;


namespace GameObjects
{
	[RequireComponent(typeof(Player))]
	[RequireComponent(typeof(SpriteRenderer))]
	public class PlayerSprites : MonoBehaviour
	{
		private static readonly float speedEpsilon = 0.05f;


		private Player ply;
		private SpriteRenderer spr;


		private Sprite Choose(Sprite withBlock, Sprite withoutBlock)
		{
			if (ply.HoldingBlock != GameBoard.BlockTypes.Empty)
				return withBlock;
			else
				return withoutBlock;
		}

		void Awake()
		{
			ply = GetComponent<Player>();
			spr = GetComponent<SpriteRenderer>();
		}
		void Update()
		{
			if (ply.MyVelocity.y > speedEpsilon)
			{
				spr.sprite = Choose(Consts.Instance.Sprite_Player_JumpBlock,
									Consts.Instance.Sprite_Player_Jump);
			}
			else if (ply.MyVelocity.y < -speedEpsilon)
			{
				spr.sprite = Choose(Consts.Instance.Sprite_Player_FallBlock,
									Consts.Instance.Sprite_Player_Fall);
			}
			else if (Mathf.Abs(ply.MyVelocity.x) > speedEpsilon)
			{
				spr.sprite = Choose(Consts.Instance.Sprite_Player_WalkBlock,
									Consts.Instance.Sprite_Player_Walk);
			}
			else
			{
				spr.sprite = Choose(Consts.Instance.Sprite_Player_StandBlock,
									Consts.Instance.Sprite_Player_Stand);
			}
		}
	}
}