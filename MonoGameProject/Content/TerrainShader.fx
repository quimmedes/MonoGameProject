float4x4 World;
float4x4 View;
float4x4 Projection;

// Single texture for the terrain
texture Texture;

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

// Simple vertex shader input/output
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float3 Normal : TEXCOORD1;
};

// Basic vertex shader
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform position
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    
    // Pass texture coordinates
    output.TexCoord = input.TexCoord * 0.1f;
    
    // Pass normal
    output.Normal = mul(input.Normal, (float3x3)World);
    
    return output;
}

// Minimal pixel shader
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // Sample texture
    float4 textureColor = tex2D(TextureSampler, input.TexCoord);
    
    // Basic lighting
    float3 lightDir = normalize(float3(0.5, -0.5, 0.5));
    float3 normal = normalize(input.Normal);
    float light = max(0.3, dot(normal, lightDir));
    
    // Apply lighting
    float4 color = textureColor * light;
    
    return color;
}

// Technique
technique TerrainShader
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
