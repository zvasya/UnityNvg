using System;
using System.Collections.Generic;
using NvgNET.Rendering;
using NvgNET.Rendering.UnityNvg;
using UnityEngine;

namespace UnityNvg
{
	public sealed class ImmediateModeNvgContext : UnityNvgContext
	{
		private Material _currentMaterial;
		private PrimitiveTopology _currentTopology;

		private NvgBuffer _vertexBuffer;
		private NvgBuffer _fragUniformBuffer;
		
		private readonly Dictionary<MaterialKey, Material> _materialsCache = new Dictionary<MaterialKey, Material>();

		public ImmediateModeNvgContext(bool edgeAntiAlias, bool stencilStrokes) : base(edgeAntiAlias, stencilStrokes)
		{
		}
		
		public override void BindMaterial(ref MaterialKey materialKey)
		{
			if (!_materialsCache.TryGetValue(materialKey, out Material material))
			{
				material = materialKey.CreateMaterial(EdgeAntiAlias);
				_materialsCache.Add(materialKey, material);
			}
			
			material.SetBuffer(VertexBufferPropID, _vertexBuffer.Buffer);
			material.SetBuffer(DataPropID, _fragUniformBuffer.Buffer);

			_currentTopology = materialKey.Topology;
			_currentMaterial = material;
		}

		public override void SetBuffer(Span<Vertex> vertexData, Span<NvgFragUniforms> fragUniformData)
		{
			NvgBuffer.UpdateB<Vertex>(ref _vertexBuffer, vertexData);
			NvgBuffer.UpdateB<NvgFragUniforms>(ref _fragUniformBuffer, fragUniformData);
		}

		public override void SetTexture(Texture2D texture)
		{
			_currentMaterial.mainTexture = texture;
		}

		public override void SetUniforms(int uniformOffset)
		{
			// material.SetVector("_ViewSize", _vertexConstants.ViewSize);
			_currentMaterial.SetInteger(UniformOffsetPropID, uniformOffset);
		}

		public override void Draw(int offset, int count)
		{
			var material = _currentMaterial;
				
			material.SetInteger(OffsetPropID, offset);
			material.SetPass(0);
			int vertexCount = GetVertexCount(_currentTopology, count);
			Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount);
		}

		public override void Finish()
		{
		}
	}
}