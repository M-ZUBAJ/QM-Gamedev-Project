// Author: Tony (19/11/2026)
// Used by: InteractOutline.cs (never put on a material by hand).
// Draws an object's shape in one flat color into InteractOutline's hidden mask picture. It ignores depth and
// draws both sides, so the whole silhouette is there; the outline pass then draws around it on screen.
Shader "Hidden/Tony/InteractOutlineMask"
{
    SubShader
    {
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Set by InteractOutline before each object: rgb = its outline color, a = how strongly it shows (fades).
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
}
