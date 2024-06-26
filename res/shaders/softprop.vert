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
    uint color;
    uint round;
    uint shadeRepeat;
    float contourExponent;
    float highlightMin;
    float shadowMin;
    float highlightExponent;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out vec4 f_shCoord;
layout(location = 2) out float f_layer;

void main()
{
    vec4 pos4 = vec4(v_position, 1);
    vec4 v_meshPos = mesh.transform * pos4;

    if (pass.idx == 0)
    {
        gl_Position = lighting.lightTransform * mesh.transform * pos4;
    }
    else
    {
        gl_Position = camera.transform * mesh.transform * pos4;
    }

    f_texCoord = v_texCoord;
    f_layer = v_meshPos.z;
    f_shCoord = lighting.biasTransform * lighting.lightTransform * pos4;
}