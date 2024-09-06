#ifndef COMMON
#define COMMON

    #ifdef DEBUG
    #extension GL_EXT_debug_printf : require
    #endif

    // [0..4] COMMON UNIFORMS
    layout(set = 0, binding = 0, std140) uniform CameraData {
        mat4 transform;
    } camera;

    layout(set = 0, binding = 1, std140) uniform StencilData {
        uint idx;
        vec2 size;
    } pass;

    layout(set = 0, binding = 2, std140) uniform LightData {
        mat4 lightTransform;
        mat4 biasTransform;
        float shadowBias;
        float pRain;
    } lighting;

    layout(set = 0, binding = 3, std140) uniform PaletteData {
        uint effectA;
        uint effectB;
    } palette;

    layout(set = 0, binding = 4, std140) uniform MeshData {
        mat4 transform;
    } mesh;

    // FUNCTIONS
    int inShadow(vec4 f_shCoord, sampler2D sTex, float bias)
    {
        vec4 shadowCoords = f_shCoord / f_shCoord.w;
        shadowCoords = shadowCoords * 0.5 + 0.5;
        shadowCoords.y = 1 - shadowCoords.y;

        return int(f_shCoord.z > texture(sTex, shadowCoords.xy).r + bias);
    }

    vec4 shadePixel(
        vec4 cPix,
        sampler2D pTex, float rain,
        sampler2D eTex, float intensity,
        float depth, int shadow
    )
    {
        // Check if pixel is unlit
        float paletteOffset = 2 + shadow * 3;
        float effectOffset = shadow;

        // Palette colors
        const vec2 pSize = vec2(32.0, 16.0);
        float palX = clamp(depth, 0, 31);
        vec4 pH    =     mix( texelFetch(pTex, ivec2(palX, paletteOffset   ),  0 ), texelFetch(pTex, ivec2(palX, paletteOffset + 8 ), 0 ), rain );   // Highlights
        vec4 pB    =     mix( texelFetch(pTex, ivec2(palX, paletteOffset + 1), 0 ), texelFetch(pTex, ivec2(palX, paletteOffset + 9 ), 0 ), rain );   // Base
        vec4 pS    =     mix( texelFetch(pTex, ivec2(palX, paletteOffset + 2), 0 ), texelFetch(pTex, ivec2(palX, paletteOffset + 10), 0 ), rain );   // Shadows
        vec4 pF    =     mix( texelFetch(pTex, ivec2(1,    paletteOffset - 2), 0 ), texelFetch(pTex, ivec2(1,    paletteOffset     ), 0 ), rain );   // Fog
        float pFI  =     mix( texelFetch(pTex, ivec2(9,    paletteOffset - 2), 0 ), texelFetch(pTex, ivec2(9,    paletteOffset     ), 0 ), rain ).r; // Fog Intensity

        // final shaded color
        vec4 shadedPx;

        // Red is Shadows
        if (cPix.r == 1)
        {
            // Purple is Effect A
            if (cPix.b == 1)
            {
                vec4 fA = texelFetch(eTex, ivec2(palette.effectA * 2 + (depth == 0 ? 0 : 1), effectOffset), 0);
                shadedPx = mix(pB, fA, 1 - intensity);
            }

            shadedPx = mix(pS, pF, depth < 10 ? 0 : pFI);
        }
        // Blue is Highlights
        else if (cPix.b == 1)
        {
            shadedPx = mix(pH, pF, depth < 10 ? 0 : pFI);
            // Cyan is Effect B
            if (cPix.g == 1)
            {
                vec4 fB = texelFetch(eTex, ivec2(palette.effectB * 2 + (depth == 0 ? 0 : 1), effectOffset), 0);
                shadedPx = mix(pB, fB, 1 - intensity);
            }
        }
        // Everything else is Base
        else
        {
            shadedPx = mix(pB, pF, depth < 10 ? 0 : pFI);
        }

        return shadedPx;
    }

    // CUSTOM UNIFORMS
    #ifdef TILE
        layout(set = 0, binding = 5, std140) uniform RenderData
        {
            float startingZ;
            float layerCount;

            vec2 tileSize;
            uint bfTiles;
            uint vars;
            uint isBox;
        } tile;
    #endif

    #ifdef STANDARDPROP
        layout(set = 0, binding = 5, std140) uniform RenderData
        {
            float startingZ;
            float layerCount;

            vec2 pixelSize;
            uint vars;
            uint bevel;
            uint color;
        } sProp;
    #endif

    #ifdef SOFTPROP
        layout(set = 0, binding = 5, std140) uniform RenderData
        {
            float startingZ;
            float layerCount;

            vec2 pixelSize;
            uint vars;
            uint color;
            uint round;
            uint shadeRepeat;
            float contourExponent;
            float highlightMin;
            float shadowMin;
            float highlightExponent;
        } fProp;
    #endif

    #ifdef DECALPROP
        layout(set = 0, binding = 5, std140) uniform RenderData
        {
            float startingZ;
            float layerCount;
            vec2 texSize;

            vec2 pixelSize;
        } dProp;
    #endif

    #ifdef ANTIPROP
        layout(set = 0, binding = 5, std140) uniform RenderData
        {
            float startingZ;
            float layerCount;

            vec2 pixelSize;
            uint vars;
            float contourExponent;
        } d;
    #endif
#endif