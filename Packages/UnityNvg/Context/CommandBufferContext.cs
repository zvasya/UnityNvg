using System;
using System.Collections.Generic;
using NvgNET.Rendering;
using NvgNET.Rendering.UnityNvg;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityNvg
{
	public sealed class CommandBufferContext : UnityNvgContext
	{
		private static readonly int MainTexPropID = Shader.PropertyToID("_MainTex");
		
		private Material _currentMaterial;
		private PrimitiveTopology _currentTopology;
		private CommandBuffer _commandBuffer;
		
		private int _currentFrame = 0;
		private readonly int _maxFrames;
		
		private readonly NvgBuffer[] _vertexBuffers;
		private readonly NvgBuffer[] _fragUniformBuffers;
		
		private readonly Dictionary<MaterialKey, List<Material>> _materialsCache = new Dictionary<MaterialKey, List<Material>>();
		private readonly Dictionary<MaterialKey, List<Material>> _materialsUsed = new Dictionary<MaterialKey, List<Material>>();
		
		public CommandBufferContext(bool edgeAntiAlias, bool stencilStrokes, int maxFrames = 3) : base(edgeAntiAlias, stencilStrokes)
		{
			_maxFrames = maxFrames;
			_vertexBuffers = new NvgBuffer[_maxFrames];
			_fragUniformBuffers =  new NvgBuffer[_maxFrames];
		}

		public void Begin(CommandBuffer commandBuffer)
		{
			_commandBuffer = commandBuffer;
		}
		
		public CommandBuffer End()
		{
			CommandBuffer cb = _commandBuffer;
			_commandBuffer = null;
			return cb;
		}

		public override void BindMaterial(ref MaterialKey materialKey)
		{
			List<Material> usedMaterials;
			if (!_materialsCache.TryGetValue(materialKey, out List<Material> materials))
			{
				materials = _materialsCache[materialKey] = new List<Material>(16);
				usedMaterials = _materialsUsed[materialKey] = new List<Material>(16);
			}
			else
				usedMaterials = _materialsUsed[materialKey];

			Material material;
			if (materials.Count > 0)
			{
				material = materials[^1];
				materials.RemoveAt(materials.Count - 1);
			}
			else
				material = materialKey.CreateMaterial(EdgeAntiAlias);

			usedMaterials.Add(material);
			
			_currentTopology = materialKey.Topology;
			_currentMaterial = material;
		}

		public override void SetBuffer(Span<Vertex> vertexData, Span<NvgFragUniforms> fragUniformData)
		{
			NvgBuffer.UpdateB<Vertex>(ref _vertexBuffers[_currentFrame], vertexData);
			NvgBuffer.UpdateB<NvgFragUniforms>(ref _fragUniformBuffers[_currentFrame], fragUniformData);
			
			_commandBuffer.SetGlobalBuffer(VertexBufferPropID, _vertexBuffers[_currentFrame]!.Buffer);
			_commandBuffer.SetGlobalBuffer(DataPropID, _fragUniformBuffers[_currentFrame]!.Buffer);
		}

		public override void SetTexture(Texture2D texture)
		{
			_commandBuffer.SetGlobalTexture(MainTexPropID, texture);
		}

		public override void SetUniforms(int uniformOffset)
		{
			// material.SetVector("_ViewSize", _vertexConstants.ViewSize);
			_commandBuffer.SetGlobalInteger(UniformOffsetPropID, uniformOffset);
		}

		public override void Draw(int offset, int count)
		{
			_commandBuffer.SetGlobalInteger(OffsetPropID, offset);
			int vertexCount = GetVertexCount(_currentTopology, count);
			_commandBuffer.DrawProcedural(Matrix4x4.identity, _currentMaterial, 0, MeshTopology.Triangles, vertexCount);
		}

		public override void Finish()
		{
			_currentFrame = (_currentFrame + 1) % _maxFrames;
			foreach ((MaterialKey key, List<Material> value) in _materialsUsed)
			{
				List<Material> cache = _materialsCache[key];
				cache.AddRange(value);
				value.Clear();
			}
		}
	}
}