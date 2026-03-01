using NvgNET.Blending;

namespace NvgNET.Rendering.UnityNvg
{
	public readonly struct NvgCall
	{
		public readonly CallType Type;
		public readonly int Image;
		public readonly int PathOffset;
		public readonly int PathCount;
		public readonly int TriangleOffset;
		public readonly int TriangleCount;
		public readonly int UniformOffset;
		public readonly CompositeOperationState CompositeOperation;

		public NvgCall(CallType type,
			int image,
			int pathOffset,
			int pathCount,
			int triangleOffset,
			int triangleCount,
			int uniformOffset,
			CompositeOperationState compositeOperation)
		{
			Type = type;
			Image = image;
			PathOffset = pathOffset;
			PathCount = pathCount;
			TriangleOffset = triangleOffset;
			TriangleCount = triangleCount;
			UniformOffset = uniformOffset;
			CompositeOperation = compositeOperation;
		}
	}
}