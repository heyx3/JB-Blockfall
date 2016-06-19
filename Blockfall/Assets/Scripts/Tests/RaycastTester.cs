using System;
using UnityEngine;


namespace Tests
{
	public class RaycastTester : MonoBehaviour
	{
		public Vector2 StartPos, EndPos;
		
		BoxRayHit Hit;
		public Vector2i HitPosI = new Vector2i(-1, -1);


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
				HitPosI = GameBoard.Board.Instance.CastRay(new Ray2D(StartPos, (EndPos - StartPos).normalized),
														   (Vector2i p, GameBoard.BlockTypes bt) =>
															  (bt == GameBoard.BlockTypes.Empty),
														   ref Hit, Vector2.Distance(StartPos, EndPos));
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

			switch (Hit.Wall)
			{
				case Walls.MinX:
					Gizmos.color = Color.green;
					break;
				case Walls.MinY:
					Gizmos.color = Color.blue;
					break;
				case Walls.MaxX:
					Gizmos.color = Color.cyan;
					break;
				case Walls.MaxY:
					Gizmos.color = Color.red;
					break;

				default: throw new NotImplementedException(Hit.Wall.ToString());
			}

			Gizmos.DrawSphere(Hit.Pos, 0.1f);
		}
	}
}