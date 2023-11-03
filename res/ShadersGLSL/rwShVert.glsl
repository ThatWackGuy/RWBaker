#version 450

layout(set = 0, binding = 0, std140) uniform ShadowData
{
    mat4 vTransform; // x+ right, y+ down matrix
    vec2 vLightOffset;
    vec2 vObjectOffset;
    vec2 texSize;
    int layerCount;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;

// this is same as tileVert.glsl
void main()
{
    vec2 pos = v_position.xy + d.vLightOffset.xy * v_position.z;
    vec2 offset = v_position.xy + max(-d.vObjectOffset.xy, 0.0) * (d.layerCount - 1);
    gl_Position = d.vTransform * vec4(offset + pos, v_position.z, 1);

    f_texCoord = v_texCoord;
}