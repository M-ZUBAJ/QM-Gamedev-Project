// Author: Tony (20/11/2026)
// Used by: WorldBarrier.cs (it makes the material itself; never put on a material by hand).
// The edge of the world: a see-through force field with slowly sliding diagonal stripes. Faint from afar, bright and
// rippling where the player is near (WorldBarrier sends the player's position every frame), fading out high above
// the player so it never stands like a cliff over the whole world. Both sides are drawn, and the scene's fog
// (ViewDistance's mist) hides it far away like everything else.
Shader "Hidden/Tony/WorldBarrier"
{
    Properties
    {
        _Color ("Color", Color) = (0.3, 0.8, 1, 1)
        _FarAlpha ("Seen From Afar", Range(0, 1)) = 0.12
        _NearAlpha ("Close Up", Range(0, 1)) = 0.8
        _GlowRadius ("Glow Radius (m)", Float) = 12
        _StripeSpacing ("Stripe Spacing (m)", Float) = 2.5
        _StripeSpeed ("Stripe Speed", Float) = 0.4
        _FadeAbove ("Fades Out Above The Player (m)", Float) = 40
        _PlayerPos ("Player Position", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _FarAlpha;
            float _NearAlpha;
            float _GlowRadius;
            float _StripeSpacing;
            float _StripeSpeed;
            float _FadeAbove;
            float4 _PlayerPos;   // where the player is (world space), set by WorldBarrier every frame

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.world = mul(unity_ObjectToWorld, vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Along the wall: x + z works on all four sides (on each side one of them stays the same).
                float along = i.world.x + i.world.z;

                // Diagonal stripes sliding slowly up the wall.
                float s = frac((along + i.world.y) / _StripeSpacing - _Time.y * _StripeSpeed);
                float stripe = saturate(1.0 - abs(s - 0.5) * 4.0);   // 1 in a stripe's middle, 0 in the gaps
                stripe *= stripe;

                // Brighter around the player, with rings running out from them like ripples on water.
                float toPlayer = distance(i.world, _PlayerPos.xyz);
                float near = 1.0 - smoothstep(0.0, _GlowRadius, toPlayer);
                float ripple = 0.5 + 0.5 * sin(toPlayer * 2.0 - _Time.y * 5.0);

                // Fades out high above the player.
                float above = 1.0 - smoothstep(_FadeAbove * 0.5, _FadeAbove, i.world.y - _PlayerPos.y);

                float alpha = lerp(_FarAlpha, _NearAlpha, near) * (0.3 + 0.7 * stripe);
                alpha += near * near * ripple * 0.25;
                alpha *= above;

                fixed4 col = fixed4(_Color.rgb * (1.0 + near), saturate(alpha));
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
