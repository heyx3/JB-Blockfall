using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



namespace GameLogic
{
	public abstract class GameUI_Base : Singleton<GameUI_Base>
	{
		public GameObject UIObject;

		[NonSerialized]
		public GameObject Canvas;


		protected override void Awake()
		{
			base.Awake();

			Canvas = GameObject.Find("Canvas");
			if (Canvas == null)
			{
				Debug.LogError("Couldn't find a GameObject named \"Canvas\"");
			}

			UIObject = Instantiate<GameObject>(UIObject);
			UIObject.transform.SetParent(Canvas.transform, false);
		}

		public abstract void OnPlayerHit(GameObjects.Player p);
	}
}
