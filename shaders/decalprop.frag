#version 450
#define DECALPROP
#include "common.glsl"

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D rTex; // removal depth map

layout(location = 0) in flat ivec2 f_texCoord;
layout(location = 1) in float f_layer;

layout(location = 0) out vec4 out_color;

void main()
{
    // Get pixel to be evaluated
    vec4 cPix = texelFetch(tex, f_texCoord, 0);

    if (cPix.a == 0)
    {
        discard;
    }

    if (gl_FragCoord.z < texture(rTex, gl_FragCoord.xy / pass.size).g)
    {
        discard;
    }

    // decals are always colored
    out_color = texelFetch(tex, ivec2(f_texCoord.x, f_texCoord.y + dProp.pixelSize.y), 0);
    out_color.a = .8;
}