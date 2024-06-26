#version 450

layout(set = 0, binding = 0, std140) uniform CameraData
{
    mat4 transform;
} camera;

layout(set = 0, binding = 1, std140) uniform PassData
{
    uint idx;
    vec2 size;
} pass;

layout(set = 0, binding = 2, std140) uniform LightData
{
    mat4 lightTransform;
    mat4 biasTransform;
    float shadowBias;
    float pRain;
} lighting;

layout(set = 0, binding = 3, std140) uniform PaletteData
{
    vec2 effectColorsSize;
    uint effectA;
    uint effectB;
} palette;

layout(set = 0, binding = 4, std140) uniform MeshData
{
    mat4 transform;
} mesh;

layout(set = 0, binding = 5, std140) uniform RenderData
{
    float startingZ;
    float layerCount;
    vec2 texSize;

    vec2 pixelSize;
    uint vars;
    float contourExponent;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out flat int f_layer;
layout(location = 2) out flat int f_localZ;

void main()
{
    vec4 vpos4 = vec4(v_position.xyz, 1);
    vec4 v_meshPos = mesh.transform * vpos4;

    // antimatter props don't have shadows
    gl_Position = camera.transform * mesh.transform * vpos4;

    f_texCoord = v_texCoord;
    f_layer = int(v_meshPos.z);
    f_localZ = int(v_meshPos.z - d.startingZ);
}