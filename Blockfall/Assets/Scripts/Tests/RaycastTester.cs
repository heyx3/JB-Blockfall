using System;
using UnityEngine;


namespace Tests
{
	public class RaycastTester : MonoBehaviour
	{
		public Vector2 StartPos, EndPos;

		GameBoard.Board.RaycastResult? Hit = null;


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
				GameBoard.Board.RaycastResult _Hit = new GameBoard.Board.RaycastResult();
				if (GameBoard.Board.Instance.CastRay(new Ray2D(StartPos, (EndPos - StartPos).normalized),
													 (Vector2i p, GameBoard.BlockTypes bt) =>
													     (bt == GameBoard.BlockTypes.Empty),
													 ref _Hit, Vector2.Distance(StartPos, EndPos)))
				{
					Hit = _Hit;
				}
				else
				{
					Hit = null;
				}
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
			
			Gizmos.color = Color.black;
			Gizmos.DrawSphere(EndPos, 0.15f);
			Gizmos.color = Color.white;
			Gizmos.DrawSphere(StartPos, 0.15f);
			Gizmos.DrawLine(StartPos, EndPos);

			if (!Hit.HasValue)
				return;

			switch (Hit.Value.Hit.HitSides)
			{
				case GridCasting2D.Walls.MinX:
					Gizmos.color = Color.green;
					break;
				case GridCasting2D.Walls.MinY:
					Gizmos.color = Color.blue;
					break;
				case GridCasting2D.Walls.MaxX:
					Gizmos.color = Color.cyan;
					break;
				case GridCasting2D.Walls.MaxY:
					Gizmos.color = Color.red;
					break;
				
				default:
					Gizmos.color = Color.white;
					break;
			}

			Gizmos.DrawSphere(Hit.Value.Hit.Pos, 0.1f);
		}
	}
}