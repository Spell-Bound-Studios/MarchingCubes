void TriplanarTexture2DArray_float(
Texture2DArray TexArray,
SamplerState TexArraySampler,
float Index,
float3 WorldPosition,
float3 WorldNormal,
float Tiling,
float Sharpness,
out float4 Color)
{
    // Calculate blend weights based on world normal
    float3 blendWeights = abs(WorldNormal);
    blendWeights = pow(blendWeights, Sharpness);
    blendWeights = blendWeights / (blendWeights.x + blendWeights.y + blendWeights.z);
    
    // Calculate UVs for each axis projection
    float2 uvX = WorldPosition.zy * Tiling;
    float2 uvY = WorldPosition.xz * Tiling;
    float2 uvZ = WorldPosition.xy * Tiling;
    
    // Sample texture array for each projection
    float4 texX = TexArray.Sample(TexArraySampler, float3(uvX, Index));
    float4 texY = TexArray.Sample(TexArraySampler, float3(uvY, Index));
    float4 texZ = TexArray.Sample(TexArraySampler, float3(uvZ, Index));
    
    // Blend the three samples
    Color = texX * blendWeights.x + texY * blendWeights.y + texZ * blendWeights.z;
}