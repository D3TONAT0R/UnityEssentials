using UnityEssentials;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
#if UNITY_2020_1_OR_NEWER
using ToolManager = UnityEditor.EditorTools.ToolManager;

#else
#endif


namespace UnityEssentialsEditor.Tools
{
#if UNITY_2021_2_OR_NEWER
	[UnityEditor.Overlays.Overlay(typeof(SceneView), "Measuring Tool")]
	internal class MeasuringToolOverlay : ToolOverlayBase<MeasuringTool>
	{
	}
#endif

	//TODO: needs some rework
	[EditorTool("Measure")]
	internal class MeasuringTool : EditorToolBase
	{
		public Vector3? p1;
		public Vector3? p2;

		private bool vertexSnapping = false;
		
		private GUIStyle labelStyle;
		
		public override bool ShowToolWindow => true;

		public override int ToolWindowWidth => 300;

		public override string ToolbarIconPath => ESSENTIALS_PACKAGE_ICONS_PATH + "tool_measure.png";

		protected override void OnBecameActive()
		{
			vertexSnapping = false;
		}

		public override void OnWindowGUI() 
		{
			EditorGUIUtility.labelWidth = 120;
			if(p1.HasValue) p1 = EditorGUILayout.Vector3Field("Point 1", p1.Value);
			else EditorGUILayout.LabelField("Point 1", "Click in Scene View to set");
			if(p2.HasValue) p2 = EditorGUILayout.Vector3Field("Point 2", p2.Value);
			else EditorGUILayout.LabelField("Point 2", "Click in Scene View to set");
			EditorGUILayout.Space(10);
			Vector3 distanceVec;
			float distance3d, distance2d, angle2d;
			if (p1.HasValue && p2.HasValue)
			{
				distanceVec = p2.Value - p1.Value;
				distance3d = Vector3.Distance(p1.Value, p2.Value);
				distance2d = Vector2.Distance(p1.Value.XZ(), p2.Value.XZ());
				var vec = p2 - p1;
				if (vec != Vector3.zero)
				{
					angle2d = Quaternion.LookRotation(p2.Value - p1.Value).eulerAngles.y;
				}
				else
				{
					angle2d = 0;
				}
			}
			else
			{
				distanceVec = Vector3.zero;
				distance3d = 0;
				distance2d = 0;
				angle2d = 0;
				GUI.enabled = false;
			}
			GUI.enabled = false;
			EditorGUILayout.LabelField("Distance (Per Axis)", $"X {distanceVec.x:F3}, Y {distanceVec.y:F3}, Z {distanceVec.z:F3}");
			EditorGUILayout.LabelField("Distance (2D)", distance2d.ToString("F3"));
			EditorGUILayout.LabelField("Distance", distance3d.ToString("F3"));
			EditorGUILayout.LabelField("Angle (2D)", angle2d.ToString("F1"));
			GUI.enabled = true;
			if (Event.current.type == EventType.Repaint)
			{
				SceneView.lastActiveSceneView.Repaint();
			}
		}

