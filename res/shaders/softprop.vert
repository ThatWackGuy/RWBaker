#version 450

layout(set = 0, binding = 0, std140) uniform SceneInfo
{
    mat4 transform;  // x+ right, y+ down matrix
    vec2 objectOffset;

    bool isShadow;
    float shadowRepeatCurrent;
    float shadowRepeatMax;
    vec2 lightOffset;
    vec2 shSize; // shadow texture size

    vec2 effectColorsSize;
    uint effectA;
    uint effectB;
} s;

layout(set = 0, binding = 1, std140) uniform RenderData
{
    float startingZ;
    float layerCount;
    vec2 texSize;

    mat4 rotate;
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
layout(location = 3) out float f_localZ;

void main()
{
    vec4 rotatedPos = d.rotate * vec4(v_position.xyz, 1);
    f_localZ = rotatedPos.z - d.startingZ;

    if (s.isShadow)
    {
        float depthOffset = s.shadowRepeatCurrent / s.shadowRepeatMax;
        // max(-s.objectOffset, 0) * (d.layerCount - 1) + s.objectOffset * localZ
        vec2 layerOffset = max(s.objectOffset, 0) * (d.layerCount - 1) + -s.objectOffset * (d.layerCount - 1 - f_localZ - depthOffset);
        vec2 offset = s.lightOffset * (d.layerCount - 1) + -s.lightOffset * (d.layerCount - 1 - f_localZ - depthOffset);
        gl_Position = s.transform * vec4(rotatedPos.xy + offset + layerOffset, rotatedPos.z + depthOffset, 1);
    }
    else
    {
        gl_Position = s.transform * vec4(rotatedPos.xy + max(-s.objectOffset, 0) * (d.layerCount - 1) + s.objectOffset * f_localZ, rotatedPos.z, 1);
    }

    f_texCoord = v_texCoord;
    f_layer = int(rotatedPos.z);
    f_shLayer = (s.transform * vec4(0, 0, rotatedPos.z - 0.8, 1)).z;
}