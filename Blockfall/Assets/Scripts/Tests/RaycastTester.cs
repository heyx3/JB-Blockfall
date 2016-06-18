using System;
using UnityEngine;


namespace Tests
{
	public class RaycastTester : MonoBehaviour
	{
		public Vector2 StartPos, EndPos;
		
		public Vector2 HitPos;
		public Vector2i HitPosI = new Vector2i(-1, -1);
		public int HitD;


		void Update()
		{
			if (Input.GetMouseButton(0))
			{
				StartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			}
			if (Input.GetMouseButton(1))
			{
				EndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			}
			if (Input.GetMouseButton(2))
			{
				float t = float.NaN;
				HitPosI = GameBoard.Board.Instance.CastRay(new Ray2D(StartPos, (EndPos - StartPos).normalized),
														   (Vector2i p, GameBoard.BlockTypes bt) =>
															  (bt == GameBoard.BlockTypes.Empty),
														   ref HitPos, ref t, ref HitD,
														   Vector2.Distance(StartPos, EndPos));
			}
		}
		void OnGUI()
		{
			GUILayout.Label("Left-click to set start point");
			GUILayout.Label("Right-click to set end point");
			GUILayout.Label("Middle-click to cast");
			GUILayout.Label("Displayed as gizmos in the scene view");
		}
		void OnDrawGizmos()
		{
			if (!Application.isPlaying)
				return;
			
			Gizmos.color = Color.white;

			Gizmos.DrawSphere(StartPos, 0.15f);
			Gizmos.DrawSphere(EndPos, 0.15f);
			Gizmos.DrawLine(StartPos, EndPos);

			if (HitPosI == new Vector2i(-1, -1))
				return;

			switch (HitD)
			{
				case 0:
					Gizmos.color = Color.green;
					break;
				case 1:
					Gizmos.color = Color.blue;
					break;

				default: throw new NotImplementedException(HitD.ToString());
			}

			Gizmos.DrawSphere(HitPos, 0.1f);
		}
	}
}