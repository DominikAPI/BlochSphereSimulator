Shader "Custom/SphereView"
{
    Properties
    {
        _TintColor ("Tint", Color) = (0.35, 0.6, 1, 1)
        _GridColor ("Grid color", Color) = (0.7, 0.85, 1, 1)
        _FresnelPower ("Fresnel power", Range(0.5, 6)) = 2.5
        _BaseAlpha ("Base alpha", Range(0, 0.2)) = 0.04
        _RimAlpha ("Rim alpha", Range(0, 1)) = 0.5
        _LatLines ("Latitude lines per 180°", Float) = 12
        _LonLines ("Longitude lines (multiple of 4)", Float) = 24
        _LineWidth ("Line width (px)", Float) = 1.2
        _BackDim ("Back-side dimming", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha     // Standard alpha blending
            ZWrite Off                          // Transparent object: don't write depth
            Cull Off                            // Render both sides (back faces visible through the sphere)

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor; half4 _GridColor;
                float _FresnelPower, _BaseAlpha, _RimAlpha;
                float _LatLines, _LonLines, _LineWidth, _BackDim;
            CBUFFER_END

            struct Attributes 
            { 
                float4 positionOS : POSITION; 
                float3 normalOS : NORMAL; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 posOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.posOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = GetWorldSpaceViewDir(posWS);
                return o;
            }

            // Integer values of u = line. The min(fwidth) protects against thick lines at the seam where frac() jumps.
            float GridLine(float u, float widthPx)
            {
                float d = abs(frac(u + 0.5) - 0.5);
                float fw = min(fwidth(frac(u)), fwidth(frac(u + 0.5)));
                fw = max(fw, 1e-4);
                return 1.0 - smoothstep(0.0, fw * widthPx, d);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Normalize object-space position to get the sphere surface direction
                float3 p = normalize(i.posOS);
                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewWS);
                float ndv = dot(N, V);

                // Dim back-facing fragments so the front hemisphere reads more clearly
                float facing = ndv > 0 ? 1.0 : _BackDim;

                // Fresnel rim: bright at grazing angles, fades toward the center
                float rim = pow(1.0 - saturate(abs(ndv)), _FresnelPower);

                // Spherical coordinates -> normalized grid parameters
                float lat = asin(clamp(p.y, -1.0, 1.0)) / PI;   // -0.5 .. 0.5
                float lon = atan2(p.z, p.x) / (2.0 * PI);       // -0.5 .. 0.5
                float polar = sqrt(saturate(1.0 - p.y * p.y));
                float lonMask = smoothstep(0.05, 0.25, polar);  // longitude lines fade out at the poles

                // Combine latitude and longitude grid lines (longitude masked near poles)
                float grid = max(GridLine(lat * _LatLines, _LineWidth),
                                 GridLine(lon * _LonLines, _LineWidth) * lonMask);

                // Blend tint and grid color, accumulate alpha from base + rim + grid
                float3 col = lerp(_TintColor.rgb, _GridColor.rgb, grid);
                float a = _BaseAlpha + rim * _RimAlpha + grid * 0.35;
                return half4(col, saturate(a) * facing);
            }
            ENDHLSL
        }
    }
}
