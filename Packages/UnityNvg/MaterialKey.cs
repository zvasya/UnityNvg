using System;
using NvgNET.Blending;
using UnityEngine;
using UnityEngine.Rendering;
using UnityNvg;

namespace NvgNET.Rendering.UnityNvg
{
    public readonly struct MaterialKey : IEquatable<MaterialKey>
    {
        private static readonly int SrcBlendColorPropID = Shader.PropertyToID("_SrcBlendColor");
        private static readonly int SrcBlendAlphaPropID = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendColorPropID = Shader.PropertyToID("_DstBlendColor");
        private static readonly int DstBlendAlphaPropID = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ClipRefPropID = Shader.PropertyToID("_ClipRef");
        private static readonly int ClipReadMaskPropID = Shader.PropertyToID("_ClipReadMask");
        private static readonly int ClipWriteMaskPropID = Shader.PropertyToID("_ClipWriteMask");
        private static readonly int ClipCompBackPropID = Shader.PropertyToID("_ClipCompBack");
        private static readonly int ClipPassBackPropID = Shader.PropertyToID("_ClipPassBack");
        private static readonly int ClipFailBackPropID = Shader.PropertyToID("_ClipFailBack");
        private static readonly int ClipZFailBackPropID = Shader.PropertyToID("_ClipZFailBack");
        private static readonly int ClipCompFrontPropID = Shader.PropertyToID("_ClipCompFront");
        private static readonly int ClipPassFrontPropID = Shader.PropertyToID("_ClipPassFront");
        private static readonly int ClipFailFrontPropID = Shader.PropertyToID("_ClipFailFront");
        private static readonly int ClipZFailFrontPropID = Shader.PropertyToID("_ClipZFailFront");
        private static readonly int ModePropID = Shader.PropertyToID("_CullMode");
        private static readonly int WriteMaskPropID = Shader.PropertyToID("_ColorWriteMask");

        public readonly StencilSetting StencilStroke;
        public readonly bool StencilFill;
        public readonly bool StencilTest;
        public readonly bool EdgeAa;
        public readonly PrimitiveTopology Topology;
        public readonly CompositeOperationState CompositeOperation;
        public readonly ColorWriteMask ColorWriteMask; // set and compare independently
        readonly int _hashCode;

        public MaterialKey(
            PrimitiveTopology topology,
            StencilSetting stencilStroke = StencilSetting.StencilStrokeUndefined,
            bool stencilFill = false,
            bool stencilTest = false,
            bool edgeAa = false,
            CompositeOperationState compositeOperation = default
        )
        {
            StencilStroke = stencilStroke;
            StencilFill = stencilFill;
            StencilTest = stencilTest;
            EdgeAa = edgeAa;
            Topology = topology;
            CompositeOperation = compositeOperation;
            ColorWriteMask = StencilStroke == StencilSetting.StencilStrokeClear || StencilFill
                ? (ColorWriteMask)0
                : ColorWriteMask.All;
            
            _hashCode = 0;
            _hashCode = CalcHashCode();
        }

        int CalcHashCode()
        {
            int hash = (int)Topology + (StencilTest ? 1 << 5: 0) + (StencilFill ? 1 << 6 : 0) + ((int)StencilStroke << 7) + ((int)ColorWriteMask << 8);
            return HashCode.Combine(hash, EdgeAa, CompositeOperation);
        }

        public MaterialKey With(
            StencilSetting? stencilStroke = null,
            bool? stencilFill = null,
            bool? stencilTest = null,
            bool? edgeAa = null,
            PrimitiveTopology? topology = null,
            CompositeOperationState? compositeOperation = null)
        {
            return new MaterialKey
            (topology ?? this.Topology,
                stencilStroke ?? this.StencilStroke, stencilFill ?? this.StencilFill, stencilTest ?? this.StencilTest, edgeAa ?? this.EdgeAa, compositeOperation ?? this.CompositeOperation);
        }

        public CullMode CullMode => StencilFill ? CullMode.Off : CullMode.Front;

        public void SetBlendMode(Material material)
        {
            material.SetInteger(SrcBlendColorPropID, (int)CompositeOperation.SrcRgb.ToBlendMode(BlendMode.One));
            material.SetInteger(SrcBlendAlphaPropID, (int)CompositeOperation.SrcAlpha.ToBlendMode(BlendMode.OneMinusSrcAlpha));
            material.SetInteger(DstBlendColorPropID, (int)CompositeOperation.DstRgb.ToBlendMode(BlendMode.One));
            material.SetInteger(DstBlendAlphaPropID, (int)CompositeOperation.DstAlpha.ToBlendMode(BlendMode.OneMinusSrcAlpha));
        }
        
        public void SetStencil(Material material)
        {
            CompareFunction backCompareOp = CompareFunction.Always;
            StencilOp backPassOp = StencilOp.Keep;
            StencilOp backFailOp = StencilOp.Keep;
            StencilOp backDepthFailOp = StencilOp.Keep;
            
            CompareFunction frontCompareOp = CompareFunction.Always;
            StencilOp frontPassOp = StencilOp.Keep;
            StencilOp frontFailOp = StencilOp.Keep;
            StencilOp frontDepthFailOp = StencilOp.Keep;

            byte reference = 0x00;
            byte compareMask = 0xFF;
            byte writeMask = 0xFF;
            
            if (StencilStroke != StencilSetting.StencilStrokeUndefined)
            {
                // enables
                frontFailOp = StencilOp.Keep;
                frontDepthFailOp = StencilOp.Keep;
                frontPassOp = StencilOp.Keep;
                frontCompareOp = CompareFunction.Equal;
                reference = 0x00;
                compareMask = 0xff;
                writeMask = 0xff;
                
                backCompareOp = frontCompareOp;
                backFailOp = frontFailOp;
                backDepthFailOp = frontDepthFailOp;
                
                backPassOp = StencilOp.DecrementSaturate;

                switch (StencilStroke)
                {
                    case StencilSetting.StencilStrokeFill:
                        frontPassOp = StencilOp.IncrementSaturate;
                        backPassOp = StencilOp.DecrementSaturate;
                        break;
                    case StencilSetting.StencilStrokeDrawAA:
                        frontPassOp = StencilOp.Keep;
                        backPassOp = StencilOp.Keep;
                        break;
                    case StencilSetting.StencilStrokeClear:
                        frontFailOp = StencilOp.Zero;
                        frontDepthFailOp = StencilOp.Zero;
                        frontPassOp = StencilOp.Zero;
                        frontCompareOp = CompareFunction.Always;
                        
                        backCompareOp = frontCompareOp;
                        backPassOp = frontPassOp;
                        backFailOp = frontFailOp;
                        backDepthFailOp = frontDepthFailOp;
                        break;
                }
            }
            else
            {
                if (StencilFill)
                {
                    frontCompareOp = CompareFunction.Always;
                    frontFailOp = StencilOp.Keep;
                    frontDepthFailOp = StencilOp.Keep;
                    frontPassOp = StencilOp.IncrementWrap;
                    reference = 0x0;
                    compareMask = 0xff;
                    writeMask = 0xff;
                    backCompareOp = frontCompareOp;
                    backFailOp = frontFailOp;
                    backDepthFailOp = frontDepthFailOp;
                    backPassOp = StencilOp.DecrementWrap;
                }
                else if (StencilTest)
                {
                    if (EdgeAa)
                    {
                        frontCompareOp = CompareFunction.Equal;
                        reference = 0x0;
                        compareMask = 0xff;
                        writeMask = 0xff;
                        frontFailOp = StencilOp.Keep;
                        frontDepthFailOp = StencilOp.Keep;
                        frontPassOp = StencilOp.Keep;
                    }
                    else
                    {
                        frontCompareOp = CompareFunction.NotEqual;
                        reference = 0x0;
                        compareMask = 0xff;
                        writeMask = 0xff;
                        frontFailOp = StencilOp.Zero;
                        frontDepthFailOp = StencilOp.Zero;
                        frontPassOp = StencilOp.Zero;
                    }

                    backCompareOp = frontCompareOp;
                    backPassOp = frontPassOp;
                    backFailOp = frontFailOp;
                    backDepthFailOp = frontDepthFailOp;
                }

            }

            material.SetInteger(ClipRefPropID, reference );
            material.SetInteger(ClipReadMaskPropID, compareMask );
            material.SetInteger(ClipWriteMaskPropID, writeMask );
            
            material.SetInteger(ClipCompBackPropID, (int) backCompareOp );
            material.SetInteger(ClipPassBackPropID, (int) backPassOp );
            material.SetInteger(ClipFailBackPropID, (int) backFailOp );
            material.SetInteger(ClipZFailBackPropID, (int) backDepthFailOp );
            
            material.SetInteger(ClipCompFrontPropID, (int) frontCompareOp );
            material.SetInteger(ClipPassFrontPropID, (int) frontPassOp );
            material.SetInteger(ClipFailFrontPropID, (int) frontFailOp );
            material.SetInteger(ClipZFailFrontPropID, (int) frontDepthFailOp );
        }

        public Material CreateMaterial(bool antialias)
        {
            Material material = new Material(Shader.Find("Nvg/Shader"));

            switch (Topology)
            {
                case PrimitiveTopology.TriangleList:
                    break;
                case PrimitiveTopology.TriangleStrip:
                    material.EnableKeyword("TRIANGLE_STRIP");
                    break;
                case PrimitiveTopology.TriangleFan:
                    material.EnableKeyword("TRIANGLE_FAN");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            material.SetInteger(ModePropID, (int)CullMode);

            material.SetInteger(WriteMaskPropID, (int)ColorWriteMask);
            

            SetBlendMode(material);
            SetStencil(material);
            

            if (antialias)
                material.EnableKeyword("EDGE_AA");

            return material;
        }

        public override int GetHashCode() => _hashCode;

        public bool Equals(MaterialKey other)
        {
            if (Topology != other.Topology ||
                StencilTest != other.StencilTest ||
                StencilFill != other.StencilFill ||
                StencilStroke != other.StencilStroke ||
                ColorWriteMask != other.ColorWriteMask ||
                EdgeAa != other.EdgeAa ||
                CompositeOperation != other.CompositeOperation)
                return false;

            return true;
        }
    }
}