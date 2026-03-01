Shader "Nvg/Shader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        
        Blend [_SrcBlendColor] [_DstBlendColor], [_SrcBlendAlpha] [_DstBlendAlpha]

        ZWrite Off ZTest LEqual Cull [_CullMode]
		Stencil 
		{
			Ref [_ClipRef]
            ReadMask [_ClipReadMask]
            WriteMask [_ClipWriteMask]
            CompBack [_ClipCompBack]
            PassBack [_ClipPassBack]
            FailBack [_ClipFailBack]
            ZFailBack [_ClipZFailBack]
            CompFront [_ClipCompFront]
            PassFront [_ClipPassFront]
            FailFront [_ClipFailFront]
            ZFailFront [_ClipZFailFront]
		}

        Pass
        {
            ColorMask [_ColorWriteMask]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile __ EDGE_AA
            #pragma multi_compile __ TRIANGLE_STRIP TRIANGLE_FAN
            
            #include "UnityCG.cginc"
                        
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 fpos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };
            
			StructuredBuffer<float4> _VertexBuffer;
            uint _Offset;
            float2 _ViewSize;
            int _UniformOffset;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (uint id : SV_VertexID)
            {
                uint index = id;
                #if (TRIANGLE_STRIP)
                {
                    uint primitive = id / 3u;
                    uint remainder = id % 3u;
                    bool isEven = (primitive % 2u) == 0u;
                    index = isEven ? primitive + remainder : primitive + abs(int(remainder) - 3) * sign(remainder);
                }
                #elif (TRIANGLE_FAN)
                {
                    uint primitive = id / 3u;
                    uint remainder = id % 3u;
                    index = primitive * sign(remainder) + remainder;
                }
                #endif
                float4 vertex = _VertexBuffer[index + _Offset]; 
                float4 position = float4(vertex.x, vertex.y, 0, 1);
                float2 uv = vertex.zw;
                
                v2f o;
                o.fpos = vertex.xy;
                o.vertex = UnityObjectToClipPos(position);
                o.uv = TRANSFORM_TEX(uv, _MainTex);
                return o;
            }
            
            struct FragmentData {
                float4x4 scissorMat;
                float4x4 paintMat;
                float4 innerCol;
                float4 outerCol;
                float2 scissorExt;
                float2 scissorScale;
                float2 extent;
                float radius;
                float feather;
                float strokeMult;
                float strokeThr;
                int texType;
                int type;
            };
			StructuredBuffer<FragmentData> data;

            float sdroundrect(float2 pt, float2 ext, float rad) {
                float2 ext2 = ext - float2(rad, rad);
                float2 d = abs(pt) - ext2;
                return min(max(d.x, d.y), 0.0) + length(max(d, 0.0)) - rad;
            }

            // Scissoring
            float scissorMask(float2 p) {
                int fid = _UniformOffset;
                float2 sc = (abs((mul(data[fid].scissorMat, float4(p, 1.0, 1.0))).xy) - data[fid].scissorExt);
                sc = float2(0.5, 0.5) - sc * data[fid].scissorScale;
                return clamp(sc.x, 0.0, 1.0) * clamp(sc.y, 0.0, 1.0);
            }

            // Stroke - from [0..1] to clipped pyramid, where the slope is 1px.
            float strokeMask(float2 ftcoord) {
                int fid = _UniformOffset;
                return min(1.0, (1.0-abs(ftcoord.x*2.0-1.0))*data[fid].strokeMult) * min(1.0, ftcoord.y);
            }
                        
            fixed4 frag (v2f i) : SV_Target
            {
                // return fixed4(1,1,1,1);
                float4 result;
                float2 fpos = i.fpos;
                float2 ftcoord = i.uv;
                int fid = _UniformOffset;
                
                float scissor = scissorMask(fpos);
                float strokeAlpha = 1.0;
                #if (EDGE_AA)
                {
                    strokeAlpha = strokeMask(ftcoord);
                    if (strokeAlpha < data[fid].strokeThr) 
                        discard;
                }
                #endif
                if (data[fid].type == 0) // Gradient
                {
                    // Calculate gradient color using box gradient
                    float2 pt = (mul(data[fid].paintMat, float4(fpos, 1.0, 1.0))).xy;
                    float d = clamp((sdroundrect(pt, data[fid].extent, data[fid].radius) + data[fid].feather*0.5) / data[fid].feather, 0.0, 1.0);
                    float4 color = lerp(data[fid].innerCol, data[fid].outerCol, d);
                    // Combine alpha
                    color *= strokeAlpha * scissor;
                    result = color;
                }
                else if (data[fid].type == 1) // Image
                {
                    // Calculate color fron texture
                    float2 pt = (mul(data[fid].paintMat, float4(fpos, 1.0, 1.0))).xy / data[fid].extent;
                    float4 color = tex2D(_MainTex, pt);
                    if (data[fid].texType == 1) color = float4(color.xyz*color.w, color.w);
                    if (data[fid].texType == 2) color = float4(color.x, color.x, color.x, color.x);
                    // Apply color tint and alpha.
                    color *= data[fid].innerCol;
                    // Combine alpha
                    color *= strokeAlpha * scissor;
                    result = color;
                }
                else if (data[fid].type == 2) // Stencil fill
                {
                    result = float4(1, 1, 1, 1);
                } 
                else if (data[fid].type == 3) // Textured tris
                {
                    float4 color = tex2D(_MainTex, ftcoord);
                    if (data[fid].texType == 1) color = float4(color.xyz*color.w, color.w);
                    if (data[fid].texType == 2) color = float4(color.x, color.x, color.x, color.x);
                    color *= scissor;
                    result = color * data[fid].innerCol;
                }
                return result;
            }
            ENDCG
        }
    }
}
