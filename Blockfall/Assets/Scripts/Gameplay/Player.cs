using System;
using UnityEngine;


namespace Gameplay
{
	public class Player : DynamicObject
	{
		public int InputIndex = InputManager.FirstKeyboardInput;

		[NonSerialized]
		public int JumpsLeft;


		public bool IsOnGround { get; private set; }

		public GameBoard.BlockTypes HoldingBlock { get; private set; }


		protected override void Awake()
		{
			base.Awake();

			HoldingBlock = GameBoard.BlockTypes.Empty;
			IsOnGround = false;
			JumpsLeft = 0;
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
				//TODO: Implement.
			}


			//Update velocity.

			Vector2 newVel = MyVelocity;

			newVel.x = inputs.Move.x * Consts.Instance.PlayerSpeed;

			if (inputs.Move.y < 0.0f)
			{
				newVel.y += Consts.Instance.FastFallAccel * inputs.Move.y * Time.deltaTime;
			}

			MyVelocity = newVel;
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