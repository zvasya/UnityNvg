Shader "Exp/Shader"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        
        Blend One Zero, One Zero

        ZWrite Off ZTest LEqual Cull Off
//		Stencil 
//		{
//			Ref [_ClipRef]
//            ReadMask [_ClipReadMask]
//            WriteMask [_ClipWriteMask]
//            CompBack [_ClipCompBack]
//            PassBack [_ClipPassBack]
//            FailBack [_ClipFailBack]
//            ZFailBack [_ClipZFailBack]
//            CompFront [_ClipCompFront]
//            PassFront [_ClipPassFront]
//            FailFront [_ClipFailFront]
//            ZFailFront [_ClipZFailFront]
//		}

        Pass
        {
//            ColorMask [_ColorWriteMask]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #include "UnityCG.cginc"
            

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };
            
			StructuredBuffer<float3> _VertexBuffer;
            uint _Offset;

            v2f vert (uint id : SV_VertexID)
            {
                uint index = id + _Offset;
                float3 vertex = _VertexBuffer[index]; 
                float4 position = float4(vertex.x, vertex.y, vertex.z, 1);
                
                // if (index == 0)
                //     position = float4(1,1,0,1);
                // else if (index == 1)
                //     position = float4(0,0,0,1);
                // else if (index == 2)
                //     position = float4(1,0,0,1);
                
                v2f o;
                o.vertex = UnityObjectToClipPos(position);
                return o;
            }
            
                    
            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1,1,1,1);
            }
            ENDCG
        }
    }
}
