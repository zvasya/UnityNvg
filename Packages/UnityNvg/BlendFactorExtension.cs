using NvgNET.Blending;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityNvg
{
	public static class BlendFactorExtension
	{
		public static BlendMode ToBlendMode(this BlendFactor blendFactor, BlendMode defaultMode)
		{
			return blendFactor switch
			{
				BlendFactor.Zero => BlendMode.Zero,
				BlendFactor.One => BlendMode.One,
				BlendFactor.SrcColour => BlendMode.SrcColor,
				BlendFactor.OneMinusSrcColour => BlendMode.OneMinusSrcColor,
				BlendFactor.DstColour => BlendMode.DstColor,
				BlendFactor.OneMinusDstColour => BlendMode.OneMinusDstColor,
				BlendFactor.SrcAlpha => BlendMode.SrcAlpha,
				BlendFactor.OneMinusSrcAlpha => BlendMode.OneMinusSrcAlpha,
				BlendFactor.DstAlpha => BlendMode.DstAlpha,
				BlendFactor.OneMinusDstAlpha => BlendMode.OneMinusDstAlpha,
				BlendFactor.SrcAlphaSaturate => BlendMode.SrcAlphaSaturate,
				_ => LogErrorAndReturn(defaultMode),
			};
		}

		private static BlendMode LogErrorAndReturn(BlendMode defaultMode)
		{
			Debug.LogWarning("Unknown blend factor");
			return defaultMode;
		}
	}
}