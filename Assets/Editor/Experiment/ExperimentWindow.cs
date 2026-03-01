
using System;
using System.IO;
using System.Linq;
using NvgNET;
using NvgNET.Rendering.UnityNvg;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NvgExample
{
	public class ExperimentElement : ImmediateModeElement
	{
		private readonly ComputeBuffer bufferWithVerts;
		private readonly Material material;
		
		public ExperimentElement()
		{
			RegisterCallback<DetachFromPanelEvent>(OnDisable)  ;
			
			style.flexGrow = 1f;
			
			bufferWithVerts = new ComputeBuffer(3 * 9, 4 * 3);
			Vector3[] verts = new Vector3[3 * 9];
			int ii = 0;
			for (int x = 0; x < 3; x++)
			{
				float xx = x / 3f;
				for (int y = 0; y < 3; y++)
				{
					float yy = y / 3f;
					verts[ii++] =  new Vector3(xx, yy) * 100f;
					verts[ii++] =  new Vector3(xx + 1f/3f, yy) * 100f;
					verts[ii++] =  new Vector3(xx + 1f/3f, yy + 1f/3f) * 100f;
				}
			}
			bufferWithVerts.SetData(verts);
			material =  new Material(Shader.Find("Exp/Shader"));
			material.SetBuffer(VertexBufferPropID,  bufferWithVerts);

		}

		private void OnDisable(DetachFromPanelEvent evt)
		{
			bufferWithVerts.Dispose();
		}

		
		private static readonly int VertexBufferPropID = Shader.PropertyToID("_VertexBuffer");
		private static readonly int OffsetPropID = Shader.PropertyToID("_Offset");
		protected override void ImmediateRepaint()
		{
			material.SetBuffer(VertexBufferPropID,  bufferWithVerts);
			for (int i = 0; i < 9; i++)
			{
				material.SetInteger(OffsetPropID, i * 3);
				material.SetPass(0);
				Graphics.DrawProceduralNow(MeshTopology.Triangles, 3);	
			}
			
			
			MarkDirtyRepaint();
			
		}
	}

	public class ExperimentWindow : EditorWindow
	{

		[MenuItem("TEST/ExperimentWindow")]
		public static void CreateWindow()
		{
			GetWindow<ExperimentWindow>();
		}


		public void CreateGUI()
		{
			VisualElement root = rootVisualElement;
			root.Add(new ExperimentElement());
		}
	}
}