Shader "StyleBlitMultiImageEffect"
{
	Properties
	{
		// Texture coming from the first pass
		[HideInInspector] _MainTex("", 2D) = "white" {}

		// Chunk threshold
		_threshold("Fragmentation", Range(0.01,0.2)) = 0.07
		// Voting span
		_votespan("Smoothness", Range(0,5)) = 2
		// Splatting level depth
		[HideInInspector] _splatsize("Max chunk size", Range(15,1)) = 9
		// Vote distribution
		[HideInInspector] _splitCo("_splitCo", Float) = 10
		// Background color
		_bgColor("Background color", Color) = (1,1,1,1)
		// Seed distribution coefficient
		[HideInInspector] _jitterCo("_jitterCo", Float) = 4

		// Lookup table for guidance
		normalToSourceLUT("Normal LUT texture", 2D) = ""
		sourceNormals("Source normals texture", 2D) = ""
		// Noise texture for preview
		noiseTexture("Noise texture", 2D) = "white"
		//sourceStyle("Style texture", 2D) = ""
		sourceStyles("Style textures", 2DArray) = ""
		styleMask("Style mask", 2D) = ""
		styleCount("Style count", Int) = 1
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }

		// Pass for primary source-to-target translation
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"			

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 screenPos: TEXCOORD1;
			};

			v2f vert(appdata_base v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _CameraDepthNormalsTexture;
			float4 _CameraDepthNormalsTexture_TexelSize;
			sampler2D styleMask;
			float4 styleMask_TexelSize;

			sampler2D_float normalToSourceLUT;
			float4 normalToSourceLUT_TexelSize;
			sampler2D_float sourceNormals;
			float4 sourceNormals_TexelSize;
			sampler2D_float noiseTexture;
			float4 noiseTexture_TexelSize;

			float _threshold;
			float _splitCo;
			int _splatsize;
			int _jitterCo;
			float4 _bgColor;

			// tells whether pixel is part of the background
			bool isBG(float2 pos)
			{
				float4 depthNormal = tex2D(_CameraDepthNormalsTexture, pos);
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(depthNormal, depthValue, normalValues);

				if (depthValue > 1.f)
					return true;
				else
					return false;
			}

			// shorthand for decoding normal values from depth-normal vector
			float3 decodeNormalValues(float4 depthNormal)
			{
				float depthValue;
				float3 normalValues;
				DecodeDepthNormal(depthNormal, depthValue, normalValues);
				normalValues = 0.f - normalValues;
				normalValues = normalValues * 0.5f + 0.5f;
				return normalValues;
			}

			// screen-space project helper
			inline float2 proj(float3 n)
			{
				return float2(n.x / n.z, n.y / n.z);
			}

			// screen-space project helper
			inline float2 proj(float2 n, float2 w)
			{
				return float2(n.x / w.x, n.y / w.y);
			}

			// pxc: pixel on normal texture
			// returns respective uv cooordinate on style texture
			inline float2 lookupCoord(float2 pxc)
			{
				float4 pattern = tex2Dlod(
					_CameraDepthNormalsTexture,
					float4(proj(pxc, _CameraDepthNormalsTexture_TexelSize.zw), 0.f, 0.f));

				float3 normalValues = decodeNormalValues(pattern);
				pattern = float4(normalValues, 1);

				return tex2Dlod(normalToSourceLUT, float4(pattern.rg, 0.f, 0.f)).rg;
			}

			// sourceCoords: uv coordinate of source normal texture
			// targetCoords: uv coordinate of target normal texture
			// returns error of guide normal to target
			inline float calcGuideError(float2 sourceCoords, float2 targetCoords)
			{
				float4 depthNormal = tex2Dlod(_CameraDepthNormalsTexture, float4(targetCoords, 0.f, 0.f));
				float3 normalValues = decodeNormalValues(depthNormal);

				float4 diff = abs(float4(normalValues, 1.f) -
					tex2Dlod(sourceNormals, float4(sourceCoords, 0.f, 0.f)));

				return diff.x + diff.y + diff.z;
			}

			// deconstructs the coords to multiple channels
			float4 decon(float2 coords)
			{
				float konf = _splitCo;
				float2 sect = float2(0,0);
				coords.x = modf(coords.x*konf, sect.x);
				coords.y = modf(coords.y*konf, sect.y);
				sect /= konf;

				return float4(coords, sect);

			}

			// returns implicit seed in a pixel vicinity
			// jitter effectivelly shaping the seed neigborhood
			float2 findSeed(float2 pxc, float spacing, int x, int y)
			{
				float2 coords = floor(pxc / spacing);
				float2 jitter = tex2Dlod(noiseTexture,
					float4(proj(coords, noiseTexture_TexelSize.zw), 0.f, 0.f)).rg;
				jitter *= _jitterCo;
				return (coords + jitter + float2(x, y)) * spacing;
			}

			// returns nearest implicit seed in a pixel vicinity
			float2 findNearestSeed(float2 pxc, float spacing, int x, int y)
			{
				float bestD = 3.402823466e+38f;
				float2 bestPxc = float2(0.f, 0.f);

				for (int i = -1; i <= 1; i++)
				{
					for (int j = -1; j <= 1; j++)
					{
						float2 p = findSeed((pxc + float2(i, j) * spacing), spacing, x, y);
						float d = distance(p, pxc);
						if (bestD > d)
						{
							bestPxc = p;
							bestD = d;
						}
					}
				}

				return bestPxc;
			}


			// performs style texture lookup for the given pixel and seed
			// calculates transfer error and compaares with the best result so far
			void findBestCoords(float2 pxc, float2 origSeedPxc, int l, float thresh,
				inout float2 bestCoords, inout float bestError, inout int bestL,
				inout float2 bestSeed)
			{
				float2 seedPxc = lookupCoord(origSeedPxc) * sourceNormals_TexelSize.zw;

				float2 sourcePxc = seedPxc + (pxc - origSeedPxc);
				float2 sourceCoords = proj(sourcePxc, sourceNormals_TexelSize.zw);

				if ((distance(pxc, origSeedPxc) < l / 2.f) &&
					all(sourcePxc >= float2(0.f, 0.f)) &&
					all(sourcePxc < sourceNormals_TexelSize.zw))
				{
					float error = calcGuideError(
						sourceCoords,
						proj(pxc.xy, _CameraDepthNormalsTexture_TexelSize.zw));

					if (error < thresh && error < bestError)
					{
						bestL = l;
						bestError = error;

						bestCoords = sourceCoords;
						bestSeed = proj(seedPxc, sourceNormals_TexelSize.zw);
					}
				}
			}

			half4 frag(v2f input) : COLOR
			{
				int levels = _splatsize;
				float thresh = _threshold;

				// if point in background, skip
				//if (isBG(input.screenPos)) {
				if (tex2D(styleMask, input.screenPos).r < 0.05f) 
					return tex2D(_MainTex, input.screenPos);

				// performs style texture initial lookup, before searching the best coordinate
				float bestError = 3.402823466e+38f;
				float bestL = -1;
				float2 pxc = proj(input.screenPos.xyw);
				pxc *= _ScreenParams.xy;

				float2 bestCoords = lookupCoord(pxc);

				float2 bestSeed = float2(0.f, 0.f);

				float2 nearSeed;


				// descending the levels for finding the largest chunk of pixels to transfer
				for (int i = levels; i > 0; i--)
				{
					int l = pow(2, i);

					if (l > bestL)
					{
						for (int x = -1; x <= 1; x++)
						{
							for (int y = -1; y <= 1; y++)
							{
								nearSeed = findNearestSeed(pxc, l, x, y);
								findBestCoords(pxc, nearSeed, l, thresh,
									bestCoords, bestError, bestL, bestSeed);
							}
						}
					}
				}

				// deconstructs the coordinate for passing
				return decon(bestCoords.xy);
			}
			ENDCG
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
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD2;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			
			sampler2D styleMask;
			float4 styleMask_TexelSize;

			sampler2D_float sourceStyle;
			float4 sourceStyle_TexelSize;
			UNITY_DECLARE_TEX2DARRAY(sourceStyles);
			float4 sourceStyles_TexelSize;
			int styleCount;

			sampler2D _CameraDepthNormalsTexture;
			float4 _CameraDepthNormalsTexture_TexelSize;

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
				float2 pixelSize = _MainTex_TexelSize.xy;
				float4 color = float4(0.f, 0.f, 0.f, 1.f);
				float4 c1 = float4(0.f, 0.f, 0.f, 1.f);
				float4 c2 = float4(0.f, 0.f, 0.f, 1.f);

				float2 pos = i.screenPos.xy / i.screenPos.w;
				pos *= _ScreenParams.xy;
				
				int w = 0;
				int vic = _votespan;

				float stylo = tex2D(styleMask, pos.xy / styleMask_TexelSize.zw).r;
			
				//true for non-styled area
				bool env = true;

				for (int x = -vic; x <= vic; x++)
				{
					for (int y = -vic; y <= vic; y++)
					{
						float2 coords = recon(tex2D(_MainTex, float2(pos.x + x, pos.y + y) / _MainTex_TexelSize.zw));
						coords *= sourceStyles_TexelSize.zw;
						coords -= float2(x, y);

						float2 maskpos = float2(pos.x + x, pos.y + y) / styleMask_TexelSize.zw;

						if (tex2Dlod(styleMask, float4(maskpos.x, maskpos.y, 0.f, 0.f)).r < 0.05f) {
							color += tex2D(_MainTex, float2(pos.x + x, pos.y + y) / _MainTex_TexelSize.zw);
						}
						else {
							int styleId = tex2Dlod(styleMask, float4(maskpos.x, maskpos.y, 0.f, 0.f)).g * styleCount - 1;
							color += UNITY_SAMPLE_TEX2DARRAY_LOD(sourceStyles, float3(coords.xy / sourceStyles_TexelSize.zw, styleId), float2(0.f, 0.f));
							env = false;
						}
						w++;
					}
				}

				if (env)
				{
					return tex2D(_MainTex, pos.xy / styleMask_TexelSize.zw);
				}

				float4 smooth = color / w;
				return smooth;
			}
			ENDCG
		}

	}
		FallBack "Diffuse"
}