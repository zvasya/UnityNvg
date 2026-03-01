#define USE_TOPOLOGY_TRIANGLE_FAN

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using NvgNET.Blending;
using NvgNET.Images;
using NvgNET.Rendering.OpenGL.Textures;
using UnityEngine.Profiling;
using UnityNvg;
using Vector2 = UnityEngine.Vector2;


namespace NvgNET.Rendering.UnityNvg
{
	public sealed class UnityNvgRenderer : INvgRenderer
	{
		public TextureManager _textureManager;
		
		private readonly IUnityNvgContext _context;
		public UnityNvgRenderer(IUnityNvgContext context, bool destroyImmediate)
		{
			_context = context;
			_textureManager = new TextureManager(destroyImmediate);
		}

		bool _disposed;

		// NvgBuffer _vertexBuffer;
		// NvgBuffer _fragUniformBuffer;

		readonly NvgList<NvgFragUniforms> _uniforms = new NvgList<NvgFragUniforms>(128);
    
		readonly List<NvgCall> _calls = new List<NvgCall>(128);
		readonly NvgList<NvgPath> _paths = new NvgList<NvgPath>(128);
		readonly NvgList<Vertex> _verts = new NvgList<Vertex>(4096);

		public struct NvgVertexConstants {
			public Vector2 ViewSize;
			public uint UniformOffset;
		}
		
		NvgVertexConstants _vertexConstants;

		public void Dispose()
		{
			if (_disposed)
				return;

			_textureManager.Dispose();
			// _vertexBuffer?.Dispose();
			// _fragUniformBuffer?.Dispose();
			_disposed = true;
		}


		public bool EdgeAntiAlias => _context.EdgeAntiAlias;
		public bool StencilStrokes => _context.StencilStrokes;
		
		public bool Create()
		{
			CreateTexture(Texture.Rgba, new Size(2, 2), 0, Span<byte>.Empty);
			return true;
		}

		public int CreateTexture(Texture type, Size size, ImageFlags imageFlags, ReadOnlySpan<byte> data)
		{
			ref var tex = ref _textureManager.AllocTexture();
			tex.Load(size, imageFlags, type, data);
			return tex.Id;
		}

		public bool DeleteTexture(int image)
		{
			return _textureManager.DeleteTexture(image);
		}

		public bool UpdateTexture(int image, Rectangle bounds, ReadOnlySpan<byte> data)
		{
			ref var tex = ref _textureManager.FindTexture(image);
			if (tex.Id == 0)
			{
				return false;
			}
			tex.Update(bounds, data);
			return true;
		}

		public bool GetTextureSize(int image, out Size size)
		{
			ref var tex = ref _textureManager.FindTexture(image);
			if (tex.Id == 0)
			{
				size = default;
				return false;
			}
			size = tex.Size;
			return false;
		}

		public void Viewport(SizeF size, float devicePixelRatio)
		{
			_vertexConstants.ViewSize = new Vector2(size.Width, size.Height);
		}

		public void Cancel()
		{
			_verts.Clear();
			_paths.Clear();
			_calls.Clear();
			_uniforms.Clear();
		}

