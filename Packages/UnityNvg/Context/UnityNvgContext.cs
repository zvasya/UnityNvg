using System;
using NvgNET.Rendering;
using NvgNET.Rendering.UnityNvg;
using UnityEngine;

namespace UnityNvg
{
	public abstract class UnityNvgContext : IUnityNvgContext
	{
		protected static readonly int VertexBufferPropID = Shader.PropertyToID("_VertexBuffer");
		protected static readonly int DataPropID = Shader.PropertyToID("data");
		protected static readonly int OffsetPropID = Shader.PropertyToID("_Offset");
		protected static readonly int UniformOffsetPropID = Shader.PropertyToID("_UniformOffset");
		
		
		public bool EdgeAntiAlias { get; }
		public bool StencilStrokes { get; }

		protected UnityNvgContext(bool edgeAntiAlias, bool stencilStrokes)
		{
			EdgeAntiAlias = edgeAntiAlias;
			StencilStrokes = stencilStrokes;
		}

		public abstract void BindMaterial(ref MaterialKey materialKey);
		
		public abstract void SetBuffer(Span<Vertex> vertexData, Span<NvgFragUniforms> vertsData);
		public abstract void SetTexture(Texture2D texture);

		public abstract void SetUniforms(int uniformOffset);

		public abstract void Draw(int offset, int count);
		public abstract void Finish();
		
		protected int GetVertexCount(PrimitiveTopology topology, int fillCount)
		{
			return topology switch
			{
				PrimitiveTopology.TriangleList => fillCount,
				PrimitiveTopology.TriangleStrip or PrimitiveTopology.TriangleFan => (fillCount - 2) * 3,
				_ => throw new ArgumentOutOfRangeException(nameof(topology), topology, null)
			};
		}
	}
}