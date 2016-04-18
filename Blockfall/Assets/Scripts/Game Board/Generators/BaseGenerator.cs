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
		}

		protected abstract void DoGeneration();
	}
}