		public void Flush()
		{
			Profiler.BeginSample("Flush");
			
			if (_calls.Count > 0)
			{
				int i;

				// NvgBuffer.Update<Vertex>(ref _vertexBuffer, _verts.Data);
				// NvgBuffer.Update<NvgFragUniforms>(ref _fragUniformBuffer, _uniforms.Data);
				_context.SetBuffer( _verts.Data, _uniforms.Data);
				
				for (i = 0; i < _calls.Count; i++)
				{
					NvgCall call = _calls[i];
					switch (call.Type)
					{
						case CallType.Fill:
							FillInternal(call);
							break;
						case CallType.ConvexFill:
							ConvexFillInternal(call);
							break;
						case CallType.Stroke:
							StrokeInternal(call);
							break;
						case CallType.Triangles:
							TrianglesInternal(call);
							break;
						case CallType.None:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			_context.Finish();
			// Reset calls
			_verts.Clear();
			_paths.Clear();
			_calls.Clear();
			_uniforms.Clear();
			Profiler.EndSample();
		}


		private void RemoveTextures()
		{
			
		}

		public void Fill(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, float fringe, RectangleF bounds, ReadOnlySpan<Path> paths1)
		{
			int i, maxverts, offset;

			var type = CallType.Fill;
			var triangleCount = 4;
			var pathOffset = _paths.EnsureCapacity(paths1.Length);
			if (pathOffset == -1)
			{
				return;
			}

			var pathCount = paths1.Length;
			var image = paint.Image;

			if (paths1.Length == 1 && paths1[0].Convex)
			{
				type = CallType.ConvexFill;
				triangleCount = 0; // Bounding box fill quad not needed for convex fill
			}

			// Allocate vertices for all the paths.
#if !USE_TOPOLOGY_TRIANGLE_FAN
			maxverts = MaxVertCountList(paths1) + triangleCount;
#else
        maxverts = MaxVertCount(paths1) + triangleCount;
#endif
			offset = _verts.EnsureCapacity(maxverts);
			if (offset == -1)
			{
				return;
			}

			var vertsData = _verts.Data;
			for (i = 0; i < paths1.Length; i++)
			{
				ref NvgPath copy = ref _paths.Data[pathOffset + i];
				Path path = paths1[i];
				copy = new NvgPath();
				if (path.FillCount > 0)
				{
					var pathFill = path.Fill;
					copy.FillOffset = (uint)offset;
#if !USE_TOPOLOGY_TRIANGLE_FAN
					copy.FillCount = (pathFill.Length - 2) * 3;
					int j;
					for (j = 0; j < pathFill.Length - 2; j++)
					{
						vertsData[offset] = pathFill[0];
						vertsData[offset + 1] = pathFill[j + 1];
						vertsData[offset + 2] = pathFill[j + 2];
						offset += 3;
					}
#else
				copy.FillCount = path.FillCount;
				path.Fill.Slice(0, path.FillCount).CopyTo(vertsData.Slice( offset, path.FillCount));   
				offset += path.FillCount;
#endif
				}

				if (path.StrokeCount > 0)
				{
					copy.StrokeOffset = offset;
					copy.StrokeCount = path.StrokeCount;
					path.Stroke.CopyTo(vertsData.Slice(offset, path.StrokeCount));
					offset += path.StrokeCount;
				}
			}

			// Setup uniforms for draw calls
			int uniformOffset;
			int triangleOffset = 0;
			if (type == CallType.Fill)
			{
				// Quad
				triangleOffset = offset;
				vertsData[triangleOffset] = new Vertex(bounds.Right, bounds.Bottom, 0.5f, 1.0f);
				vertsData[triangleOffset + 1] = new Vertex(bounds.Right, bounds.Top, 0.5f, 1.0f);
				vertsData[triangleOffset + 2] = new Vertex(bounds.Left, bounds.Bottom, 0.5f, 1.0f);
				vertsData[triangleOffset + 3] = new Vertex(bounds.Left, bounds.Top, 0.5f, 1.0f);

				uniformOffset = _uniforms.EnsureCapacity(2);
				if (uniformOffset == -1)
				{
					return;
				}

				// Simple shader for stencil
				ref NvgFragUniforms frag = ref _uniforms.Data[uniformOffset];
				frag = new NvgFragUniforms();
				frag.StrokeThr = -1.0f;
				frag.Type = ShaderType.Simple;
				// Fill shader
				ConvertPaint(out _uniforms.Data[uniformOffset + 1], paint, scissor, fringe, fringe, -1.0f);
			}
			else
			{
				uniformOffset = _uniforms.EnsureCapacity(1);
				if (uniformOffset == -1)
				{
					return;
				}

				// Fill shader
				ConvertPaint(out _uniforms.Data[uniformOffset], paint, scissor, fringe, fringe, -1.0f);
			}

			NvgCall call = new NvgCall(type, image, pathOffset, pathCount, triangleOffset, triangleCount, uniformOffset, compositeOperation);
			_calls.Add(call);
		}

		public void Stroke(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, float fringe, float strokeWidth, ReadOnlySpan<Path> paths1)
		{
			var pathCount = paths1.Length;
			int i;

			var pathOffset = _paths.EnsureCapacity(pathCount);
			if (pathOffset == -1)
			{
				return;
			}

			var image = paint.Image;

			// Allocate vertices for all the paths.
			var maxVerts = MaxVertCount(paths1);
			var offset = _verts.EnsureCapacity(maxVerts);
			if (offset == -1)
			{
				return;
			}

			for (i = 0; i < pathCount; i++)
			{
				ref NvgPath copy = ref _paths.Data[pathOffset + i];
				Path path = paths1[i];
				copy = new NvgPath();
				if (path.StrokeCount > 0)
				{
					copy.StrokeOffset = offset;
					copy.StrokeCount = path.StrokeCount;
					path.Stroke.CopyTo(_verts.Data.Slice(offset, path.StrokeCount));
					offset += path.StrokeCount;
				}
			}

			int uniformOffset;
			if (StencilStrokes)
			{
				// Fill shader
				uniformOffset = _uniforms.EnsureCapacity(2);
				if (uniformOffset == -1)
				{
					return;
				}

				ConvertPaint(out _uniforms.Data[uniformOffset], paint, scissor, strokeWidth, fringe, -1.0f);
				int i1 = uniformOffset + 1;
				ConvertPaint(out _uniforms.Data[i1], paint, scissor, strokeWidth, fringe, 1.0f - 0.5f / 255.0f);
			}
			else
			{
				// Fill shader
				uniformOffset = _uniforms.EnsureCapacity(1);
				if (uniformOffset == -1)
				{
					return;
				}

				ConvertPaint(out _uniforms.Data[uniformOffset], paint, scissor, strokeWidth, fringe, -1.0f);
			}

			NvgCall call = new NvgCall(CallType.Stroke, image, pathOffset, pathCount, 0, 0, uniformOffset, compositeOperation);
			_calls.Add(call);
		}

		public void Triangles(Paint paint, CompositeOperationState compositeOperation, Scissor scissor, ReadOnlySpan<Vertex> vertices, float fringeWidth)
		{
			int nverts = vertices.Length;

			var image = paint.Image;

			// Allocate vertices for all the paths.
			var triangleOffset = _verts.EnsureCapacity(nverts);
			if (triangleOffset == -1)
			{
				return;
			}

			vertices.CopyTo(_verts.Data.Slice(triangleOffset, nverts));
        
			// Fill shader
			var uniformOffset = _uniforms.EnsureCapacity(1);
			if (uniformOffset == -1)
			{
				return;
			}

			ref NvgFragUniforms frag = ref _uniforms.Data[uniformOffset];
			ConvertPaint(out frag, paint, scissor, 1.0f, fringeWidth, -1.0f);
			frag.Type = ShaderType.Image;

			NvgCall call = new NvgCall(CallType.Triangles, image, 0, 0, triangleOffset, nverts, uniformOffset, compositeOperation);
			_calls.Add(call);
		}

		void FillInternal(in NvgCall call)
		{
			Profiler.BeginSample("FillInternal");
			Span<NvgPath> paths = _paths.Data.Slice(call.PathOffset, call.PathCount);
			int npaths = paths.Length;

			var compositeOperation = call.CompositeOperation;
#if !USE_TOPOLOGY_TRIANGLE_FAN
			var topology = PrimitiveTopology.TriangleList;
#else
	        var topology = PrimitiveTopology.TriangleFan;
#endif
			var stencilFill = true;

			MaterialKey materialKey = new MaterialKey(topology: topology,
				stencilFill: stencilFill, compositeOperation: compositeOperation);

			_context.BindMaterial(ref materialKey);
			_context.SetUniforms(call.UniformOffset);
			_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
			
			for (int i = 0; i < npaths; i++)
			{
				_context.Draw((int)paths[i].FillOffset, paths[i].FillCount);
			}

			
			if (EdgeAntiAlias)
			{
				materialKey = materialKey.With
				(
					compositeOperation: call.CompositeOperation,
					topology: PrimitiveTopology.TriangleStrip,
					stencilFill: false,
					stencilTest: true,
					edgeAa: true
				);
				_context.BindMaterial(ref materialKey);
				_context.SetUniforms(call.UniformOffset + 1);
				_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
				// Draw fringes
				for (int i = 0; i < npaths; ++i)
				{
					_context.Draw(paths[i].StrokeOffset, paths[i].StrokeCount);
				}
			}

			materialKey = materialKey.With(
				compositeOperation: call.CompositeOperation,
				topology: PrimitiveTopology.TriangleStrip,
				stencilFill: false,
				stencilTest: true,
				edgeAa: false
			);
			
			_context.BindMaterial(ref materialKey);
			_context.SetUniforms( call.UniformOffset + 1);
			_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
			
			_context.Draw(call.TriangleOffset, call.TriangleCount);
			Profiler.EndSample();
		}
		


		void ConvexFillInternal(in NvgCall call)
		{
			Profiler.BeginSample("ConvexFillInternal");
			Span<NvgPath> paths = _paths.Data.Slice(call.PathOffset, call.PathCount);
			int npaths = paths.Length;

        
			var compositeOperation = call.CompositeOperation;
#if !USE_TOPOLOGY_TRIANGLE_FAN
			var topology = PrimitiveTopology.TriangleList;
#else
        var topology = PrimitiveTopology.TriangleFan;
#endif

			MaterialKey materialKey = new MaterialKey(topology: topology, compositeOperation: compositeOperation);
        
			_context.BindMaterial(ref materialKey);
			_context.SetUniforms(call.UniformOffset);
			_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
			
			for (int i = 0; i < npaths; ++i)
			{
				_context.Draw((int)paths[i].FillOffset, paths[i].FillCount);
			}

			if (EdgeAntiAlias)
			{
				materialKey = materialKey.With(topology: PrimitiveTopology.TriangleStrip);
				_context.BindMaterial(ref materialKey);
				_context.SetUniforms( call.UniformOffset);
				_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
				// Draw fringes
				for (int i = 0; i < npaths; ++i)
				{
					_context.Draw(paths[i].StrokeOffset, paths[i].StrokeCount);
				}
			}
			Profiler.EndSample();
		}

		void StrokeInternal(in NvgCall call)
		{
			Profiler.BeginSample("StrokeInternal");
			Span<NvgPath> paths = _paths.Data.Slice(call.PathOffset, call.PathCount);
			int npaths = paths.Length;

			if (StencilStrokes)
			{
				MaterialKey materialKey = new MaterialKey(
					topology: PrimitiveTopology.TriangleStrip,
					compositeOperation: call.CompositeOperation,
					// Fill stencil with 1 if stencil EQUAL passes
					stencilStroke: StencilSetting.StencilStrokeFill
				);
			
			
				_context.BindMaterial(ref materialKey);
				_context.SetUniforms( call.UniformOffset + 1);
				
				_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
			
				for (int i = 0; i < npaths; ++i)
				{
					_context.Draw(paths[i].StrokeOffset, paths[i].StrokeCount);
				}
			
				// //Draw AA shape if stencil EQUAL passes
				materialKey = materialKey.With(stencilStroke: StencilSetting.StencilStrokeDrawAA);
				_context.BindMaterial(ref materialKey);
				_context.SetUniforms( call.UniformOffset);
				_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
				
				for (int i = 0; i < npaths; ++i)
				{
					_context.Draw(paths[i].StrokeOffset, paths[i].StrokeCount);
				}
			
				// Fill stencil with 0, always
				materialKey = materialKey.With(stencilStroke: StencilSetting.StencilStrokeClear);
				_context.BindMaterial(ref materialKey);
				_context.SetUniforms( call.UniformOffset);
				_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
			
				for (int i = 0; i < npaths; ++i)
				{
					_context.Draw(paths[i].StrokeOffset, paths[i].StrokeCount);
				}
			}
			else
			{
				MaterialKey materialKey = new MaterialKey(topology: PrimitiveTopology.TriangleStrip, stencilFill: false, compositeOperation: call.CompositeOperation);

				_context.BindMaterial(ref materialKey);
				_context.SetUniforms( call.UniformOffset);
				_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
				
				// Draw Strokes

				for (int i = 0; i < npaths; ++i)
				{
					_context.Draw(paths[i].StrokeOffset, paths[i].StrokeCount);
				}
			}
			Profiler.EndSample();
		}

		void TrianglesInternal(in NvgCall call)
		{
			if (call.TriangleCount == 0)
			{
				return;
			}
			Profiler.BeginSample("TrianglesInternal");

			MaterialKey materialKey = new MaterialKey(topology: PrimitiveTopology.TriangleList,
				stencilFill: false, compositeOperation: call.CompositeOperation);

			_context.BindMaterial(ref materialKey);
			_context.SetUniforms( call.UniformOffset);
			_context.SetTexture(_textureManager.FindTexture(call.Image != 0 ? call.Image : 1).Texture);
			
			_context.Draw(call.TriangleOffset, call.TriangleCount);
			Profiler.EndSample();
		}
		
		static int MaxVertCount(ReadOnlySpan<Path> paths)
		{
			int i, count = 0;
			for (i = 0; i < paths.Length; i++)
			{
				count += paths[i].FillCount;
				count += paths[i].StrokeCount;
			}

			return count;
		}

		static int MaxVertCountList(ReadOnlySpan<Path> paths)
		{
			int i, count = 0;
			for (i = 0; i < paths.Length; i++)
			{
				count += (paths[i].FillCount - 2) * 3;
				count += paths[i].StrokeCount;
			}

			return count;
		}

		private NvgTexture FindTexture(int id)
		{
			return _textureManager.FindTexture(id);
		}

		private void ConvertPaint(
			out NvgFragUniforms frag,
			Paint paint,
			Scissor scissor,
			float width,
			float fringe,
			float strokeThr
		)
		{
			Matrix3x2 invTransform;

			frag = new NvgFragUniforms
			{
				InnerCol = paint.InnerColour.Premult(),
				OuterCol = paint.OuterColour.Premult(),
			};

			if (scissor.Extent.Width < -0.5f || scissor.Extent.Height < -0.5f)
			{
				frag.ScissorMat = new System.Numerics.Matrix4x4();
				frag.ScissorExt.X = 1.0f;
				frag.ScissorExt.Y = 1.0f;
				frag.ScissorScale.X = 1.0f;
				frag.ScissorScale.Y = 1.0f;
			}
			else
			{
				Matrix3x2 scissorMat = scissor.Transform;
				Matrix3x2.Invert(scissorMat, out invTransform);
				frag.ScissorMat = new System.Numerics.Matrix4x4(invTransform);
				frag.ScissorExt.X = scissor.Extent.Width;
				frag.ScissorExt.Y = scissor.Extent.Height;

				frag.ScissorScale = new System.Numerics.Vector2(
					MathF.Sqrt(scissorMat.M11 * scissorMat.M11 + scissorMat.M21 * scissorMat.M21) / fringe,
					MathF.Sqrt(scissorMat.M21 * scissorMat.M21 + scissorMat.M22 * scissorMat.M22) / fringe
				);
			}

			frag.Extent = paint.Extent.ToVector2();
			frag.StrokeMult = (width * 0.5f + fringe * 0.5f) / fringe;
			frag.StrokeThr = strokeThr;

			if (paint.Image != 0)
			{
				NvgTexture tex = FindTexture(paint.Image);
				if (tex.Texture == null) return;
				if ((tex.Flags & ImageFlags.FlipY) != 0)
				{
					Matrix3x2 m1, m2;
					m1 = Matrix3x2.CreateTranslation(0.0f, frag.Extent.Y * 0.5f);
					m1 = Transforms.NvgTransforms.Multiply(m1, paint.Transform);
					m2 = Matrix3x2.CreateScale(1.0f, -1.0f);
					m2 = Transforms.NvgTransforms.Multiply(m2, m1);
					m1 = Matrix3x2.CreateTranslation(0.0f, -frag.Extent.Y * 0.5f);
					m1 = Transforms.NvgTransforms.Multiply(m1, m2);
					Matrix3x2.Invert(m1, out invTransform);
				}
				else
				{
					Matrix3x2.Invert(paint.Transform, out invTransform);
				}

				frag.Type = ShaderType.FillImage;

				if (tex.TextureType == Texture.Rgba)
					frag.TexType = (tex.Flags & ImageFlags.Premultiplied) == ImageFlags.Premultiplied ? 0 : 1;
				else
					frag.TexType = 2;
			}
			else
			{
				frag.Type = ShaderType.FillGradient;
				frag.Radius = paint.Radius;
				frag.Feather = paint.Feather;
				Matrix3x2.Invert(paint.Transform, out invTransform);
			}

			frag.PaintMat = new System.Numerics.Matrix4x4(invTransform);
		}
	}
}
