#version 450

layout(set = 0, binding = 0, std140) uniform CameraInfo
{
    mat4 transform;
    mat4 lightTransform;

    float stcId; // current stencil id
    vec2 cameraSize; // current stencil size

    vec2 effectColorsSize;
    uint effectA;
    uint effectB;

    float pRain;
} s;

layout(set = 0, binding = 1, std140) uniform RenderData
{
    float startingZ;
    float layerCount;
    vec2 texSize;

    vec2 pixelSize;
} d;

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

layout(location = 0) out vec2 f_texCoord;
layout(location = 1) out flat int f_layer;

void main()
{
    // decals don't have shadows
    gl_Position = s.transform * vec4(v_position.xyz, 1);

    f_texCoord = v_texCoord;
    f_layer = int(v_position.z);
}