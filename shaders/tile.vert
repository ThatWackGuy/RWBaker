#version 450
#define TILE
#include "common.glsl"

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out vec4 f_shCoord;
layout(location = 2) out flat int f_layer;
layout(location = 3) out flat int f_localZ;

void main()
{
    vec4 v_pos4 = vec4(v_position, 1);
    vec4 v_meshPos = mesh.transform * v_pos4;

    f_shCoord = lighting.lightTransform * mesh.transform * v_pos4;

    if (pass.idx == 0)
    {
        gl_Position = f_shCoord;
    }
    else
    {
        gl_Position = camera.transform * mesh.transform * v_pos4;
    }

    f_texCoord = v_texCoord;
    f_layer = int(v_meshPos.z);
    f_localZ = int(v_meshPos.z - tile.startingZ);
}