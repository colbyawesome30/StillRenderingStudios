Shader "Custom/GrassBillboard"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _MaxDistance ("Max Draw Distance", Float) = 60
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Cutoff;
            float _MaxDistance;

            // Per-blade tint, sent from the script
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;

                // Blade position and scale come from the instance matrix
                float3 origin = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float sx = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float sy = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));

                // Face the camera, rotating around Y only
                float3 toCam = _WorldSpaceCameraPos - origin;
                float dist = length(toCam);
                toCam.y = 0;
                float3 fwd = normalize(toCam + float3(0, 0, 0.0001));
                float3 right = normalize(cross(fwd, float3(0, 1, 0)));

                float3 world = origin + right * v.vertex.x * sx + float3(0, 1, 0) * v.vertex.y * sy;

                // Too far away: collapse the blade so it costs nothing to shade
                if (dist > _MaxDistance) world = origin;

                o.pos = UnityWorldToClipPos(world);
                o.uv = v.uv;
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip(col.a - _Cutoff);
                return col;
            }
            ENDCG
        }
    }
}