// Author: Tony (20/11/2026)
// Used by: ViewDistance.cs (it makes the material itself; never put on a material by hand).
// The mist low on the sky: a flat card just inside where the camera stops drawing, colored like the scene's fog.
// Fully mist below the horizon and a little above it, fading out higher up (_MistTop), so the far ground melts into
// the sky instead of ending in a hard edge. Anything closer than the card hides it (depth test), so only what's past
// the drawing distance (the sky, the empty space past the last drawn hill) gets the mist.
Shader "Hidden/Tony/ViewDistanceMist"
{
    Properties
    {
        _MistTop ("Mist Top", Float) = 0.35
    }
    SubShader
    {
        // After the sky and everything solid, before other see-through things.
        Tags { "Queue" = "Transparent-100" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _MistTop;   // how high up the sky the mist reaches (0 = the horizon, 1 = straight up)

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.world = mul(unity_ObjectToWorld, vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float up = normalize(i.world - _WorldSpaceCameraPos).y;   // -1 straight down, 0 the horizon, 1 straight up
                float mist = 1.0 - smoothstep(-0.02, max(_MistTop, 0.01), up);
                return fixed4(unity_FogColor.rgb, mist);
            }
            ENDCG
        }
    }
}
