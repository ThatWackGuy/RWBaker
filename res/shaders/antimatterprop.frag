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

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) flat in int f_layer;
layout(location = 2) in float f_shLayer;
layout(location = 3) in flat int f_localZ;

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

    float dpthRemove = pow(cPix.g, d.contourExponent) * d.layerCount;

    float renderTo = round(clamp(dpthRemove, 0, 31));

    if (f_layer > renderTo)
    {
        discard;
    }

    // antimatter props don't have shadows

    // discard doesn't do a write and skips pixel, we're replacing *and* writing to the depth buffer here, disallowing anything to be here
    out_color = vec4(0);
}