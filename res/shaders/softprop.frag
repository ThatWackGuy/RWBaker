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
    uint color;
    uint round;
    uint shadeRepeat;
    float contourExponent;
    float highlightMin;
    float shadowMin;
    float highlightExponent;
} d;

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map
layout(set = 1, binding = 4) uniform sampler2D rTex; // removal depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) in vec4 f_shCoord;
layout(location = 2) in flat int f_layer;
layout(location = 3) in float f_localZ;

layout(location = 0) out vec4 out_color;

float depth(vec2 pos)
{
    vec4 px = texture(tex, pos / d.texSize);

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
    vec4 cPix = texture(tex, f_texCoord / d.texSize);

    // white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1))
    {
        discard;
    }

    // see: renderProps.lingo
    float dpth = depth(f_texCoord);
    float dpthRemove = pow(1 - dpth, d.contourExponent) * d.layerCount;

    float renderFrom = clamp(dpthRemove / d.round, 0, 30);
    float renderTo = clamp(mix(d.layerCount - (dpthRemove/2) * (d.round-1), dpthRemove, cPix.r), 0, 30);

    if (f_localZ < renderFrom || f_localZ > renderTo)
    {
        discard;
    }

    // check removal texture and remove
    if (gl_FragCoord.z < texture(rTex, gl_FragCoord.xy / pass.size).g)
    {
        discard;
    }

    // Shadows are rendered in red
    if (pass.idx == 0)
    {
        out_color = vec4(gl_FragCoord.z, 0, 0, 1);
        return;
    }

    vec4 palCol = vec4(0, 1, 0, 1); // green

    if (d.shadeRepeat > 0)
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
            palCol = vec4(0, 0, 1, 1); // highlights
        }
        else if(cPix.b < 1.0 / 3.0)
        {
            palCol = vec4(1, 0, 0, 1); // shadows
        }
    }

    cPix = palCol;

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
    vec4 pH    = mix( texture(pTex, vec2(palX, paletteOffset    ) / pSize), texture(pTex, vec2(palX, paletteOffset + 8 ) / pSize), lighting.pRain );   // Highlights
    vec4 pB    = mix( texture(pTex, vec2(palX, paletteOffset + 1) / pSize), texture(pTex, vec2(palX, paletteOffset + 9 ) / pSize), lighting.pRain );   // Base
    vec4 pS    = mix( texture(pTex, vec2(palX, paletteOffset + 2) / pSize), texture(pTex, vec2(palX, paletteOffset + 10) / pSize), lighting.pRain );   // Shadows
    vec4 pF    = mix( texture(pTex, vec2(1,    paletteOffset - 2) / pSize), texture(pTex, vec2(1,    paletteOffset     ) / pSize), lighting.pRain );   // Fog
    float pFI  = mix( texture(pTex, vec2(9,    paletteOffset - 2) / pSize), texture(pTex, vec2(9,    paletteOffset     ) / pSize), lighting.pRain ).r; // Fog Intensity

    // get colored side of the prop if it has the tag
    vec4 colored = vec4(0);
    float coloredPer = 0;
    if (d.color == 1)
    {
        colored = texture(tex, vec2(f_texCoord.x, f_texCoord.y + d.pixelSize.y) / d.texSize);
        coloredPer = .5;
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