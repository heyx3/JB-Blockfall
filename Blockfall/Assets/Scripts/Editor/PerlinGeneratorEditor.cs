using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GameBoard.Generators
{
	[CustomEditor(typeof(PerlinGenerator))]
	public class PerlinGeneratorEditor : Editor
	{
		public AnimationCurve HorzCurve = null,
							  VertCurve = null;


		public override void OnInspectorGUI()
		{
			PerlinGenerator gen = (PerlinGenerator)target;

			//Edit the values, and if things have changed, regenerate the display curve.
			EditorGUI.BeginChangeCheck();
			base.OnInspectorGUI();
			if (EditorGUI.EndChangeCheck() || HorzCurve == null)
			{
				float[] horzLines, vertLines;
				gen.GenerateNoise(out horzLines, out vertLines);

				HorzCurve = MakeCurve(horzLines, gen.Threshold);
				VertCurve = MakeCurve(vertLines, gen.Threshold);
			}


			//Display the curves for each noise line.
			GUI.enabled = false;

			HorzCurve = EditorGUILayout.CurveField("Horizontal Noise", HorzCurve);
			VertCurve = EditorGUILayout.CurveField("Vertical Noise", VertCurve);
		}
		private AnimationCurve MakeCurve(float[] keys, float threshold)
		{
			float epsilon = 0.001f;
			AnimationCurve ac = new AnimationCurve();
			float increment = 1.0f / (float)(keys.Length - 1);
			for (int i = 0; i < keys.Length; ++i)
			{
				ac.AddKey(new Keyframe((i * increment) + epsilon, keys[i]));
				ac.AddKey(new Keyframe(((i + 1) * increment) - epsilon, keys[i]));
			}
			return ac;
		}
	}
}
