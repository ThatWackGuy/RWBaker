#version 450
#extension GL_EXT_debug_printf : enable

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

    vec2 tileSize;
    uint bfTiles;
    uint vars;
    uint pRain;
    uint isBox;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out flat int f_layer;
layout(location = 2) out float f_shLayer;
layout(location = 3) out flat int f_localZ;

void main()
{
    float localZ = v_position.z - d.startingZ;

    if (s.isShadow)
    {
        float depthOffset = s.shadowRepeatCurrent / s.shadowRepeatMax;
        vec2 layerOffset = max(-s.objectOffset, 0) * (d.layerCount - 1) + s.objectOffset * (localZ - depthOffset);
        vec2 offset = s.lightOffset * (d.layerCount - 1) + -s.lightOffset * (d.layerCount - 1 - localZ - depthOffset);
        gl_Position = s.transform * vec4(v_position.xy + offset + layerOffset, v_position.z + depthOffset, 1);
    }
    else
    {
        gl_Position = s.transform * vec4(v_position.xy + max(-s.objectOffset, 0) * (d.layerCount - 1) + s.objectOffset * localZ, v_position.z, 1);
    }

    f_texCoord = v_texCoord;
    f_layer = int(v_position.z);
    f_shLayer = (s.transform * vec4(0, 0, v_position.z - 0.8, 1)).z;
    f_localZ = int(localZ);
    debugPrintfEXT("localZ: %d", f_localZ);
}