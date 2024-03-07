#version 450

layout(set = 0, binding = 0, std140) uniform SceneInfo
{
    mat4 transform;  // x+ right, y+ down matrix
    vec2 objectOffset;

    bool isShadow;
    uint shadowRepeatCurrent;
    uint shadowRepeatMax;
    vec2 lightOffset;
    vec2 shSize; // shadow texture size

    vec2 effectColorsSize;
    uint effectA;
    uint effectB;
} s;

layout(set = 0, binding = 1, std140) uniform RenderData
{
    float startingZ;
    float layerCount;
    vec2 texSize;

    vec2 tileSize;
    uint bfTiles;
    uint vars;
    uint pRain;
    uint isBox;
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // tile texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) flat in int f_layer;
layout(location = 2) in float f_shLayer;
layout(location = 3) in flat int f_localZ;

layout(location = 0) out vec4 out_color;

void main()
{
    // Get pixel to be evaluated
    vec4 cPix;
    if (d.isBox == 1)
    {
        vec2 tileSize = d.tileSize * 20;
        float bounds = d.bfTiles * 20;
        vec2 bPos = f_texCoord - bounds; // Bounded Position

        // pixels outside bounds don't matter
        if (bPos.x < 0 || bPos.y < 0 || bPos.x > tileSize.x || bPos.y > tileSize.y) discard;

        vec2 pxCoord;

        // FACE
        if (f_localZ == 0)
        {
            pxCoord = vec2(f_texCoord.x, d.tileSize.x * tileSize.y + f_texCoord.y);
        }
        // VERTICAL
        else if (bPos.y > 5 && bPos.y < tileSize.y - 5)
        {
            if (bPos.x > 5 && bPos.x < tileSize.x - 5) discard;

            // Left
            if (bPos.x < 5)
            {
                pxCoord = vec2(20 + f_localZ, f_texCoord.y - bounds);
            }
            // Right piece
            else if (bPos.x > tileSize.x - 5 && bPos.x < tileSize.x)
            {
                pxCoord = vec2(30 + f_localZ, (d.tileSize.x - 1) * tileSize.y + f_texCoord.y - bounds);
            }
        }
        // HORIZONTAL
        else
        {
            float pieceIdx = floor((f_texCoord.x - bounds) / 20);

            // Top
            if (bPos.y < 5)
            {
                pxCoord = vec2(mod(f_texCoord.x - bounds, 20), pieceIdx * tileSize.y + f_localZ);
            }
            // Bottom
            else if (bPos.y > tileSize.y - 5)
            {
                pxCoord = vec2(mod(f_texCoord.x - bounds, 20), pieceIdx * tileSize.y + f_localZ);
            }
        }

        cPix = texture(tex, pxCoord / d.texSize);
    }
    else
    {
        cPix = texture(tex, f_texCoord / d.texSize);
    }

    // Black, white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1) || cPix == vec4(0, 0, 0, 1))
    {
        discard;
    }

    // Shadows are rendered in red
    if (s.isShadow)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    // Check if pixel is unlit
    float paletteOffset = d.pRain == 1 ? 10 : 2;
    float effectOffset = d.pRain == 1 ? 2 : 0;
    if (f_shLayer > texture(sTex, gl_FragCoord.xy / s.shSize).r)
    {
        paletteOffset += 3;
        effectOffset += 1;
    }

    // Palette colors
    vec2 pSize = vec2(32.0, 16.0);
    float palX = f_layer + .2;
    vec4 pH = texture(pTex, vec2(palX, paletteOffset) / pSize); // Highlights
    vec4 pB = texture(pTex, vec2(palX, paletteOffset + 1) / pSize); // Base
    vec4 pS = texture(pTex, vec2(palX, paletteOffset + 2) / pSize); // Shadows
    vec4 pF = texture(pTex, vec2(1.2, paletteOffset - 2) / pSize); // Fog
    float pFI = texture(pTex, vec2(9.2, paletteOffset - 2) / pSize).r; // Fog Intensity

    // Red is Shadows
    if (cPix.r == 1)
    {
        // Purple is Effect A
        if (cPix.b == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * ((2 * d.bfTiles) + d.tileSize.x) + f_texCoord.x, f_texCoord.y) / d.texSize).r;

            vec4 fA = texture(eTex, vec2(s.effectA * 2 + (f_layer == 0 ? 0 : 1) + 0.5, effectOffset) / s.effectColorsSize);
            out_color = mix(pB, fA, 1 - intensity);

            return;
        }

        out_color = mix(pS, pF, f_layer < 10 ? 0 : pFI);
        return;
    }
    // Blue is Highlights
    else if (cPix.b == 1)
    {
        // Cyan is Effect B
        if (cPix.g == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * ((2 * d.bfTiles) + d.tileSize.x) + f_texCoord.x, f_texCoord.y) / d.texSize).r;

            vec4 fB = texture(eTex, vec2(s.effectB * 2 + (f_layer == 0 ? 0 : 1) + 0.5, effectOffset) / s.effectColorsSize);
            out_color = mix(pB, fB, 1 - intensity);

            return;
        }

        out_color = mix(pH, pF, f_layer < 10 ? 0 : pFI);
        return;
    }

    // Everything else is Base
    out_color = mix(pB, pF, f_layer < 10 ? 0 : pFI);
}