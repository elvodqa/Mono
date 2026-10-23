#version 410 core
in vec2 vUV;
in vec4 vColor;
in vec4 vGlyphUV;
in float vDistanceRange;
in float vIsText;
out vec4 FragColor;

uniform sampler2D uTexture;

// MSDF helper
float median(float r, float g, float b) {
    return max(min(r, g), min(max(r, g), b));
}

float roundedBoxSDF(vec2 centerPosition, vec2 size, float radius) {
    return length(max(abs(centerPosition) - size + radius, 0.0)) - radius;
}

void main() {
    vec4 finalColor;

    if (vIsText > 0.5) {
        // ---------- MSDF text rendering ----------
        vec2 customUVfoo = vec2(
        mix(vGlyphUV.x, vGlyphUV.z, vUV.x),
        mix(vGlyphUV.y, vGlyphUV.w, vUV.y)
        );

        vec2 customUV = vUV;

        vec3 msdf = texture(uTexture, customUV).rgb;
        float sd = median(msdf.r, msdf.g, msdf.b) - 0.5;

        vec2 texSize = vec2(textureSize(uTexture, 0));
        vec2 unitRange = vec2(vDistanceRange) / texSize;

        // Use derivatives of the *atlas UVs* (customUV) so edge thickness is correct
        vec2 screenTexSize = vec2(1.0) / fwidth(customUV);
        float screenPxRange = max(0.5 * dot(unitRange, screenTexSize), 1.0);

        float screenPxDistance = screenPxRange * sd;
        float opacity = smoothstep(-0.5, 0.5, screenPxDistance);

        float dist = sd * screenPxRange;
        float glyphAlpha = smoothstep(-0.5, 0.5, dist);

        finalColor = vec4(vColor.rgb, vColor.a * opacity);
    }
    else {
        // ---------- Normal sprite texturing ----------
        finalColor = texture(uTexture, vUV) * vColor;

    }

    FragColor = finalColor;
}