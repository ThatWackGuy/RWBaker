#version 450
#define STANDARDPROP
#include "common.glsl"

layout(set = 1, binding = 0) uniform sampler2D tex;  // prop texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map
layout(set = 1, binding = 4) uniform sampler2D rTex; // removal depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) in vec4 f_shCoord;
layout(location = 2) in float f_layer;

layout(location = 0) out vec4 out_color;

void main()
{
    ivec2 texCoord = ivec2(f_texCoord);

    // Get pixel to be evaluated
    vec4 cPix = texelFetch(tex, texCoord, 0);

    // transparent is skipped
    if (cPix.a == 0) discard;

    // bevel props
    if (sProp.bevel > 0)
    {
        vec4 black = vec4(0, 0, 0, 1);
        vec4 h = texelFetch(tex, ivec2(texCoord.x - sProp.bevel, texCoord.y - sProp.bevel), 0); // highlights
        vec4 s = texelFetch(tex, ivec2(texCoord.x + sProp.bevel, texCoord.y + sProp.bevel), 0); // shadows

        // highlighted
        if (h != black)
        {
            cPix = vec4(0, 0, 1, 1);
        }
        // shadowed
        else if (s != black)
        {
            cPix = vec4(1, 0, 0, 1);
        }
        // base
        else
        {
            cPix = vec4(0, 1, 0, 1);
        }
    }

    // Shadows are rendered in red
    if (pass.idx == 0)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    float intensity = texelFetch(tex, ivec2(sProp.vars * 20 * (sProp.pixelSize.x) + texCoord.x, texCoord.y), 0).r;
    out_color = shadePixel(
        cPix,
        pTex, lighting.pRain,
        eTex, intensity,
        f_layer, inShadow(f_shCoord, sTex, lighting.shadowBias)
    );

    // get colored side of the prop if it has the tag
    if (sProp.color != 1) return;

    vec4 colored = texelFetch(tex, ivec2(texCoord.x + sProp.pixelSize.x, texCoord.y), 0);
    out_color = mix(out_color, colored, .5); // just eyeballing it, don't expect much precision
}