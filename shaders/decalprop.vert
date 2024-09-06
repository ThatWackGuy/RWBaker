#version 450
#define DECALPROP
#include "common.glsl"

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out flat ivec2 f_texCoord;
layout(location = 1) out float f_layer;

void main()
{
    vec4 vpos4 = vec4(v_position.xyz, 1);
    vec4 v_meshPos = mesh.transform * vpos4;

    // decals don't have shadows
    gl_Position = camera.transform * mesh.transform * vpos4;

    f_texCoord = ivec2(v_texCoord);
    f_layer = v_meshPos.z;
}