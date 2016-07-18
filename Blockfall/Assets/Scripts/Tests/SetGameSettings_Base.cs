using System;
using System.Collections.Generic;
using UnityEngine;


namespace Tests
{
	public class SetGameSettings_Base : MonoBehaviour
	{
		public GameLogic.GameSettings_Base Settings = new GameLogic.GameSettings_Base();


		void Awake()
		{
			GameLogic.GameSettings_Base.CurrentSettings = Settings;
			Debug.Log("Using hardcoded game settings from 'SetGameSettings_Base'");
		}
		void Start()
		{
			GameObject.Destroy(this);
		}
	}
}