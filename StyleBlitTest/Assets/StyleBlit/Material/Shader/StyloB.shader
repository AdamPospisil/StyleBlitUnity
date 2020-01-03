Shader "StyloB"
{
	Properties
	{
		// Voting span
		_votespan("Smoothness", Range(0,5)) = 2
		// Splatting level depth
		[HideInInspector] _splatsize("Max chunk size", Range(15,1)) = 9
		// Vote distribution
		[HideInInspector] _splitCo("_splitCo", Float) = 10

		// Noise texture for preview
		sourceStyle("Style texture", 2D) = ""
	}

		
	SubShader
	{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1" "DisableBatching" = "True"}
		Lighting Off
				
		GrabPass
		{
			"_CoordTexture"
		}

		// pass for blending patches of coordinates of given size to eliminate seams and noise
	    // smoothing the result
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 grabPos : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD2;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				
				o.grabPos = ComputeGrabScreenPos(o.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			sampler2D_float _CoordTexture;
			float4 _CoordTexture_TexelSize;
			sampler2D_float sourceStyle;
			float4 sourceStyle_TexelSize;

			float _votespan;
			float _splitCo;

			// recontructs the coordinates from the previous pass
			float2 recon(float4 pattern)
			{
				float konf = _splitCo;

				pattern.zw *= konf;
				pattern.x += pattern.z;
				pattern.y += pattern.w;
				pattern.xy /= konf;

				return pattern.xy;
			}

			// performs averaging of the final transfer pixel contribution in votespan vicinity
			// translates the coordinates to result pixel from source style
			fixed4 frag(v2f i) : SV_Target
			{

				float2 pixelSize = float2(_CoordTexture_TexelSize.x, _CoordTexture_TexelSize.y);
				float4 color = float4(0.f, 0.f, 0.f, 1.f);
			
				float2 pos = i.screenPos.xy / i.screenPos.w;
				pos *= _ScreenParams.xy;

				int w = 0;
				int vic = _votespan;

					
				for (int x = -vic; x <= vic; x++)
				{
					for (int y = -vic; y <= vic; y++)
					{
						float2 coords = recon(tex2D(_CoordTexture, float2(pos.x + x, pos.y + y) / _CoordTexture_TexelSize.zw));
						coords *= sourceStyle_TexelSize.zw;
						coords -= float2(x, y);
						color += tex2D(sourceStyle, coords.xy / sourceStyle_TexelSize.zw);
						w++;
					}
				}
				float4 smooth = color / w;				
				return smooth;
			}
			ENDCG
		}
	}
}
