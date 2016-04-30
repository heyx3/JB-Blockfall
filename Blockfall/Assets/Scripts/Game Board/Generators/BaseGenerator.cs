using System;
using UnityEngine;


namespace GameBoard.Generators
{
	public abstract class BaseGenerator : Singleton<BaseGenerator>
	{
		public Camera GameCam;

		
		protected virtual void Start()
		{
			DoGeneration();

			GameCam.transform.position = Board.Instance.CenterWorldPos;
			GameCam.orthographicSize = Board.Instance.Height * 0.5f;

			//If the board's width is bigger than the camera's view width, adjust the camera.
			float worldWidth = GameCam.orthographicSize * 2.0f * GameCam.aspect;
			float scale = Board.Instance.Width / worldWidth;
			if (scale > 1.0f)
			{
				GameCam.orthographicSize *= scale;
			}
		}

		protected abstract void DoGeneration();
	}
}