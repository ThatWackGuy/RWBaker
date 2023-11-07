#version 450
#extension GL_EXT_debug_printf : enable

layout(location = 0) in vec3 v_position;
layout(location = 1) in vec2 v_texCoord;
layout(location = 2) in vec4 v_color;

void main()
{
    debugPrintfEXT("POS %f %f %f", v_position.x, v_position.y, v_position.z);

    gl_Position = vec4(v_position, 1);
}