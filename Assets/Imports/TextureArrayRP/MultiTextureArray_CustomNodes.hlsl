


void TextureArrayBlend_float(UnityTexture2DArray Texture, float Slice, float2 UV, UnitySamplerState Sampler, float2 UV3, float UV3Power, out float4 Out, out float R, out float G, out float B, out float A)
{
    float slice = Slice + UV3.x * UV3Power;
    slice = max(0, slice);
    int textureindex = slice;

    float blendfactor = slice % 1;

    float4 c1 = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, UV, textureindex) * (1 - blendfactor);
    float4 c2 = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, UV, textureindex + 1) * (blendfactor);
    Out = c1 + c2;
    R = Out.x;
    G = Out.y;
    B = Out.z;
    A = Out.w;
}



void TextureArrayBlend_triplanar_float(UnityTexture2DArray Texture, float Slice, float3 position, float3 normal, UnitySamplerState Sampler, float2 UV3, float UV3Power, out float4 Out, out float R, out float G, out float B, out float A)
{  
    float slice = Slice + UV3.x * UV3Power;
    slice = max(0, slice);
    int textureindex = slice;

    float blendfactor = slice % 1;

    float4 x =  SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, position.yz, textureindex) *normal.x;
    float4 y = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, position.xz, textureindex) * normal.y;
    float4 z = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, position.xy, textureindex) * normal.z;
   
    
    float4 c1 = (x + y + z) * (1 - blendfactor);
    
    x = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, position.yz, textureindex + 1) * normal.x;
    y = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, position.xz, textureindex + 1) * normal.y;
    z = SAMPLE_TEXTURE2D_ARRAY(Texture, Sampler, position.xy, textureindex + 1) * normal.z;
     
    float4 c2 = (x + y + z) * (blendfactor);
    Out = c1 + c2;
    R = Out.x;
    G = Out.y;
    B = Out.z;
    A = Out.w;
}