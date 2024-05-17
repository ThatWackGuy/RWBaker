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
    uint vars;
    float contourExponent;
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) flat in int f_layer;
layout(location = 2) in flat int f_localZ;

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

    float dpthRemove = pow(1 - cPix.g, d.contourExponent) * d.layerCount;

    float renderTo = round(clamp(dpthRemove, 0, 31));

    if (f_layer < renderTo)
    {
        discard;
    }

    out_color = vec4(0, 1 - gl_FragCoord.z, 0, 1);
    gl_FragDepth = 0;
}