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
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D rTex; // removal depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) in float f_layer;

layout(location = 0) out vec4 out_color;

void main()
{
    // Get pixel to be evaluated
    vec4 cPix = texture(tex, f_texCoord / d.texSize);

    // white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1))
    {
        discard;
    }

    if (gl_FragCoord.z < texture(rTex, gl_FragCoord.xy / pass.size).g)
    {
        discard;
    }

    // decals are always colored
    out_color = texture(tex, vec2(f_texCoord.x, f_texCoord.y + d.pixelSize.y) / d.texSize);
    out_color.a = .8;
}