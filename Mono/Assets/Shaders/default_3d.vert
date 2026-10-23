#version 410 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aUV;
layout(location = 3) in vec4 aTangent;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

out VS_OUT {
    vec3 FragPos;
    vec2 UV;
    mat3 TBN;
} vs_out;

void main()
{
    mat3 normalMatrix = transpose(inverse(mat3(model)));

    vec3 N = normalize(normalMatrix * aNormal);
    vec3 T = normalize(normalMatrix * aTangent.xyz);

    // Make tangent perpendicular to normal
    T = normalize(T - N * dot(N, T));

    vec3 B = cross(N, T) * aTangent.w;

    vs_out.TBN = mat3(T, B, N);
    vs_out.UV = aUV;

    vec4 worldPos = model * vec4(aPosition, 1.0);
    vs_out.FragPos = worldPos.xyz;

    gl_Position = projection * view * worldPos;
}