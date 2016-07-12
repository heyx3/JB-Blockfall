using System;
using UnityEngine;


namespace GameObjects
{
	public class InputManager : Singleton<InputManager>
	{
		public static readonly int NInputs = 7;
		private static readonly string[] InStr = new string[7] { "1", "2", "3", "4", "5", "6", "7" };


		public static readonly int NGamepadInputs = 4,
								   NKeyboardInputs = 3,
								   FirstGamepadInput = 0,
								   FirstKeyboardInput = 4;


		public struct Values
		{
			public Vector2 Move, Aim;
			public bool Jump, Action;
		}

		public Values[] Inputs, InputsLastFrame;


		protected override void Awake()
		{
			base.Awake();

			Inputs = new Values[NInputs];
			InputsLastFrame = new Values[NInputs];
			for (int i = 0; i < NInputs; ++i)
			{
				Inputs[i].Move = Vector2.zero;
				Inputs[i].Aim = Vector2.zero;
				Inputs[i].Jump = false;
				Inputs[i].Action = false;

				InputsLastFrame[i].Move = Vector2.zero;
				InputsLastFrame[i].Jump = false;
				InputsLastFrame[i].Action = false;
			}
		}
		void Update()
		{
			for (int i = 0; i < NInputs; ++i)
			{
				InputsLastFrame[i] = Inputs[i];

				Inputs[i].Move.x = Input.GetAxis("Left/Right " + InStr[i]);
				Inputs[i].Move.y = Input.GetAxis("Up/Down " + InStr[i]);
				Inputs[i].Jump = (Input.GetAxis("Jump " + InStr[i]) > 0.0f);
				Inputs[i].Action = (Input.GetAxis("Action " + InStr[i]) > 0.0f);
				
				if (i < NGamepadInputs)
				{
					Inputs[i].Aim.x = Input.GetAxis("Aim L/R " + InStr[i]);
					Inputs[i].Aim.y = Input.GetAxis("Aim U/D " + InStr[i]);
				}
				else
				{
					Inputs[i].Aim = Inputs[i].Move;
				}
				Inputs[i].Aim = new Vector2(Inputs[i].Aim.x.Sign(),
											Inputs[i].Aim.y.Sign()).normalized;
			}
		}
	}
}