#version 450
#define TILE
#include "common.glsl"

layout(set = 1, binding = 0) uniform sampler2D tex;  // tile texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map
layout(set = 1, binding = 4) uniform sampler2D rTex; // removal depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) in vec4 f_shCoord;
layout(location = 2) in flat int f_layer;
layout(location = 3) in flat int f_localZ;

layout(location = 0) out vec4 out_color;

void main()
{
    ivec2 texCoord = ivec2(f_texCoord);

    // Get pixel to be evaluated
    vec4 cPix;
    if (tile.isBox == 1)
    {
        vec2 tileSize = tile.tileSize * 20;
        float bounds = tile.bfTiles * 20;
        ivec2 bPos = ivec2(texCoord.x - bounds, texCoord.y - bounds); // Bounded Position

        // pixels outside bounds don't matter
        if (bPos.x < 0 || bPos.y < 0 || bPos.x > tileSize.x || bPos.y > tileSize.y) discard;

        ivec2 pxCoord;

        // FACE
        if (f_localZ == 0)
        {
            pxCoord = ivec2(texCoord.x, tile.tileSize.x * tileSize.y + texCoord.y);
        }
        // VERTICAL
        else if (bPos.y > 5 && bPos.y < tileSize.y - 5)
        {
            if (bPos.x > 5 && bPos.x < tileSize.x - 5) discard;

            // Left
            if (bPos.x < 5)
            {
                pxCoord = ivec2(20 + f_localZ, texCoord.y - bounds);
            }
            // Right piece
            else if (bPos.x > tileSize.x - 5 && bPos.x < tileSize.x)
            {
                pxCoord = ivec2(30 + f_localZ, (tile.tileSize.x - 1) * tileSize.y + texCoord.y - bounds);
            }
        }
        // HORIZONTAL
        else
        {
            float pieceIdx = floor(bPos.x / 20);

            // Top
            if (bPos.y < 5)
            {
                pxCoord = ivec2(mod(bPos.x, 20), pieceIdx * tileSize.y + f_localZ);
            }
            // Bottom
            else if (bPos.y > tileSize.y - 5)
            {
                pxCoord = ivec2(mod(bPos.x, 20), pieceIdx * tileSize.y + f_localZ);
            }
        }

        texCoord = pxCoord;
    }

    cPix = texelFetch(tex, texCoord, 0);

    if (cPix.a == 0)
    {
        discard;
    }

    // fetch remove stencil and remove if needed
    if (gl_FragCoord.z < texture(rTex, gl_FragCoord.xy).g)
    {
        discard;
    }

    // Shadows are rendered in red
    if (pass.idx == 0)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    float intensity = texelFetch(tex, ivec2(tile.vars * 20 * ((2 * tile.bfTiles) + tile.tileSize.x) + texCoord.x, texCoord.y), 0).r;
    out_color = shadePixel(
        cPix,
        pTex, lighting.pRain,
        eTex, intensity,
        f_layer, inShadow(f_shCoord, sTex, lighting.shadowBias)
    );
}