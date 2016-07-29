using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameObjects
{
	//TODO: Abstract out input so that AI players can exist too.

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
		private bool jumpedSinceFixedUpdate = false;

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
		protected virtual void OnDestroy()
		{
			allPlayers.Remove(this);
		}
        protected virtual void Start()
        {
            TimeTillVulnerable = Consts.Instance.SpawnInvincibilityTime;
            Blinker.enabled = true;
        }
		protected virtual void Update()
		{
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
            if (!jumpedSinceFixedUpdate && inputs.Jump && !inputsLastFrame.Jump && JumpsLeft > 0)
			{
				jumpedSinceFixedUpdate = false;
				JumpsLeft -= 1;
				VerticalSpeed = Consts.Instance.JumpSpeed;
				IsOnFloor = false;
			}

			//Fall faster/slower based on input.
            if (!IsOnFloor && VerticalSpeed < 0.0f && inputs.Jump)
            {
				VerticalSpeed += Consts.Instance.SlowFallAccel * Time.deltaTime;
            }
			if (!IsOnFloor && inputs.Move.y < 0.0f)
			{
				VerticalSpeed += Consts.Instance.FastFallAccel * inputs.Move.y * Time.deltaTime;
			}


			//Action.
			GameBoard.Board.RaycastResult dummyVar = new GameBoard.Board.RaycastResult();
			if (inputs.Action && !inputsLastFrame.Action)
			{
				//If this player isn't holding a block, pick one up.
				if (HoldingBlock == GameBoard.BlockTypes.Empty)
				{
					foreach (Transform indicator in GetIndicatorsToSearch(inputs.Move.y.SignI(),
																		  (inputs.Move.x != 0.0f)))
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
				//Otherwise, throw the block if nothing is in the way.
				else
				{
					//If not aiming in any particular direction, just aim straight forward.
					Vector2 aimDir = inputs.Aim;
					if (aimDir == Vector2.zero)
						aimDir = new Vector2(Mathf.Sign(MyTr.localScale.x), 0.0f);

					//TODO: Test.
					//Only throw the block if there's at least one empty tile in front of the player.
					//if (!Board.CastRay(new Ray2D(BlockHoldIndicator.position, aimDir),
					//					(posI, bType) => !GameBoard.Board.IsSolid(bType),
					//					ref dummyVar, 1.001f))
					//{
					//}

					//Sweep the block, find the closest time to contact,
					//    and see if the block there gets in the way of this player.
					ThrownBlock prefabTB = Consts.Instance.ThrownBlockPrefab.GetComponent<ThrownBlock>();
					Vector2 throwStart = BlockHoldIndicator.position,
							throwMin = throwStart - (prefabTB.CollisionBoxSize * 0.5f);
					const float maxDist = 5.0f;
					Vector2 movement = aimDir * maxDist;
					HitsPerFace hitsPerFace;
					bool hit = TrySweep(new Rect(throwMin, prefabTB.CollisionBoxSize),
										new Vector2i(prefabTB.NCollisionRaysXFace,
													 prefabTB.NCollisionRaysYFace),
										ref movement, out hitsPerFace);
					if (hit)
					{
						//See whether the block will get in the way of the player that threw it.

						float closestT = float.PositiveInfinity;
						if (hitsPerFace.MinX.HasValue)
							closestT = Math.Min(closestT, hitsPerFace.MinX.Value.Hit.Distance);
						if (hitsPerFace.MaxX.HasValue)
							closestT = Math.Min(closestT, hitsPerFace.MaxX.Value.Hit.Distance);
						if (hitsPerFace.MinY.HasValue)
							closestT = Math.Min(closestT, hitsPerFace.MinY.Value.Hit.Distance);
						if (hitsPerFace.MaxY.HasValue)
							closestT = Math.Min(closestT, hitsPerFace.MaxY.Value.Hit.Distance);

						//Get the block's new position and the player's predicted position
						//    at the moment of impact.
						Vector2 newBlockPos = throwStart + (aimDir * closestT);
						Rect newBlockBnds = new Rect(newBlockPos - (prefabTB.CollisionBoxSize * 0.5f),
													 prefabTB.CollisionBoxSize),
							 currentPlayerBnds = MyCollRect,
							 newPlayerBnds = new Rect(currentPlayerBnds.min +
														  (new Vector2(LastMoveSpeedX, VerticalSpeed) * closestT),
													  currentPlayerBnds.size);
						hit = newBlockBnds.Overlaps(newPlayerBnds);
					}

					if (hit)
					{
						GameObject thrownBlock = Instantiate(Consts.Instance.ThrownBlockPrefab);

						Transform tr = thrownBlock.transform;
						tr.position = BlockHoldIndicator.position;

						ThrownBlock tb = thrownBlock.GetComponent<ThrownBlock>();
						tb.BlockType = HoldingBlock;
						tb.Owner = this;
						tb.MySpr.sprite = Board.GetSpriteFor(HoldingBlock);
						
						tb.Velocity = aimDir * Consts.Instance.BlockThrowSpeed;

						HoldingBlock = GameBoard.BlockTypes.Empty;
					}
				}
			}
		}
		protected override void FixedUpdate()
		{
			jumpedSinceFixedUpdate = false;
			base.FixedUpdate();
		}

		protected override sealed float GetLeftRightMoveInput()
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