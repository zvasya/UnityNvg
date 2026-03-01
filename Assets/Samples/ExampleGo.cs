using System.IO;
using NvgNET;
using NvgNET.Rendering.UnityNvg;
using UnityEngine;
using UnityEngine.Profiling;
using UnityNvg;

namespace NvgExample
{
	public class ExampleGo : MonoBehaviour
	{
		[SerializeField]
		private DemoResources _resources;
		
		UnityNvgRenderer nvgRenderer;
		Nvg nvg;
		Demo demo;
		PerformanceGraph frameGraph;
		PerformanceGraph cpuGraph;
		readonly FrameTiming[] frameTiming =  new FrameTiming[1];
		
		public float pxRatio = 1;

		private void OnEnable()
		{
			UnityNvgContext context = new ImmediateModeNvgContext(true, true);
			nvgRenderer = new UnityNvgRenderer(context, false);
			nvg = Nvg.Create(nvgRenderer);
			frameGraph = new PerformanceGraph(PerformanceGraph.GraphRenderStyle.Fps, "Frame Time");
			cpuGraph = new PerformanceGraph(PerformanceGraph.GraphRenderStyle.Ms, "CPU Time");
			
			demo = new Demo(nvg, _resources);
			
		}

		private void OnDisable()
		{
			demo.Dispose();
			nvg.Dispose();
			nvgRenderer.Dispose();
		}

		double t = 0;
		double mx = 0, my = 0;
		int winWidth;
		int winHeight;
		
		private void OnGUI()
		{
			if (Event.current.type == EventType.Repaint)
			{
				float dt = Time.deltaTime;
				t += dt;

				int cwinWidth = Screen.width;
				int cwinHeight = Screen.height;
				winWidth = cwinWidth;
				winHeight = cwinHeight;
				
				FrameTimingManager.CaptureFrameTimings();
				FrameTimingManager.GetLatestTimings(1, frameTiming);
				frameGraph.Update(dt);
				cpuGraph.Update((float)(frameTiming[0].cpuMainThreadFrameTime / 1000.0));
				Profiler.BeginSample("NVG BeginFrame");	
				nvg.BeginFrame(winWidth, winHeight, pxRatio);
				Profiler.EndSample();
				Profiler.BeginSample("demo Render");
				demo.Render((float)mx, (float)my, winWidth, winHeight, (float)t, false);
				Profiler.EndSample();
				frameGraph.Render(5.0f, 5.0f, nvg);
				cpuGraph.Render(5.0f + 200.0f + 5.0f, 5.0f, nvg);
				Profiler.BeginSample("NVG EndFrame");
				nvg.EndFrame();
				Profiler.EndSample();
			}
		}
	}
}