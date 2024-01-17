#version 450

layout(set = 0, binding = 0, std140) uniform RenderData
{
    mat4 vTransform;  // x+ right, y+ down matrix
    vec2 vOffsetPerLayer; // object offset
    vec2 texSize;
    vec2 shTexSize;
    vec2 tileSize;
    int bfTiles;
    bool pRain;
    int layerCount;
    int palLayer;
    bool isBox;
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
    
    vec4 cPix;
    // BOX TYPE RENDERING
    if (d.isBox)
    {
        vec2 tileSize = d.tileSize * 20;
        float bounds = d.bfTiles * 20;
        vec2 pxCoord = vec2(0);

        // FACE
        if (f_layer == 0)
        {
            pxCoord = vec2(f_texCoord.x, d.tileSize.x * tileSize.y + f_texCoord.y);
        }
        // VERTICAL PIECES
        else if (f_texCoord.y > bounds + 5 && f_texCoord.y < bounds + tileSize.y - 5)
        {
            // left piece
            if (f_texCoord.x > bounds && f_texCoord.x < bounds + 5)
            {
                pxCoord = vec2(20 + f_layer, f_texCoord.y - bounds);
            }
            // right piece
            else if (f_texCoord.x > bounds + tileSize.x - 5 && f_texCoord.x < bounds + tileSize.x)
            {
                pxCoord = vec2(30 + f_layer, (d.tileSize.x - 1) * tileSize.y + f_texCoord.y - bounds);
            }
            else
            {
                discard;
            }
        }
        // HORIZONTAL PIECES
        else if (f_texCoord.x > bounds && f_texCoord.x < bounds + tileSize.x)
        {
            float pieceIdx = floor((f_texCoord.x - bounds) / 20);

            // top
            if (f_texCoord.y > bounds && f_texCoord.y < bounds + 5)
            {
                pxCoord = vec2(mod(f_texCoord.x - bounds, 20), pieceIdx * tileSize.y + f_layer);
            }
            // bottom
            else if (f_texCoord.y > bounds + tileSize.y - 5 && f_texCoord.y < bounds + tileSize.y)
            {
                pxCoord = vec2(mod(f_texCoord.x - bounds, 20), pieceIdx * tileSize.y + f_layer);
            }
            else
            {
                discard;
            }
        }
        else
        {
            discard;
        }

        cPix = texture(tex, pxCoord / d.texSize);
    }
    else
    {
        cPix = texture(tex, f_texCoord / d.texSize);
    }
    
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