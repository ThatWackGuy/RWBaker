#version 450

layout(set = 0, binding = 0, std140) uniform RenderData
{
    mat4 vTransform;  // x+ right, y+ down matrix
    vec2 vOffsetPerLayer; // object offset
    vec2 texSize;
    vec2 shTexSize;
    bool pRain;
    int layerCount;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out flat int f_layer;
layout(location = 2) out float f_shLayer;

void main()
{
    gl_Position = d.vTransform * vec4(v_position.xy + max(-d.vOffsetPerLayer.xy, 0.0) * (d.layerCount - 1) + d.vOffsetPerLayer.xy * v_position.z, v_position.z, 1);

    f_texCoord = v_texCoord;
    f_layer = int(v_position.z);
    f_shLayer = (d.vTransform * vec4(0, 0, v_position.z - 0.2, 1)).z;
}