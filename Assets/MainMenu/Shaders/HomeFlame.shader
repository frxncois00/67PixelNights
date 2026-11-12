Shader "PixelNights/MenuFlame"
{
    Properties { [PerRendererData] _MainTex ("Flame", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            sampler2D _MainTex;
            v2f vert(appdata v) { v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float4 c=tex2D(_MainTex,i.uv);
                float heat=saturate(c.g*1.65);
                c.rgb=lerp(float3(.95,.19,.01),float3(1,1,.67),sqrt(heat));
                c.a=saturate(c.a*2.4);
                return c*i.color;
            }
            ENDCG
        }
    }
}
