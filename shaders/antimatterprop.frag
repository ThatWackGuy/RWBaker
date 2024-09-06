#version 450
#define ANTIPROP
#include "common.glsl"

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture

layout(location = 0) in flat ivec2 f_texCoord;
layout(location = 1) in flat int f_layer;
layout(location = 2) in flat int f_localZ;

layout(location = 0) out vec4 out_color;

void main()
{
    // Get pixel to be evaluated
    vec4 cPix = texelFetch(tex, f_texCoord, 0);

    // white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1))
    {
        discard;
    }

    float dpthRemove = pow(1 - cPix.g, d.contourExponent) * d.layerCount;

    float renderTo = round(clamp(dpthRemove, 0, 31));

    if (f_layer < renderTo)
    {
        discard;
    }

    out_color = vec4(0, 1 - gl_FragCoord.z, 0, 1);
    gl_FragDepth = 0;
}