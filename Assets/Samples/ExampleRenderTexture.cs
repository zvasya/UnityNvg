using System.IO;
using NvgNET;
using NvgNET.Rendering.UnityNvg;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityNvg;

namespace NvgExample
{
	public class ExampleRenderTexture : MonoBehaviour
	{
		[SerializeField]
		private DemoResources _resources;
		
		CommandBufferContext context;
		UnityNvgRenderer nvgRenderer;
		Nvg nvg;
		Demo demo;
		PerformanceGraph frameGraph;
		PerformanceGraph cpuGraph;
		readonly FrameTiming[] frameTiming =  new FrameTiming[1];
		private RenderTexture _renderTexture;
		
		public float pxRatio = 1;

		private void OnEnable()
		{
			_renderTexture = new RenderTexture(2048, 2048, 32);
			context = new CommandBufferContext(true, true);
			nvgRenderer = new UnityNvgRenderer(context, false);
			nvg = Nvg.Create(nvgRenderer);
			frameGraph = new PerformanceGraph(PerformanceGraph.GraphRenderStyle.Fps, "Frame Time");
			cpuGraph = new PerformanceGraph(PerformanceGraph.GraphRenderStyle.Ms, "CPU Time");
			
			demo = new Demo(nvg, _resources);
			
			Material m = GetComponent<Renderer>().material;
			m.mainTexture = _renderTexture;
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
		
		private void Update()
		{
			{
				float dt = Time.deltaTime;
				t += dt;

				int cwinWidth = 2048;
				int cwinHeight = 2048;
				winWidth = cwinWidth;
				winHeight = cwinHeight;
				CommandBuffer cb = new CommandBuffer();
				cb.SetRenderTarget(_renderTexture);
				cb.ClearRenderTarget(true, true, Color.clear);
				
				Matrix4x4 projectionMatrix = Matrix4x4.Ortho(0, 2048, 0, 2048, -1, 1000);
				projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
				cb.SetViewProjectionMatrices(Matrix4x4.identity, projectionMatrix);
				cb.SetViewport(new Rect(0, 0, winWidth, winHeight));
				
				FrameTimingManager.CaptureFrameTimings();
				FrameTimingManager.GetLatestTimings(1, frameTiming);
				frameGraph.Update(dt);
				cpuGraph.Update((float)(frameTiming[0].cpuMainThreadFrameTime / 1000.0));
				cpuGraph.Update(dt);
				Profiler.BeginSample("NVG BeginFrame");	
				nvg.BeginFrame(winWidth, winHeight, pxRatio);
				Profiler.EndSample();
				Profiler.BeginSample("demo Render");
				context.Begin(cb);
				demo.Render((float)mx, (float)my, winWidth, winHeight, (float)t, false);
				Profiler.EndSample();
				frameGraph.Render(5.0f, 5.0f, nvg);
				cpuGraph.Render(5.0f + 200.0f + 5.0f, 5.0f, nvg);
				Profiler.BeginSample("NVG EndFrame");
				nvg.EndFrame();
				context.End();
				Graphics.ExecuteCommandBuffer(cb);
				Profiler.EndSample();
			}
		}
	}
}