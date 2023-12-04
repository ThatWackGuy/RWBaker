#version 450

layout(set = 0, binding = 0, std140) uniform RenderData
{
    mat4 vTransform;  // x+ right, y+ down matrix
    vec2 vOffsetPerLayer; // object offset
    vec2 texSize;
    vec2 shTexSize;
    bool pRain;
    int layerCount;
    int palLayer;
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // tile texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D sTex; // shadow depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) flat in int f_layer;
layout(location = 2) in float f_shLayer;

layout(location = 0) out vec4 out_color;

void main()
{
    vec2 pSize = vec2(32.0, 16.0);
    
    float offset = 2;
    
    if (d.pRain)
    {
        offset = 10;
    }
    
    // check if pixel is unlit
    if (f_shLayer > texture(sTex, gl_FragCoord.xy / d.shTexSize).r)
    {
        offset += 3;
    }
    
    vec4 pH = texture(pTex, vec2(f_layer + d.palLayer * 10, offset) / pSize);
    vec4 pB = texture(pTex, vec2(f_layer + d.palLayer * 10, offset + 1) / pSize);
    vec4 pS = texture(pTex, vec2(f_layer + d.palLayer * 10, offset + 2) / pSize);
    
    vec4 cPix = texture(tex, f_texCoord / d.texSize);
    
    // Black, white and transparent are skipped
    if (cPix.w == 0 || cPix == vec4(1) || cPix == vec4(0, 0, 0, 1))
    {
        discard;
    }
    
    if (cPix == vec4(0, 0, 1, 1))
    {
        out_color = pH;
        return;
    }
    
    if (cPix == vec4(1, 0, 0, 1))
    {
        out_color = pS;
        return;
    }
    
    out_color = pB;
}