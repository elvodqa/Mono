#version 410 core

layout(location = 0) in vec2 aPos;

layout(location = 2) in mat4 iModel;
layout(location = 6) in vec4 iColor;
layout(location = 7) in vec4 iUVRect;      // minU, minV, maxU, maxV
layout(location = 8) in vec2 iGlyphSize;
layout(location = 9) in float iDistanceRange;
layout(location = 10) in float iIsText;

uniform mat4 projection;
uniform mat4 view;

out vec2 vUV;
out vec4 vColor;
out vec4 vGlyphUV;
out float vDistanceRange;
out float vIsText;

void main()
{
    gl_Position = projection * view * iModel * vec4(aPos, 0.0, 1.0);

    // Compute UV from per-instance rectangle
    vUV = mix(iUVRect.xy, iUVRect.zw, aPos);

    vGlyphUV = iUVRect;           // reused by text path
    vColor = iColor;
    vDistanceRange = iDistanceRange;
    vIsText = iIsText;
}