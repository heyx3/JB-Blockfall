using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameLogic
{
	public abstract class GameMode_Base : Singleton<GameMode_Base>
	{
		public Camera GameCam;


		protected static GameBoard.Board Board { get { return GameBoard.Board.Instance; } }
		protected static GameObjects.Consts Consts { get { return GameObjects.Consts.Instance; } }


		public virtual void OnPlayerHit(GameObjects.Player p)
		{
			GameUI_Base.Instance.OnPlayerHit(p);
			RespondToPlayerHit(p);
		}


		/// <summary>
		/// Reacts to the given player getting hit by something.
		/// Default behavior: Respawns him somewhere randomly inside the game camera's visible region.
		/// </summary>
		protected virtual void RespondToPlayerHit(GameObjects.Player p)
		{
			//Get the camera's viewable area.
			Transform camTr = GameCam.transform;
			Vector2 viewCenter = camTr.position;
			float renderWidthOverHeight = GameCam.aspect;
			float viewHeight = GameCam.orthographicSize * 2.0f,
				  viewWidth = viewHeight * renderWidthOverHeight;
			Rect viewBnds = new Rect(viewCenter.x - (viewWidth * 0.5f),
									 viewCenter.y - (viewHeight * 0.5f),
									 viewWidth, viewHeight);

			//Get valid places to respawn in.
			Rect pBnds = p.MyCollRect;
			List<Vector2i> spawnPoses = Board.GetSpawnablePositions(Board.ToTilePos(viewBnds.min),
																	Board.ToTilePos(viewBnds.max),
																	Board.ToTilePos(pBnds.max) -
																		Board.ToTilePos(pBnds.min) +
																		1,
																	false,
																	(bP, b) => b == GameBoard.BlockTypes.Empty);

			//Spawn the player in a random location.
			p.gameObject.SetActive(false);
			Vector2i newPos = spawnPoses[UnityEngine.Random.Range(0, spawnPoses.Count - 1)];
			ActionScheduler.Instance.Schedule(() =>
			{
				p.MyTr.position = Board.ToMinCorner(newPos) +
									new Vector2(pBnds.width, pBnds.height) * 0.5f;
				p.gameObject.SetActive(true);

				p.TimeTillVulnerable = Consts.SpawnInvincibilityTime;
				p.Blinker.Start();
			}, GameSettings_Base.CurrentSettings.RespawnTime);
		}
	}
}