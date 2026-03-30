#version 330 core
in vec2  vTexCoord;
flat in float vTileLayer;

out vec4 FragColor;

uniform sampler2DArray uTexture;
// Optional: Tint/Brightness (default = weiß/voll)
uniform vec4 uColor;

void main()
{
    vec4 texColor = texture(uTexture, vec3(vTexCoord, vTileLayer));
    // Transparente Pixel wegwerfen (Glas-Rand etc. bleibt erhalten)
    if (texColor.a < 0.05) discard;
    FragColor = texColor * uColor;
}
