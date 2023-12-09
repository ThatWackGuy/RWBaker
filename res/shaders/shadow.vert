#version 450

layout(set = 0, binding = 0, std140) uniform ShadowData
{
    mat4 vTransform; // x+ right, y+ down matrix
    vec2 vLightOffset;
    vec2 vObjectOffset;
    vec2 texSize;
    int layerCount; // layer count * 30
    float repeatCurrent;
    float repeatMax;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;

void main()
{
    float depthOffset = d.repeatCurrent / d.repeatMax;
    vec2 layerOffset = max(d.vObjectOffset, 0) * (d.layerCount - 1) + -d.vObjectOffset * (d.layerCount - 1 - v_position.z - depthOffset);
    vec2 offset = d.vLightOffset * (d.layerCount - 1) + -d.vLightOffset * (d.layerCount - 1 - v_position.z - depthOffset);
    gl_Position = d.vTransform * vec4(v_position.xy + layerOffset + offset, v_position.z + depthOffset, 1);

    f_texCoord = v_texCoord;
}