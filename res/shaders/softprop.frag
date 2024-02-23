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
    uint color;
    uint shadeRepeat;
    float contourExponent;
    float highlightMin;
    float shadowMin;
    float highlightExponent;
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

float depth(vec2 pos)
{
    vec4 px = texture(tex, pos / s.texSize);

    if (px.g > 0)
    {
        return px.g;
    }
        
    if (px.b > 0)
    {
        return px.b;
    }
        
    return px.r;
}

void main()
{
    // Get pixel to be evaluated
    vec4 cPix = texture(tex, f_texCoord / s.texSize);

    // white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1))
    {
        discard;
    }

    // see: renderProps.lingo
    float dpth = depth(f_texCoord);
    float dpthRemove = pow(1 - dpth, d.contourExponent) * s.layerCount;

    float renderFrom = round(clamp(dpthRemove, 0, 30));
    float renderTo = round(clamp(mix(s.layerCount, dpthRemove, cPix.r), 0, 30));

    if (f_layer < renderFrom || f_layer > renderTo)
    {
        discard;
    }

    // Shadows are rendered in red
    if (s.isShadow)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    vec4 palCol = vec4(0, 1, 0, 1); // green

    if (d.shadeRepeat > -1)
    {
        float ang = 0;
        for (int shadeOffset = 1; shadeOffset <= d.shadeRepeat; shadeOffset++)
        {
            // (1, 0) + (1, 1) + (0, 1)
            ang += (dpth - depth(vec2(f_texCoord.x - shadeOffset, f_texCoord.y)) + (depth(vec2(f_texCoord.x + shadeOffset, f_texCoord.y)) - dpth))
                +  (dpth - depth(vec2(f_texCoord.x - shadeOffset, f_texCoord.y - shadeOffset)) + (depth(vec2(f_texCoord.x + shadeOffset, f_texCoord.y + shadeOffset)) - dpth))
                +  (dpth - depth(vec2(f_texCoord.x, f_texCoord.y - shadeOffset)) + (depth(vec2(f_texCoord.x, f_texCoord.y + shadeOffset)) - dpth));
        }
        ang /= d.shadeRepeat * 3.0;

        if (ang * 10 * pow(dpth, d.highlightExponent) > d.highlightMin)
        {
            palCol = vec4(0, 0, 1, 1); // blue because highlights, duh
            
        }
        else if(-ang * 10 > d.shadowMin)
        {
            palCol = vec4(1, 0, 0, 0); // red because shadows
        }
    }
    else
    {
        if(cPix.b > (1.0 / 3.0) * 2.0)
        {
            palCol = vec4(0, 0, 1, 1);
        }
        else if(cPix.b < 1 / 3.0)
        {
            palCol = vec4(1, 0, 0, 1);
        }
    }

    cPix = palCol;

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
    
    // get colored side of the prop if it has the tag
    vec4 colored = vec4(0);
    float coloredPer = 0;
    if (d.color == 1)
    {
        colored = texture(tex, vec2(f_texCoord.x, f_texCoord.y + d.pixelSize.y) / s.texSize);
        coloredPer = .5;
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