		private bool GetPosition(SceneView sv, out Vector3? pos, out Transform target, out Vector3 normal)
		{
			var mousePos = Event.current.mousePosition;
			if (vertexSnapping)
			{
				if (HandleUtility.FindNearestVertex(mousePos, out var vPos, out var go))
				{
					pos = vPos;
					target = go != null ? go.transform : null;
					normal = Vector3.zero;
					return true;
				}
			}
			if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(mousePos), out var hit, 10000f, sv.camera.cullingMask,
				    QueryTriggerInteraction.Ignore))
			{
				pos = hit.point;
				target = hit.transform;
				normal = hit.normal;
				return true;
			}
			pos = null;
			target = null;
			normal = Vector3.zero;
			return false;
		}

		protected override void OnSceneGUI(EditorWindow window, bool enableInteraction)
		{
			var sv = window as SceneView;
			GetPosition(sv, out var hoveredPos, out var hoveredTarget, out var hoveredNormal);
			if (!p1.HasValue || !p2.HasValue)
			{
				if (hoveredPos.HasValue)
				{
					DrawPoint(hoveredPos.Value);
					if(hoveredNormal != Vector3.zero)
					{
						Handles.color = Color.white;
						var size = HandleUtility.GetHandleSize(hoveredPos.Value) * 0.15f;
						Handles.DrawWireDisc(hoveredPos.Value, hoveredNormal, size);
						Handles.ArrowHandleCap(-1, hoveredPos.Value, Quaternion.LookRotation(hoveredNormal), size * 2f, EventType.Repaint);
					}
				}
			} 

			if (p1.HasValue && !p2.HasValue)
			{
				DrawPoint(p1.Value);
			}
			if (p1.HasValue && p2.HasValue)
			{
				float scale = 0.5f;
				Handles.matrix = Matrix4x4.Scale(Vector3.one * scale);
				p1 = Handles.PositionHandle(p1.Value / scale, Quaternion.identity) * scale;
				p2 = Handles.PositionHandle(p2.Value / scale, Quaternion.identity) * scale;
				Handles.matrix = Matrix4x4.identity;
			}

			if (p1.HasValue)
			{
				var pos2 = p2 ?? hoveredPos;
				if (pos2.HasValue)
				{
					Handles.color = Color.white;
					Handles.DrawDottedLine(p1.Value, pos2.Value, 4);
					var center = (p1.Value + pos2.Value) * 0.5f;
					var size = (pos2.Value - p1.Value).Abs();
					var pt = HandleUtility.WorldToGUIPoint(center);
					Handles.BeginGUI();
					if(labelStyle == null)
					{
						var background = new Texture2D(1, 1);
						background.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
						background.Apply();
						labelStyle = new GUIStyle(EditorStyles.boldLabel)
						{
							normal =
							{
								background = background,
								textColor = Color.white
							},
							fontSize = 12,
							alignment = TextAnchor.MiddleCenter,
							wordWrap = true
						};
					}
					var distance = Vector3.Distance(p1.Value, pos2.Value);
					if (Event.current.type == EventType.Repaint)
					{
						var content = new GUIContent(distance.ToString("F3"));
						var labelSize = labelStyle.CalcSize(content);
						labelStyle.Draw(new Rect(pt - labelSize / 2, labelSize), content, false, false, false, false);
					}
					Handles.EndGUI();
					int i = 0; 
					if(size.x > 0.001f) i++;
					if(size.y > 0.001f) i++;
					if(size.z > 0.001f) i++;
					if (i > 1)
					{
						Handles.color = Handles.color.WithAlpha(0.1f);
						Handles.DrawWireCube(center, size);
					}
				}
			}
			
			if (Event.current.type == EventType.KeyDown)
			{
				if(Event.current.keyCode == KeyCode.V)
				{
					Event.current.Use();
					vertexSnapping = true;
				}
			}
			if(Event.current.type == EventType.KeyUp)
			{
				if(Event.current.keyCode == KeyCode.V)
				{
					Event.current.Use();
					vertexSnapping = false;
				}
			}
			
			if(Event.current.type == EventType.MouseDown && Event.current.button == 0)
			{
				Event.current.Use();
				if(!p1.HasValue) p1 = hoveredPos;
				else if(!p2.HasValue) p2 = hoveredPos;
				else
				{
					p1 = null;
					p2 = null;
				}
			}
		}

		private void DrawPoint(Vector3 vec)
		{
			var size = HandleUtility.GetHandleSize(vec) * 0.25f;
			Handles.DrawDottedLine(vec + Vector3.left * size, vec + Vector3.right * size, 1);
			Handles.DrawDottedLine(vec + Vector3.down * size, vec + Vector3.up * size, 1);
			Handles.DrawDottedLine(vec + Vector3.back * size, vec + Vector3.forward * size, 1);
			Handles.DrawWireCube(vec, Vector3.one * size * 0.1f);
		}
	}
}