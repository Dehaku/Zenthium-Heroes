
float2 convertToSliceUV(float3 uv, float width, float height)
{
    float2 texUV = abs(uv.xy);
            

    float2 index = uv.z;

    float texturesizeX = 1 / width;
    float texturesizeY = 1 / height;

    float2 uv1slice = float2(texUV.x % texturesizeX, texUV.y % texturesizeY);
           
    uv1slice.x += texturesizeX * (int) (index % width);
    uv1slice.y += texturesizeY * ((int) (index / width) % height);
    return uv1slice;
}

float4 _GetFromTextureAtlas(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf, float atlaswidth, float atlasheight)
{
    float4 o1 = tex2D(tex, convertToSliceUV(uvxy1, atlaswidth, atlasheight));
    float4 result1 = (o1) * bf.z;
    o1 = tex2D(tex, convertToSliceUV(uvxz1, atlaswidth, atlasheight));
    float4 result2 = (o1) * bf.y;
    o1 = tex2D(tex, convertToSliceUV(uvyz1, atlaswidth, atlasheight));
    float4 result3 = (o1) * bf.x;
    return result1 + result2 + result3;
}

float4 _GetFromTextureAtlas(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf)
{
    float4 o1 = tex2D(tex, uvxy1.xy);
    float4 result1 = (o1) * bf.z;
    o1 = tex2D(tex, uvxz1.xy);
    float4 result2 = (o1) * bf.y;
    o1 = tex2D(tex, uvyz1.xy);
    float4 result3 = (o1) * bf.x;
    return result1 + result2 + result3;
}

float4 _GetFromTextureAtlas_LOD(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf, float atlaswidth, float atlasheight)
{
    float4 o1 = tex2Dlod(tex, float4(convertToSliceUV(uvxy1, atlaswidth, atlasheight), 0, 0));
    float4 result1 = (o1) * bf.z;
    o1 = tex2Dlod(tex, float4(convertToSliceUV(uvxz1, atlaswidth, atlasheight), 0, 0));
    float4 result2 = (o1) * bf.y;
    o1 = tex2Dlod(tex, float4(convertToSliceUV(uvyz1, atlaswidth, atlasheight), 0, 0));
    float4 result3 = (o1) * bf.x;
    return result1 + result2 + result3;
}

float4 _GetFromTextureAtlas_LOD(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf)
{
    float4 o1 = tex2Dlod(tex, float4(uvxy1.xy, 0, 0));
    float4 result1 = (o1) * bf.z;
    o1 = tex2Dlod(tex, float4(uvxz1.xy, 0, 0));
    float4 result2 = (o1) * bf.y;
    o1 = tex2Dlod(tex, float4(uvyz1.xy, 0, 0));
    float4 result3 = (o1) * bf.x;
    return result1 + result2 + result3;
}

void _GetFromTextureAtlas_RESULTSONLY(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf, float atlaswidth, float atlasheight, out float4 result1, out float4 result2, out float4 result3)
{
    float4 o1 = tex2D(tex, convertToSliceUV(uvxy1, atlaswidth, atlasheight));
    result3 = (o1) * bf.z;
    o1 = tex2D(tex, convertToSliceUV(uvxz1, atlaswidth, atlasheight));
    result2 = (o1) * bf.y;
    o1 = tex2D(tex, convertToSliceUV(uvyz1, atlaswidth, atlasheight));
    result1 = (o1) * bf.x;
}

void _GetFromTextureAtlas_RESULTSONLY(sampler2D tex, float3 uvxy1, float3 uvxz1, float3 uvyz1, float3 bf, out float4 result1, out float4 result2, out float4 result3)
{
    float4 o1 = tex2D(tex, uvxy1.xy);
    result3 = (o1) * bf.z;
    o1 = tex2D(tex, uvxz1.xy);
    result2 = (o1) * bf.y;
    o1 = tex2D(tex, uvyz1.xy);
    result1 = (o1) * bf.x;
}