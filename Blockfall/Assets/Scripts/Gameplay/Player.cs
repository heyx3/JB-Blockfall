using System;
using UnityEngine;


namespace Gameplay
{
	public class Player : DynamicObject
	{
		public int InputIndex = InputManager.FirstKeyboardInput;
		public string ThrowPosChildName = "Throw Position";

		[NonSerialized]
		public int JumpsLeft;


		public bool IsOnGround { get; private set; }

		public GameBoard.BlockTypes HoldingBlock { get; private set; }

		public Transform ThrowPosIndicator { get; private set; }


		protected override void Awake()
		{
			base.Awake();

			HoldingBlock = GameBoard.BlockTypes.Empty;
			IsOnGround = false;
			JumpsLeft = 0;

			ThrowPosIndicator = MyTr.FindChild(ThrowPosChildName);
			UnityEngine.Assertions.Assert.IsNotNull(ThrowPosIndicator, ThrowPosChildName);
		}
		void Update()
		{
			InputManager.Values inputs = InputManager.Instance.Inputs[InputIndex],
							    inputsLastFrame = InputManager.Instance.InputsLastFrame[InputIndex];

			//Jump.
			if (inputs.Jump && !inputsLastFrame.Jump && JumpsLeft > 0)
			{
				JumpsLeft -= 1;
				MyVelocity = new Vector2(MyVelocity.x, Consts.Instance.JumpSpeed);
				IsOnGround = false;
			}

			//Action.
			if (inputs.Action && !inputsLastFrame.Action)
			{
				//If this player isn't holding a block, pick one up.
				if (HoldingBlock == GameBoard.BlockTypes.Empty)
				{
					//TODO: Implement.
				}
				//Otherwise, throw the block.
				else
				{
					GameObject thrownBlock = Instantiate(Consts.Instance.ThrownBlockPrefab);
					
					Transform tr = thrownBlock.transform;
					tr.position = ThrowPosIndicator.position;

					ThrownBlock tb = thrownBlock.GetComponent<ThrownBlock>();
					tb.BlockType = HoldingBlock;
					tb.MySpr.sprite = Board.GetSpriteForBlock(HoldingBlock);

					//Calculate the block's velocity.
					tb.MyVelocity = inputs.Move;
					if (tb.MyVelocity == Vector2.zero)
					{
						tb.MyVelocity = new Vector2(Mathf.Sign(MyTr.localScale.x), 0.0f);
					}
					tb.MyVelocity *= Consts.Instance.BlockThrowSpeed;
				}
			}


			//Update velocity.

			Vector2 newVel = MyVelocity;

			newVel.x = inputs.Move.x * Consts.Instance.PlayerSpeed;

			if (inputs.Move.y < 0.0f)
			{
				newVel.y += Consts.Instance.FastFallAccel * inputs.Move.y * Time.deltaTime;
			}

			MyVelocity = newVel;


			//Update mirroring.
			if (inputs.Move.x < 0.0f)
			{
				MyTr.localScale = new Vector3(-Mathf.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}
			else if (inputs.Move.x > 0.0f)
			{
				MyTr.localScale = new Vector3(Mathf.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}
		}
		protected override void FixedUpdate()
		{
			//Apply gravity.
			Vector2 newVel = MyVelocity;
			newVel.y += Consts.Instance.Gravity * Time.deltaTime;
			MyVelocity = newVel;

			//Update collision and see if we're on the ground.
			if (MyVelocity.y < 0.0f)
				IsOnGround = false;
			base.FixedUpdate();
		}


		public override void OnHitCeiling(Vector2i ceilingPos, ref Vector2 nextPos)
		{
			MyVelocity = new Vector2(MyVelocity.x, Mathf.Min(MyVelocity.y, 0.0f));
		}
		public override void OnHitFloor(Vector2i floorPos, ref Vector2 nextPos)
		{
			MyVelocity = new Vector2(MyVelocity.x, Mathf.Max(MyVelocity.y, 0.0f));
			IsOnGround = true;
			JumpsLeft = Consts.Instance.NJumps;
		}

		public override void OnHitDynamicObject(DynamicObject other, ref Vector2 nextPos)
		{
			//TODO: React to a thrown block.
		}
	}
}