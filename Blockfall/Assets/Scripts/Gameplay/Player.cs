using System;
using System.Collections.Generic;
using UnityEngine;


namespace Gameplay
{
	public class Player : DynamicObject
	{
		public int InputIndex = InputManager.FirstKeyboardInput;
		public string BlockHoldChildName = "Block Hold Position",
					  GrabForwardsChildName = "Grab Forwards Pos",
					  GrabBackwardsChildName = "Grab Backwards Pos",
					  GrabAboveChildName = "Grab Above Pos",
					  GrabBelowChildName = "Grab Below Pos",
					  GrabForwardsAboveChildName = "Grab Forwards/Above Pos",
					  GrabBackwardsAboveChildName = "Grab Backwards/Above Pos",
					  GrabForwardsBelowChildName = "Grab Forwards/Below Pos",
					  GrabBackwardsBelowChildName = "Grab Backwards/Below Pos";

		[NonSerialized]
		public int JumpsLeft;


		public bool IsOnGround { get; private set; }

		public GameBoard.BlockTypes HoldingBlock { get; private set; }
		
		public Transform BlockHoldIndicator { get; private set; }
		public Transform GrabForwardsIndicator { get; private set; }
		public Transform GrabBackwardsIndicator { get; private set; }
		public Transform GrabAboveIndicator { get; private set; }
		public Transform GrabBelowIndicator { get; private set; }
		public Transform GrabForwardsAboveIndicator { get; private set; }
		public Transform GrabForwardsBelowIndicator { get; private set; }
		public Transform GrabBackwardsAboveIndicator { get; private set; }
		public Transform GrabBackwardsBelowIndicator { get; private set; }


		/// <summary>
		/// Gets the "grab" indicators to use to search for grabbable blocks,
		///     in the order they should be searched in.
		/// </summary>
		/// <param name="moveInput">The sign of the player's X and Y movement input axes.</param>
		private IEnumerable<Transform> GetIndicatorsToSearch(int moveYInput, bool movingSideInput)
		{
			//If the player is only pressing "up"/"down", that takes priority.
			if (moveYInput < 0 && !movingSideInput)
			{
				yield return GrabBelowIndicator;
				yield return GrabForwardsIndicator;
				yield return GrabForwardsAboveIndicator;
				yield return GrabForwardsBelowIndicator;
				yield return GrabBackwardsIndicator;
				yield return GrabBackwardsAboveIndicator;
				yield return GrabBackwardsBelowIndicator;
				yield return GrabBelowIndicator;
			}
			else if (moveYInput > 0 && !movingSideInput)
			{
				yield return GrabAboveIndicator;
				yield return GrabForwardsIndicator;
				yield return GrabForwardsBelowIndicator;
				yield return GrabForwardsAboveIndicator;
				yield return GrabBackwardsIndicator;
				yield return GrabBackwardsBelowIndicator;
				yield return GrabBackwardsAboveIndicator;
				yield return GrabAboveIndicator;
			}
			//Otherwise, start at the front and work around to the back of the player.
			else
			{
				yield return GrabForwardsIndicator;
				yield return GrabBelowIndicator;
				yield return GrabForwardsBelowIndicator;
				yield return GrabAboveIndicator;
				yield return GrabForwardsAboveIndicator;
				yield return GrabBackwardsIndicator;
				yield return GrabBackwardsBelowIndicator;
				yield return GrabBackwardsAboveIndicator;
			}
		}

		private Transform TryGetChild(string name)
		{
			Transform tr = MyTr.FindChild(name);
			UnityEngine.Assertions.Assert.IsNotNull(tr, "Child named \"" + name + "\" does not exist in player");
			return tr;
		}
		protected override void Awake()
		{
			base.Awake();

			HoldingBlock = GameBoard.BlockTypes.Empty;
			IsOnGround = false;
			JumpsLeft = 0;

			BlockHoldIndicator = TryGetChild(BlockHoldChildName);
			GrabForwardsIndicator = TryGetChild(GrabForwardsChildName);
			GrabBackwardsIndicator = TryGetChild(GrabBackwardsChildName);
			GrabAboveIndicator = TryGetChild(GrabAboveChildName);
			GrabBelowIndicator = TryGetChild(GrabBelowChildName);
			GrabForwardsAboveIndicator = TryGetChild(GrabForwardsAboveChildName);
			GrabForwardsBelowIndicator = TryGetChild(GrabForwardsBelowChildName);
			GrabBackwardsAboveIndicator = TryGetChild(GrabBackwardsAboveChildName);
			GrabBackwardsBelowIndicator = TryGetChild(GrabBackwardsBelowChildName);
		}
		void Update()
		{
			InputManager.Values inputs = InputManager.Instance.Inputs[InputIndex],
							    inputsLastFrame = InputManager.Instance.InputsLastFrame[InputIndex];

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
					foreach (Transform indicator in GetIndicatorsToSearch(Math.Sign(inputs.Move.y),
																		  (inputs.Move.x != 0)))
					{
						Vector2i tilePos = Board.ToTilePos(indicator.position);
						if (Board.CanPickUp(tilePos))
						{
							HoldingBlock = Board[tilePos];
							Board.RemoveBlockAt(tilePos);
							break;
						}
					}
				}
				//Otherwise, throw the block.
				else
				{
					GameObject thrownBlock = Instantiate(Consts.Instance.ThrownBlockPrefab);
					
					Transform tr = thrownBlock.transform;
					tr.position = BlockHoldIndicator.position;

					ThrownBlock tb = thrownBlock.GetComponent<ThrownBlock>();
					tb.BlockType = HoldingBlock;
					tb.Owner = this;
					tb.MySpr.sprite = Board.GetSpriteForBlock(HoldingBlock);

					//Calculate the block's velocity.
					tb.MyVelocity = inputs.Move;
					if (tb.MyVelocity == Vector2.zero)
					{
						tb.MyVelocity = new Vector2(Mathf.Sign(MyTr.localScale.x), 0.0f);
					}
					tb.MyVelocity *= Consts.Instance.BlockThrowSpeed;

					HoldingBlock = GameBoard.BlockTypes.Empty;
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