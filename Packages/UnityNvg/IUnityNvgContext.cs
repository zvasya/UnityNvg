using System;
using NvgNET.Rendering;
using NvgNET.Rendering.UnityNvg;
using UnityEngine;

namespace UnityNvg
{
	public interface IUnityNvgContext
	{
		bool EdgeAntiAlias { get; }
		bool StencilStrokes { get; }
		
		void BindMaterial(ref MaterialKey materialKey);
		
		void SetBuffer(Span<Vertex> vertexData, Span<NvgFragUniforms> fragUniformData);
		void SetTexture(Texture2D texture);

		void SetUniforms(int uniformOffset);

		void Draw(int offset, int count);
		void Finish();
	}
}