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
    float vars;
    uint bevel;
    uint color;
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
    vec4 cPix;

    // bevel props
    if (d.bevel > 0)
    {
        vec4 black = vec4(0, 0, 0, 1);
        vec4 h = texture(tex, (f_texCoord - d.bevel) / s.texSize); // highlights
        vec4 b = texture(tex, f_texCoord / s.texSize); // base
        vec4 s = texture(tex, (f_texCoord + d.bevel) / s.texSize); // shadows

        if (b == vec4(1)) discard;

        // highlighted
        if (h != black && b == b)
        {
            cPix = vec4(0, 0, 1, 1);
        }
        // shadowed
        else if (s != black && b == b)
        {
            cPix = vec4(1, 0, 0, 1);
        }
        // base
        else
        {
            cPix = vec4(0, 1, 0, 1);
        }
    }
    else
    {
        cPix = texture(tex, f_texCoord / s.texSize); // normal standard prop
    }

    // Black, white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1) || cPix == vec4(0, 0, 0, 1))
    {
        discard;
    }

    // Shadows are rendered in red
    if (s.isShadow)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    // Check if pixel is unlit
    float paletteOffset = d.pRain == 1 ? 10 : 2;
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
    
    // get colored side of the prop if it has the tag
    vec4 colored = vec4(0);
    float coloredPer = 0;
    if (d.color == 1)
    {
        colored = texture(tex, vec2(f_texCoord.x + d.pixelSize.x, f_texCoord.y) / s.texSize);
        coloredPer = .5; // just eyeballing it, don't expect much precision
    }

    // Red is Shadows
    if (cPix.r == 1)
    {
        // Purple is Effect A
        if (cPix.b == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * d.pixelSize.x + f_texCoord.x, f_texCoord.y) / s.texSize).r;

            vec4 fA = texture(eTex, vec2(s.effectA * 2 + (f_layer == 0 ? 0 : 1) + 0.5, effectOffset) / s.effectColorsSize);
            out_color = mix(pB, fA, 1 - intensity);

            return;
        }

        out_color = mix(pS, pF, f_layer < 10 ? 0 : pFI);
        out_color = mix(out_color, colored, coloredPer);
        
        return;
    }
    // Blue is Highlights
    else if (cPix.b == 1)
    {
        // Cyan is Effect B
        if (cPix.g == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * d.pixelSize.x + f_texCoord.x, f_texCoord.y) / s.texSize).r;

            vec4 fB = texture(eTex, vec2(s.effectB * 2 + (f_layer == 0 ? 0 : 1) + 0.5, effectOffset) / s.effectColorsSize);
            out_color = mix(pB, fB, 1 - intensity);

            return;
        }

        out_color = mix(pH, pF, f_layer < 10 ? 0 : pFI);
        out_color = mix(out_color, colored, coloredPer);

        return;
    }

    // Everything else is Base
    out_color = mix(pB, pF, f_layer < 10 ? 0 : pFI);
    out_color = mix(out_color, colored, coloredPer);
}