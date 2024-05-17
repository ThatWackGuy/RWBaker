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

layout(set = 0, binding = 4, std140) uniform RenderData
{
    float startingZ;
    float layerCount;
    vec2 texSize;

    vec2 pixelSize;
    uint vars;
    uint bevel;
    uint color;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out vec4 f_shCoord;
layout(location = 2) out flat int f_layer;

void main()
{
    vec4 pos4 = vec4(v_position.xyz, 1);

    if (pass.idx == 0)
    {
        gl_Position = lighting.lightTransform * pos4;
    }
    else
    {
        gl_Position = camera.transform * pos4;
    }

    f_texCoord = v_texCoord;
    f_layer = int(v_position.z - d.startingZ);
    f_shCoord = lighting.biasTransform * lighting.lightTransform * pos4;
}