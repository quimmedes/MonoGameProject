#if OPENGL
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Matrices
matrix World;
matrix View;
matrix Projection;

// Texture
texture BasicTexture;
sampler BasicTextureSampler = sampler_state
{
    Texture = <BasicTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

// Vertex input structure
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinate : TEXCOORD0;
};

// Vertex output structure
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 Normal : TEXCOORD0;
    float2 TextureCoordinate : TEXCOORD1;
};

// Vertex shader
VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    
    // Apply transformations
    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);
    
    // Pass normal and texture coordinate
    output.Normal = mul(input.Normal, (float3x3)World);
    output.TextureCoordinate = input.TextureCoordinate;
    
    return output;
}

// Pixel shader
float4 MainPS(VertexShaderOutput input) : COLOR
{
    // Sample the texture
    float4 textureColor = tex2D(BasicTextureSampler, input.TextureCoordinate);
    
    // Apply simple lighting
    float3 lightDirection = normalize(float3(0.5, -1, 0.5));
    float3 normal = normalize(input.Normal);
    float lightIntensity = max(0.3, dot(normal, -lightDirection));
    
    // Combine texture and lighting
    float4 finalColor = textureColor * lightIntensity;
    
    return finalColor;
}

// Technique
technique BasicColorDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
