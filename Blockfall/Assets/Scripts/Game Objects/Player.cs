using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameObjects
{
    [RequireComponent(typeof(BlinkRenderer))]
	public class Player : ControllableObject
	{
		public static IEnumerable<Player> AllPlayers { get { return allPlayers; } }
		public static IEnumerable<Player> AllActivePlayers { get { return allPlayers.Where(p => p.gameObject.activeSelf); } }
		private static List<Player> allPlayers = new List<Player>();

		protected static GameLogic.GameMode_Base GameMode { get { return GameLogic.GameMode_Base.Instance; } }
		protected static GameLogic.GameSettings_Base GameSettings { get { return GameLogic.GameSettings_Base.CurrentSettings; } }


		public Transform ArrowPivot;

		public int TeamIndex = 0;
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

        [NonSerialized]
        public float TimeTillVulnerable = -1.0f;


        public bool IsInvincible { get { return TimeTillVulnerable > 0.0f; } }

        public BlinkRenderer Blinker { get; private set; }

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

			allPlayers.Add(this);

            Blinker = GetComponent<BlinkRenderer>();

			HoldingBlock = GameBoard.BlockTypes.Empty;
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
		void OnDestroy()
		{
			allPlayers.Remove(this);
		}
        void Start()
        {
            TimeTillVulnerable = Consts.Instance.SpawnInvincibilityTime;
            Blinker.enabled = true;
        }
		protected override void Update()
		{
			base.Update();

            //Update invincibility.
            if (TimeTillVulnerable > 0.0f)
            {
                Blinker.enabled = true;
                TimeTillVulnerable -= Time.deltaTime;
                if (TimeTillVulnerable <= 0.0f)
                    Blinker.Stop();
            }
            
			InputManager.Values inputs = InputManager.Instance.Inputs[InputIndex],
							    inputsLastFrame = InputManager.Instance.InputsLastFrame[InputIndex];

			//Mirror based on aiming.
			if (inputs.Aim.x < 0.0f)
			{
				MyTr.localScale = new Vector3(-Math.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}
			else if (inputs.Aim.x > 0.0f)
			{
				MyTr.localScale = new Vector3(Math.Abs(MyTr.localScale.x),
											  MyTr.localScale.y, MyTr.localScale.z);
			}


			//Update aim arrow.
			if (inputs.Aim.x != 0.0f || inputs.Aim.y != 0.0f)
			{
				Vector2 lookDir = inputs.Aim;
				lookDir = new Vector2(lookDir.x * MyTr.localScale.x, lookDir.y * MyTr.localScale.y);
				ArrowPivot.localRotation = Quaternion.identity;
				ArrowPivot.Rotate(new Vector3(0.0f, 0.0f, 1.0f),
								  Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg);
			}

			//Jump.
            if (inputs.Jump && !inputsLastFrame.Jump && JumpsLeft > 0)
			{
				JumpsLeft -= 1;
				MyVelocity = new Vector2(MyVelocity.x, Consts.Instance.JumpSpeed);
				IsOnFloor = false;
			}

			//Action.
			if (inputs.Action && !inputsLastFrame.Action)
			{
				//If this player isn't holding a block, pick one up.
				if (HoldingBlock == GameBoard.BlockTypes.Empty)
				{
					foreach (Transform indicator in GetIndicatorsToSearch(inputs.Move.y.SignI(),
																		  (inputs.Move.x != 0)))
					{
						Vector2i tilePos = Board.ToTilePos(indicator.position);
						if (Board.CanPickUp(tilePos))
						{
							HoldingBlock = Board[tilePos];
							Board[tilePos] = GameBoard.BlockTypes.Empty;
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
					tb.MySpr.sprite = Board.GetSpriteFor(HoldingBlock);

					//Calculate the block's velocity.
					tb.MyVelocity = inputs.Aim;
					if (tb.MyVelocity == Vector2.zero)
					{
						tb.MyVelocity = new Vector2(Mathf.Sign(MyTr.localScale.x), 0.0f);
					}
					tb.MyVelocity *= Consts.Instance.BlockThrowSpeed;

					HoldingBlock = GameBoard.BlockTypes.Empty;
				}
			}


			//Fall faster/slower based on input.
            if (!IsOnFloor && MyVelocity.y < 0.0f && inputs.Jump)
            {
				MyVelocity = new Vector2(MyVelocity.x,
										 MyVelocity.y +
											(Consts.Instance.SlowFallAccel * Time.deltaTime));
            }
			if (!IsOnFloor && inputs.Move.y < 0.0f)
			{
				MyVelocity = new Vector2(MyVelocity.x,
										 MyVelocity.y +
											 (Consts.Instance.FastFallAccel * inputs.Move.y * Time.deltaTime));
			}
		}
		protected override void FixedUpdate()
		{
			//Apply gravity.
			if (!IsOnFloor)
			{
				MyVelocity = new Vector2(MyVelocity.x,
										 MyVelocity.y + (Consts.Instance.Gravity * Time.deltaTime));
			}

			base.FixedUpdate();
		}

		protected override float GetLeftRightMoveInput()
		{
			return InputManager.Instance.Inputs[InputIndex].Move.x * Consts.Instance.PlayerSpeed;
		}


		public override void OnHitFloor(Vector2i floorPos)
		{
			base.OnHitFloor(floorPos);
			JumpsLeft = Consts.Instance.NJumps;
		}

		public override void OnHitDynamicObject(DynamicObject other)
		{
			if (other is ThrownBlock)
			{
				ThrownBlock blck = (ThrownBlock)other;
				if (!IsInvincible && blck.Owner != this)
					GameMode.OnPlayerHit(this);
			}
		}
	}
}