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

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map
layout(set = 1, binding = 4) uniform sampler2D rTex; // removal depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) in vec4 f_shCoord;
layout(location = 2) in flat int f_layer;

layout(location = 0) out vec4 out_color;

bool inShadow()
{
    vec4 shadowCoords = f_shCoord / f_shCoord.w;
    shadowCoords = shadowCoords * 0.5 + 0.5;
    shadowCoords.y = 1 - shadowCoords.y;

    return f_shCoord.z > texture(sTex, shadowCoords.xy).r + lighting.shadowBias;
}

void main()
{
    // Get pixel to be evaluated
    vec4 cPix;

    // bevel props
    if (d.bevel > 0)
    {
        vec4 black = vec4(0, 0, 0, 1);
        vec4 h = texture(tex, (f_texCoord - d.bevel) / d.texSize); // highlights
        vec4 b = texture(tex, f_texCoord / d.texSize); // base
        vec4 s = texture(tex, (f_texCoord + d.bevel) / d.texSize); // shadows

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
        cPix = texture(tex, f_texCoord / d.texSize); // normal standard prop
    }

    // Black, white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1) || cPix == vec4(0, 0, 0, 1))
    {
        discard;
    }

    // Shadows are rendered in red
    if (pass.idx == 0)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    // Check if pixel is unlit
    float paletteOffset = 2;
    float effectOffset = 0;
    if (inShadow())
    {
        paletteOffset += 3;
        effectOffset += 1;
    }

    // Palette colors
    vec2 pSize = vec2(32.0, 16.0);
    float palX = f_layer;
    vec4 pH    =     mix( texture(pTex, vec2(palX, paletteOffset    ) / pSize), texture(pTex, vec2(palX, paletteOffset + 8 ) / pSize), lighting.pRain );   // Highlights
    vec4 pB    =     mix( texture(pTex, vec2(palX, paletteOffset + 1) / pSize), texture(pTex, vec2(palX, paletteOffset + 9 ) / pSize), lighting.pRain );   // Base
    vec4 pS    =     mix( texture(pTex, vec2(palX, paletteOffset + 2) / pSize), texture(pTex, vec2(palX, paletteOffset + 10) / pSize), lighting.pRain );   // Shadows
    vec4 pF    =     mix( texture(pTex, vec2(1,    paletteOffset - 2) / pSize), texture(pTex, vec2(1,    paletteOffset     ) / pSize), lighting.pRain );   // Fog
    float pFI  = 1 - mix( texture(pTex, vec2(9,    paletteOffset - 2) / pSize), texture(pTex, vec2(9,    paletteOffset     ) / pSize), lighting.pRain ).r; // Fog Intensity

    // get colored side of the prop if it has the tag
    vec4 colored = vec4(0);
    float coloredPer = 0;
    if (d.color == 1)
    {
        colored = texture(tex, vec2(f_texCoord.x + d.pixelSize.x, f_texCoord.y) / d.texSize);
        coloredPer = .5; // just eyeballing it, don't expect much precision
    }

    // Red is Shadows
    if (cPix.r == 1)
    {
        // Purple is Effect A
        if (cPix.b == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * d.pixelSize.x + f_texCoord.x, f_texCoord.y) / d.texSize).r;

            vec4 fA = texture(eTex, vec2(palette.effectA * 2 + (f_layer == 0 ? 0 : 1) + 0.5, effectOffset) / palette.effectColorsSize);
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
            float intensity = texture(tex, vec2(d.vars * 20 * d.pixelSize.x + f_texCoord.x, f_texCoord.y) / d.texSize).r;

            vec4 fB = texture(eTex, vec2(palette.effectB * 2 + (f_layer == 0 ? 0 : 1) + 0.5, effectOffset) / palette.effectColorsSize);
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