// Author: Tony (19/11/2026)
// Used by: InteractOutline.cs (never put on a material by hand).
// Turns InteractOutline's mask (the shapes of the highlighted things, in their colors) into outlines on screen.
// A pixel just outside a shape takes the shape's color: it looks around itself in 20 directions, up to the outline
// width away (the outer half a little softer). A pixel on the shape itself only gets the light tint (Fill), so the
// thing keeps its own look. Drawn see-through on top of the finished picture.
Shader "Hidden/Tony/InteractOutlineComposite"
{
    Properties
    {
        _MainTex ("Mask", 2D) = "black" {}
    }

    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;   // 1 / the mask's size in pixels
            float _OutlineWidth;         // pixels
            float _OutlinePulse;         // 0..1: multiplies how strongly everything shows (the gentle pulsing)
            float _FillAlpha;            // 0..1: the light tint over the thing itself

            // Keeps whichever is stronger: what was found so far, or the mask a little way off (weight = how much
            // that distance counts: the far ring is softer).
            fixed4 Stronger (fixed4 best, float2 uv, float weight)
            {
                fixed4 found = tex2D(_MainTex, uv);
                found.a *= weight;
                return found.a > best.a ? found : best;
            }

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 here = tex2D(_MainTex, i.uv);
                if (here.a > 0.004)   // on the thing itself: only the light tint
                {
                    return fixed4(here.rgb, here.a * _FillAlpha * _OutlinePulse);
                }

                float2 spread = _MainTex_TexelSize.xy * _OutlineWidth;
                fixed4 best = fixed4(0, 0, 0, 0);

                // The near ring (half the width), 8 directions, full strength.
                best = Stronger(best, i.uv + spread * float2( 0.500,  0.000), 1.0);
                best = Stronger(best, i.uv + spread * float2( 0.354,  0.354), 1.0);
                best = Stronger(best, i.uv + spread * float2( 0.000,  0.500), 1.0);
                best = Stronger(best, i.uv + spread * float2(-0.354,  0.354), 1.0);
                best = Stronger(best, i.uv + spread * float2(-0.500,  0.000), 1.0);
                best = Stronger(best, i.uv + spread * float2(-0.354, -0.354), 1.0);
                best = Stronger(best, i.uv + spread * float2( 0.000, -0.500), 1.0);
                best = Stronger(best, i.uv + spread * float2( 0.354, -0.354), 1.0);

                // The far ring (the full width), 12 directions, a little softer at the edge.
                best = Stronger(best, i.uv + spread * float2( 1.000,  0.000), 0.65);
                best = Stronger(best, i.uv + spread * float2( 0.866,  0.500), 0.65);
                best = Stronger(best, i.uv + spread * float2( 0.500,  0.866), 0.65);
                best = Stronger(best, i.uv + spread * float2( 0.000,  1.000), 0.65);
                best = Stronger(best, i.uv + spread * float2(-0.500,  0.866), 0.65);
                best = Stronger(best, i.uv + spread * float2(-0.866,  0.500), 0.65);
                best = Stronger(best, i.uv + spread * float2(-1.000,  0.000), 0.65);
                best = Stronger(best, i.uv + spread * float2(-0.866, -0.500), 0.65);
                best = Stronger(best, i.uv + spread * float2(-0.500, -0.866), 0.65);
                best = Stronger(best, i.uv + spread * float2( 0.000, -1.000), 0.65);
                best = Stronger(best, i.uv + spread * float2( 0.500, -0.866), 0.65);
                best = Stronger(best, i.uv + spread * float2( 0.866, -0.500), 0.65);

                return fixed4(best.rgb, best.a * _OutlinePulse);
            }
            ENDCG
        }
    }
}
