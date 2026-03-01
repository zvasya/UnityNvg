using System.Diagnostics;
using System.IO;
using NvgNET;
using NvgNET.Rendering.UnityNvg;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityNvg;

namespace NvgExample
{
	public class NvgDemoElement : ImmediateModeElement
	{
		double t = 0;
		double mx = 0, my = 0;
		float winWidth;
		float winHeight;

		private DemoResources _resources;
		
		UnityNvgRenderer nvgRenderer;
		Nvg nvg;
		Demo demo;
		PerformanceGraph frameGraph;
		PerformanceGraph cpuGraph;
		Stopwatch sw = new Stopwatch();
		

		public NvgDemoElement(DemoResources resources)
		{
			_resources = resources;
			RegisterCallback<AttachToPanelEvent>(OnEnable);	
			RegisterCallback<DetachFromPanelEvent>(OnDisable);
			
			style.flexGrow = 1f;
		}

		private void OnEnable(AttachToPanelEvent evt)
		{
			UnityNvgContext context = new ImmediateModeNvgContext(false, false);
			nvgRenderer = new UnityNvgRenderer(context, true);
			nvg = Nvg.Create(nvgRenderer);
			frameGraph = new PerformanceGraph(PerformanceGraph.GraphRenderStyle.Fps, "Frame Time");
			cpuGraph = new PerformanceGraph(PerformanceGraph.GraphRenderStyle.Ms, "CPU Time");
			
			
			demo = new Demo(nvg, _resources);
			sw.Start();
		}
		
		private void OnDisable(DetachFromPanelEvent evt)
		{
			demo.Dispose();
			nvg.Dispose();
			nvgRenderer.Dispose();
			sw.Stop();
		}
		
		protected override void ImmediateRepaint()
		{
			MarkDirtyRepaint();
			double time = sw.ElapsedMilliseconds / 1000.0;
			float dt = (float)(time - t);
			t = time;
			float pxRatio;
			
			
			float cwinWidth = layout.width;
			float cwinHeight = layout.height;
			winWidth = cwinWidth;
			winHeight = cwinHeight;
			
			frameGraph.Update(dt);
			cpuGraph.Update(dt);
			
			pxRatio = EditorGUIUtility.pixelsPerPoint;
			
			nvg.BeginFrame(winWidth, winHeight, pxRatio);
			
			demo.Render((float)mx, (float)my, winWidth, winHeight, (float)t, false);
			
			frameGraph.Render(5.0f, 5.0f, nvg);
			cpuGraph.Render(5.0f + 200.0f + 5.0f, 5.0f, nvg);
			
			nvg.EndFrame();
		}
	}

	public class ExampleWindow : EditorWindow
	{
		[SerializeField]
		private DemoResources _resources;
		
		[MenuItem("TEST/NvgExampleWindow")]
		public static void CreateWindow()
		{
			GetWindow<ExampleWindow>();
		}


		public void CreateGUI()
		{
			VisualElement root = rootVisualElement;
			root.Add(new NvgDemoElement(_resources));
		}
	}
}