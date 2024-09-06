#version 450
#define SOFTPROP
#include "common.glsl"

layout(set = 1, binding = 0) uniform sampler2D tex; // prop texture
layout(set = 1, binding = 1) uniform sampler2D pTex; // palette texture
layout(set = 1, binding = 2) uniform sampler2D eTex; // effect color texture
layout(set = 1, binding = 3) uniform sampler2D sTex; // shadow depth map
layout(set = 1, binding = 4) uniform sampler2D rTex; // removal depth map

layout(location = 0) in vec2 f_texCoord;
layout(location = 1) in vec4 f_shCoord;
layout(location = 2) in float f_layer;

layout(location = 0) out vec4 out_color;

float depth(ivec2 pos)
{
    vec4 px = texelFetch(tex, pos, 0);

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
    ivec2 texPos = ivec2(f_texCoord);

    // Get pixel to be evaluated
    vec4 cPix = texelFetch(tex, texPos, 0);

    // white and transparent are skipped
    if (cPix.a == 0 || cPix == vec4(1))
    {
        discard;
    }

    // see: renderProps.lingo
    float dpth = depth(texPos);
    float dpthRemove = pow(1 - dpth, fProp.contourExponent) * fProp.layerCount;

    float renderFrom = clamp(dpthRemove / fProp.round, 0, 30);
    float renderTo = clamp(mix(fProp.layerCount - (dpthRemove/2) * (fProp.round-1), dpthRemove, cPix.r), 0, 30);

    if (f_layer - fProp.startingZ < renderFrom || f_layer - fProp.startingZ > renderTo)
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

    if (fProp.shadeRepeat > 0)
    {
        float ang = 0;
        for (int shadeOffset = 1; shadeOffset <= fProp.shadeRepeat; shadeOffset++)
        {
            // (1, 0) + (1, 1) + (0, 1)
            ang += (dpth - depth(ivec2(texPos.x - shadeOffset, texPos.y)) + (depth(ivec2(texPos.x + shadeOffset, texPos.y)) - dpth))
                +  (dpth - depth(ivec2(texPos.x - shadeOffset, texPos.y - shadeOffset)) + (depth(ivec2(texPos.x + shadeOffset, texPos.y + shadeOffset)) - dpth))
                +  (dpth - depth(ivec2(texPos.x, texPos.y - shadeOffset)) + (depth(ivec2(texPos.x, texPos.y + shadeOffset)) - dpth));
        }
        ang /= fProp.shadeRepeat * 3.0;

        if (ang * 10 * pow(dpth, fProp.highlightExponent) > fProp.highlightMin)
        {
            palCol = vec4(0, 0, 1, 1); // blue because highlights, duh

        }
        else if(-ang * 10 > fProp.shadowMin)
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

    float intensity = texelFetch(tex, ivec2(fProp.vars * 20 * (fProp.pixelSize.x) + texPos.x, texPos.y), 0).r;
    out_color = shadePixel(
        cPix,
        pTex, lighting.pRain,
        eTex, intensity,
        f_layer, inShadow(f_shCoord, sTex, lighting.shadowBias)
    );

    // get colored side of the prop if it has the tag
    if (fProp.color != 1) return;

    vec4 colored = texelFetch(tex, ivec2(texPos.x + fProp.pixelSize.x, texPos.y), 0);
    out_color = mix(out_color, colored, .5); // just eyeballing it, don't expect much precision
}