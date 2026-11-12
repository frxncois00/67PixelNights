Shader "PixelNights/HomeAtmosphere"
{
    Properties {
        [PerRendererData] _MainTex ("Artwork", 2D) = "white" {}
        _ClickRect ("Clicked button", Vector) = (0,0,0,0)
        _ClickStrength ("Click feedback", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            float4 _ClickRect;
            float _ClickStrength;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float glow(float2 p, float2 center, float2 radius) { float2 d=(p-center)/radius; return exp(-dot(d,d)*2); }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float t=_Time.y;
                float2 pixel=float2(uv.x*1672,(1-uv.y)*941);
                // Keep title motion inside the sign instead of its surrounding tree canopy.
                float sign=smoothstep(500,508,pixel.x)*(1-smoothstep(1204,1212,pixel.x));
                sign*=smoothstep(110,118,pixel.y)*(1-smoothstep(307,315,pixel.y));
                float number=smoothstep(752,760,pixel.x)*(1-smoothstep(923,931,pixel.x));
                number*=smoothstep(44,52,pixel.y)*(1-smoothstep(163,171,pixel.y));
                sign=saturate(sign+number);
                // Follow the cloth's pointed hem. The old rectangular mask extended
                // below that hem onto the tree. An inset keeps UV samples in the fabric.
                float taper=saturate((pixel.y-338)/33);
                float leftEdge=lerp(74,116,taper),rightEdge=lerp(156,116,taper);
                float flag=smoothstep(leftEdge,leftEdge+4,pixel.x)*(1-smoothstep(rightEdge-4,rightEdge,pixel.x));
                flag*=smoothstep(238,250,pixel.y)*(1-smoothstep(365,371,pixel.y));
                float hanging=smoothstep(238,371,pixel.y);
                float2 sampleUV=uv;
                sampleUV+=sign*float2(sin(t*.75)*.0011,sin(t*.62)*.0013);
                sampleUV.x+=flag*hanging*sin(uv.y*34-t*2.4)*.0022;
                sampleUV.y+=flag*hanging*sin(uv.x*60-t*2.4)*.0013;
                float4 c=tex2D(_MainTex,sampleUV)*i.color;
                // Distort only blue river pixels; banks, plants, fence and menu remain fixed.
                float river=step(.75,uv.x)*step(uv.y,.19);
                river*=saturate((c.b-c.r)*5)*saturate((c.g-c.r)*6);
                float2 flow=float2(sin(uv.y*280+t*2.4)*.0030+sin(uv.y*117-t*1.7)*.0015,
                    sin(uv.x*110+t*2.1)*.0010);
                c=lerp(c,tex2D(_MainTex,uv+flow)*i.color,river);
                float fire=glow(uv,float2(.261,.190),float2(.06,.12));
                float lantern=glow(uv,float2(.281,.750),float2(.025,.060));
                lantern+=glow(uv,float2(.263,.330),float2(.022,.048));
                lantern+=glow(uv,float2(.160,.377),float2(.032,.050));
                lantern+=glow(uv,float2(.179,.713),float2(.027,.037));
                float flicker=.6+.22*sin(t*12.7)+.12*sin(t*23.3)+.06*sin(t*37.1);
                float warm=saturate((c.r-c.b)*3);
                c.rgb+=float3(1,.46,.08)*(fire*.15+lantern*.11)*flicker*(.25+warm);
                // Highlight existing stars instead of moving the painted landscape.
                float star=step(.7,uv.y)*smoothstep(.58,.95,min(c.r,min(c.g,c.b)));
                float titleArea=step(.245,uv.x)*step(uv.x,.76)*step(.60,uv.y);
                float moonArea=step(.78,uv.x)*step(uv.x,.91)*step(.78,uv.y);
                star*=1-saturate(titleArea+moonArea);
                float seed=frac(sin(dot(floor(uv*float2(1672,941)),float2(12.9898,78.233)))*43758.54);
                c.rgb*=1+star*.22*sin(t*(.9+seed)+seed*20);
                float water=step(.76,uv.x)*step(uv.y,.19);
                water*=saturate((c.b-c.r)*4)*saturate((c.g-c.r)*5);
                float ripple=pow(saturate(sin(uv.y*1650+t*1.4+sin(uv.x*55+t*.25)*2)),14);
                c.rgb+=water*ripple*.085*float3(.55,.72,1);
                float button=step(_ClickRect.x,uv.x)*step(uv.x,_ClickRect.z)*step(_ClickRect.y,uv.y)*step(uv.y,_ClickRect.w);
                c.rgb+=button*_ClickStrength*float3(.20,.14,.035);
                return c;
            }
            ENDCG
        }
    }
}
