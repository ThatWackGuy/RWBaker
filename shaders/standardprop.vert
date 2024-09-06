#version 450
#define STANDARDPROP
#include "common.glsl"

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out vec4 f_shCoord;
layout(location = 2) out flat int f_layer;

void main()
{
    vec4 pos4 = vec4(v_position.xyz, 1);
    vec4 v_meshPos = mesh.transform * pos4;

    if (pass.idx == 0)
    {
        gl_Position = lighting.lightTransform * mesh.transform* pos4;
    }
    else
    {
        gl_Position = camera.transform * mesh.transform * pos4;
    }

    f_texCoord = v_texCoord;
    f_layer = int(v_meshPos.z - sProp.startingZ);
    f_shCoord = lighting.biasTransform * lighting.lightTransform * pos4;
}