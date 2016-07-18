using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameLogic
{
	public class GameUI_Test : GameUI_Base
	{
		public string[] TeamHitIndicatorNames = new string[] { "Team 1 Text", "Team 2 Text",
															   "Team 3 Text", "Team 4 Text" };

		private GameObject[] TeamHitIndicators;


		[Serializable]
		public class TweenInfo
		{
			public UIAnimations.TweenColor.TweenInfo Color = new UIAnimations.TweenColor.TweenInfo();
			public UIAnimations.TweenSize.TweenInfo Size = new UIAnimations.TweenSize.TweenInfo();
		}
		public TweenInfo TeamHitIndicatorEffects = new TweenInfo();


		public override void OnPlayerHit(GameObjects.Player p)
		{
			TeamHitIndicators[p.TeamIndex].AddComponent<UIAnimations.TweenColor>().Info =
				TeamHitIndicatorEffects.Color;
			TeamHitIndicators[p.TeamIndex].AddComponent<UIAnimations.TweenSize>().Info =
				TeamHitIndicatorEffects.Size;
		}


		protected override void Awake()
		{
			base.Awake();
			
			Transform tr = UIObject.transform;

			TeamHitIndicators = new GameObject[TeamHitIndicatorNames.Length];
			for (int i = 0; i < TeamHitIndicators.Length; ++i)
			{
				Transform tr2 = tr.FindChild(TeamHitIndicatorNames[i]);
				TeamHitIndicators[i] = tr2.gameObject;
			}
		}
	}
}