#version 450

layout(set = 0, binding = 0, std140) uniform SceneInfo
{
    mat4 transform;  // x+ right, y+ down matrix
    float layer;
    float layerCount;
    vec2 objectOffset;

    bool isShadow;
    float shadowRepeatCurrent;
    float shadowRepeatMax;
    vec2 lightOffset;
    vec2 shSize; // shadow texture size

    vec2 texSize;
    vec2 effectColorsSize;
    float effectA;
    float effectB;
} s;

layout(set = 0, binding = 1, std140) uniform RenderData
{
    vec2 pixelSize;
    uint vars;
    uint pRain;
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) flat in int f_layer;
layout(location = 2) in float f_shLayer;

layout(location = 0) out vec4 out_color;

void main()
{
    // Get pixel to be evaluated
    vec4 cPix = texture(tex, f_texCoord / s.texSize);

    // white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1))
    {
        discard;
    }

    // no shadows
    if (s.isShadow)
    {
        discard;
    }

    // Check if pixel is unlit
    float paletteOffset = d.pRain == 1 ? 9.5 : 1.5;
    float effectOffset = d.pRain == 1 ? 2 : 0;
    if (f_shLayer > texture(sTex, gl_FragCoord.xy / s.shSize).r)
    {
        paletteOffset += 3;
        effectOffset += 1;
    }

    // Palette colors
    vec2 pSize = vec2(32.0, 16.0);
    float palX = f_layer + s.layer + .2;
    vec4 pH = texture(pTex, vec2(palX, paletteOffset) / pSize); // Highlights
    vec4 pB = texture(pTex, vec2(palX, paletteOffset + 1) / pSize); // Base
    vec4 pS = texture(pTex, vec2(palX, paletteOffset + 2) / pSize); // Shadows
    vec4 pF = texture(pTex, vec2(1.2, paletteOffset - 2) / pSize); // Fog
    float pFI = texture(pTex, vec2(9.2, paletteOffset - 2) / pSize).r; // Fog Intensity
    
    // decals are always colored
    out_color = texture(tex, vec2(f_texCoord.x, f_texCoord.y + d.pixelSize.y) / s.texSize);
    out_color.a = .8;
}