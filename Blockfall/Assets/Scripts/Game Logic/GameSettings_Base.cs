using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace GameLogic
{
	[Serializable]
	public class GameSettings_Base
	{
		public static GameSettings_Base CurrentSettings = new GameSettings_Base();


		public bool FriendlyFire = false;
		public float RespawnTime = 2.0f;


		public GameSettings_Base() { }
		public GameSettings_Base(bool friendlyFire, float respawnTime)
		{
			FriendlyFire = friendlyFire;
			RespawnTime = respawnTime;
		}
	}
}