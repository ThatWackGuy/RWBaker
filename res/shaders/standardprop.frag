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
layout(location = 2) in float f_layer;

layout(location = 0) out vec4 out_color;

int inShadow()
{
    vec4 shadowCoords = f_shCoord / f_shCoord.w;
    shadowCoords = shadowCoords * 0.5 + 0.5;
    shadowCoords.y = 1 - shadowCoords.y;

    return int(f_shCoord.z > texture(sTex, shadowCoords.xy).r + lighting.shadowBias);
}

vec4 shadePixel(vec4 cPix, float depth, int shadow)
{
    // Check if pixel is unlit
    float paletteOffset = 2 + shadow * 3;
    float effectOffset = shadow;

    // Palette colors
    const vec2 pSize = vec2(32.0, 16.0);
    float palX = clamp(depth, 0, 31);
    vec4 pH    =     mix( texture(pTex, vec2(palX, paletteOffset    ) / pSize), texture(pTex, vec2(palX, paletteOffset + 8 ) / pSize), lighting.pRain );   // Highlights
    vec4 pB    =     mix( texture(pTex, vec2(palX, paletteOffset + 1) / pSize), texture(pTex, vec2(palX, paletteOffset + 9 ) / pSize), lighting.pRain );   // Base
    vec4 pS    =     mix( texture(pTex, vec2(palX, paletteOffset + 2) / pSize), texture(pTex, vec2(palX, paletteOffset + 10) / pSize), lighting.pRain );   // Shadows
    vec4 pF    =     mix( texture(pTex, vec2(1,    paletteOffset - 2) / pSize), texture(pTex, vec2(1,    paletteOffset     ) / pSize), lighting.pRain );   // Fog
    float pFI  =     mix( texture(pTex, vec2(9,    paletteOffset - 2) / pSize), texture(pTex, vec2(9,    paletteOffset     ) / pSize), lighting.pRain ).r; // Fog Intensity

    // final shaded color
    vec4 shadedPx;

    // Red is Shadows
    if (cPix.r == 1)
    {
        // Purple is Effect A
        if (cPix.b == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * d.pixelSize.x + f_texCoord.x, f_texCoord.y) / d.texSize).r;

            vec4 fA = texture(eTex, vec2(palette.effectA * 2 + (depth == 0 ? 0 : 1), effectOffset) / palette.effectColorsSize);
            shadedPx = mix(pB, fA, 1 - intensity);
        }

        shadedPx = mix(pS, pF, depth < 10 ? 0 : pFI);
    }
    // Blue is Highlights
    else if (cPix.b == 1)
    {
        // Cyan is Effect B
        if (cPix.g == 1)
        {
            float intensity = texture(tex, vec2(d.vars * 20 * d.pixelSize.x + f_texCoord.x, f_texCoord.y) / d.texSize).r;

            vec4 fB = texture(eTex, vec2(palette.effectB * 2 + (depth == 0 ? 0 : 1), effectOffset) / palette.effectColorsSize);
            shadedPx = mix(pB, fB, 1 - intensity);
        }

        shadedPx = mix(pH, pF, depth < 10 ? 0 : pFI);
    }
    // Everything else is Base
    else
    {
        shadedPx = mix(pB, pF, depth < 10 ? 0 : pFI);
    }

    // get colored side of the prop if it has the tag
    vec4 colored = vec4(0);
    if (d.color == 1)
    {
        colored = texture(tex, vec2(f_texCoord.x + d.pixelSize.x, f_texCoord.y) / d.texSize);
        shadedPx = mix(shadedPx, colored, .5); // just eyeballing it, don't expect much precision
    }

    return shadedPx;
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

    out_color = shadePixel(cPix, f_layer, inShadow());
}