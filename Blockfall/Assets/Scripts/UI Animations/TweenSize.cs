using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UIAnimations
{
	public class TweenSize : MonoBehaviour
	{
		[Serializable]
		public class TweenInfo
		{
			public AnimationCurve SizeOverTime = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);
			public float AnimLength = 1.0f;
			public bool ResetSizeWhenDone = true;
		}

		public TweenInfo Info = new TweenInfo();


		[NonSerialized]
		public float ElapsedTime = 0.0f;

		public Transform MyTr { get; private set; }
		public Vector3 OriginalSize { get; private set; }


		void Awake()
		{
			MyTr = transform;
			OriginalSize = MyTr.localScale;
		}
		void Update()
		{
			ElapsedTime += Time.deltaTime;

			MyTr.localScale = OriginalSize * Info.SizeOverTime.Evaluate(ElapsedTime / Info.AnimLength);

			if (ElapsedTime >= Info.AnimLength)
			{
				if (Info.ResetSizeWhenDone)
				{
					MyTr.localScale = OriginalSize;
				}

				Destroy(this);
			}
		}
	}
}