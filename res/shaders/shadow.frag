#version 450
#extension GL_EXT_debug_printf : enable

layout(set = 0, binding = 0, std140) uniform ShadowData
{
    mat4 vTransform; // x+ right, y+ down matrix
    vec2 vLightOffset;
    vec2 vObjectOffset;
    vec2 texSize;
    int layerCount; // layer count * 30
    int repeatCurrent;
    int repeatMax;
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // tile texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D sTex; // shadow depth map

layout(location = 0) in vec2 f_texCoord;

layout(location = 0) out vec4 out_color;

void main()
{    
    vec4 cPix = texture(tex, f_texCoord / d.texSize);
    
    // Black, white and transparent are skipped
    if (cPix.w == 0 || cPix == vec4(1) || cPix == vec4(0, 0, 0, 1))
    {
        discard;
    }
    
    out_color = vec4(gl_FragCoord.z, 0, 0, 1);
}