using System;
using System.Collections.Generic;
using UnityEngine;


namespace GameObjects
{
	/// <summary>
	/// Turns into a solid block as soon as it hits something.
	/// </summary>
	public class ThrownBlock : DynamicObject
	{
		public GameBoard.BlockTypes BlockType;
		public Player Owner;

		[NonSerialized]
		public Vector2 Velocity;

		private bool deadYet = false;


		protected override void FixedUpdate()
		{
			Move(Velocity * Time.deltaTime, MovementTypes.Sweep);
			base.FixedUpdate();
		}

		public override void OnHitLeftSide(Vector2i wallPos) { Settle(); }
		public override void OnHitRightSide(Vector2i wallPos) { Settle(); }
		public override void OnHitFloor(Vector2i floorPos) { Settle(); }
		public override void OnHitCeiling(Vector2i ceilingPos) { Settle(); }

		public override void OnHitDynamicObject(DynamicObject other)
		{
			Player p = other as Player;
			if (p != null && !p.IsInvincible && Owner != p &&
				(GameLogic.GameSettings_Base.CurrentSettings.FriendlyFire ||
				 Owner.TeamIndex != p.TeamIndex))
			{
				Settle();
			}
			else if (other is ThrownBlock)
			{
				Settle();
			}
		}

		private void Settle()
		{
			Vector2i tilePos = Board.ToTilePos((Vector2)MyTr.position);

			if (deadYet)
				return;
			deadYet = true;

			MySpr.enabled = false;
			Velocity = Vector2.zero;
			ActionScheduler.Instance.Schedule(() => Destroy(gameObject), 5);

			Board[tilePos] = BlockType;

			//TODO: Push any hit players away in the opposite direction of this block's velocity.
		}
	}
}