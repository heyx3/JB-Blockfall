using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UIAnimations
{
	[RequireComponent(typeof(UnityEngine.UI.Graphic))]
	public class TweenColor : MonoBehaviour
	{
		[Serializable]
		public class TweenInfo
		{
			public AnimationCurve RedOverTime = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f),
								  GreenOverTime = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f),
								  BlueOverTime = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f),
								  AlphaOverTime = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);
			public float AnimLength = 1.0f;
			public bool ResetColorWhenDone = true;
		}
		public TweenInfo Info = new TweenInfo();


		[NonSerialized]
		public float ElapsedTime = 0.0f;

		public UnityEngine.UI.Graphic MyGraphic { get; private set; }
		public Color OriginalColor { get; private set; }


		void Awake()
		{
			MyGraphic = GetComponent<UnityEngine.UI.Graphic>();
			OriginalColor = MyGraphic.color;
		}
		void Update()
		{
			ElapsedTime += Time.deltaTime;

			float t = ElapsedTime / Info.AnimLength;
			MyGraphic.color = new Color(OriginalColor.r * Info.RedOverTime.Evaluate(t),
										OriginalColor.g * Info.GreenOverTime.Evaluate(t),
										OriginalColor.b * Info.BlueOverTime.Evaluate(t),
										OriginalColor.a * Info.AlphaOverTime.Evaluate(t));

			if (ElapsedTime >= Info.AnimLength)
			{
				if (Info.ResetColorWhenDone)
				{
					MyGraphic.color = OriginalColor;
				}

				Destroy(this);
			}
		}
	}
}