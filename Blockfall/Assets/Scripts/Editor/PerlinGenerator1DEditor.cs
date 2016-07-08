using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace GameBoard.Generators
{
	[CustomEditor(typeof(BoardGenerator_Perlin1D))]
	public class PerlinGenerator1DEditor : Editor
	{
		public AnimationCurve HorzCurve = null,
							  VertCurve = null;

		public int PreviewLevelWidth = 40,
				   PreviewLevelHeight = 60;


		public override void OnInspectorGUI()
		{
			BoardGenerator_Perlin1D gen = (BoardGenerator_Perlin1D)target;

			//Edit the values, and if things have changed, regenerate the display curve.

			EditorGUI.BeginChangeCheck();

			base.OnInspectorGUI();

			GUILayout.Space(15.0f);

			//Edit custom fields.
			PreviewLevelWidth = EditorGUILayout.IntField("Preview width", PreviewLevelWidth);
			PreviewLevelHeight = EditorGUILayout.IntField("Preview height", PreviewLevelHeight);

			if (EditorGUI.EndChangeCheck() || HorzCurve == null)
			{
				GUILayout.Space(15.0f);

				float[] horzLines, vertLines;
				gen.GenerateNoise(out horzLines, out vertLines,
								  Vector2i.Zero,
								  new Vector2i(PreviewLevelWidth - 1, PreviewLevelHeight - 1));

				HorzCurve = MakeCurve(horzLines, gen.Threshold);
				VertCurve = MakeCurve(vertLines, gen.Threshold);
			}


			//Display the curves for each noise line.
			GUI.enabled = false;
			HorzCurve = EditorGUILayout.CurveField("Horizontal Noise", HorzCurve);
			VertCurve = EditorGUILayout.CurveField("Vertical Noise", VertCurve);
			GUI.enabled = true;
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
