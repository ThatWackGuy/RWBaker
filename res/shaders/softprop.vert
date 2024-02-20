#version 450

layout(set = 0, binding = 0, std140) uniform SceneInfo
{
    mat4 transform;  // x+ right, y+ down matrix
    float layer;
    float layerCount;
    vec2 objectOffset;

    bool isShadow;
    float shadowRepeatCurrent;
    float shadowRepeatMax;
    vec2 lightOffset;
    vec2 shSize; // shadow texture size

    vec2 texSize;
    vec2 effectColorsSize;
    float effectA;
    float effectB;
} s;

layout(set = 0, binding = 1, std140) uniform RenderData
{
    vec2 pixelSize;
    uint vars;
    uint color;
    uint shadeRepeat;
    float contourExponent;
    float highlightMin;
    float shadowMin;
    float highlightExponent;
    uint pRain;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out flat int f_layer;
layout(location = 2) out float f_shLayer;

void main()
{
    if (s.isShadow)
    {
        float depthOffset = s.shadowRepeatCurrent / s.shadowRepeatMax;
        vec2 layerOffset = max(s.objectOffset, 0) * (s.layerCount - 1) + -s.objectOffset * (s.layerCount - 1 - v_position.z - depthOffset);
        vec2 offset = s.lightOffset * (s.layerCount - 1) + -s.lightOffset * (s.layerCount - 1 - v_position.z - depthOffset);
        gl_Position = s.transform * vec4(v_position.xy + layerOffset + offset, v_position.z + depthOffset, 1);
    }
    else
    {
        gl_Position = s.transform * vec4(v_position.xy + max(-s.objectOffset, 0) * (s.layerCount - 1) + s.objectOffset * v_position.z, v_position.z, 1);
    }

    f_texCoord = v_texCoord;
    f_layer = int(v_position.z);
    f_shLayer = (s.transform * vec4(0, 0, v_position.z - 0.8, 1)).z;
}