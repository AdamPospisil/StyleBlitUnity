Shader "Unlit/MaterialID"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_StyleId ("Style number", Int) = 0
		[HideInInspector] _StyleCount ("Style count", Int) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry"}
        LOD 100

        Pass
        {

			//Stencil {
			//	Ref [_StyleId]
			//	Comp always
			//	Pass replace
			//}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
			int _StyleId;
			int _StyleCount;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				return (float)_StyleId / (_StyleCount - 1);
            }
            ENDCG
        }
    }
}
