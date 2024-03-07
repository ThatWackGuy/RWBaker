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
    float contourExponent;
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
    vec4 rotatedPos = d.rotate * vec4(v_position.xyz, 1);
    float localZ = rotatedPos.z - d.startingZ;

    // antimatter props don't have shadows
    gl_Position = s.transform * vec4(rotatedPos.xy + max(-s.objectOffset, 0) * (d.layerCount - 1) + s.objectOffset * localZ, rotatedPos.z, 1);

    f_texCoord = v_texCoord;
    f_layer = int(rotatedPos.z);
    f_shLayer = (s.transform * vec4(0, 0, rotatedPos.z - 0.8, 1)).z;
}