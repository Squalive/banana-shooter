void BluredColor_float(float4 Seed,float Min,float Max,float BlurX,float BlurY,out float4 Out)
{
    float randomno = frac(sin(dot(Seed.xy,float2(12.9898,78.233))) * 43758.5453);

    float noise = lerp(Min,Max,randomno);
    float uvx = float(sin(noise)) * BlurX;
    float uvy = float(cos(noise)) * BlurY;

    float4 uvpos = float4(Seed.x + uvx, Seed.y+uvy,Seed.zw);
    Out = uvpos;
}