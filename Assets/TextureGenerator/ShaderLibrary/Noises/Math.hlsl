#ifndef YU_MATH_H
#define YU_MATH_H

//https://www.shadertoy.com/view/4djSRW  不使用三角函数实现的Hash
#define ITERATIONS 4
// *** Change these to suit your range of random numbers..
// *** Use this for integer stepped ranges, ie Value-Noise/Perlin Noise functions.
#define HASHSCALE1 .1031
#define HASHSCALE3 float3(.1031, .1030, .0973)
#define HASHSCALE4 float4(.1031, .1030, .0973, .1099)
//  1 out, 1 in...
float Hash11(float p)
{
    float3 p3 = frac(p.xxx * HASHSCALE1);
    p3 += dot(p3, p3.yzx + 31.32);
    return frac((p3.x + p3.y) * p3.z);
}
//  1 out, 2 in...
float Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * HASHSCALE1);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}
//  1 out, 3 in...
float Hash13(float3 p3)
{
    p3 = frac(p3 * HASHSCALE1);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}
//  2 out, 1 in...
float2 Hash21(float p)
{
    float3 p3 = frac(p * HASHSCALE3);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);

}
///  2 out, 2 in...
float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * HASHSCALE3);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}
///  2 out, 3 in...
float2 Hash23(float3 p3)
{
    p3 = frac(p3 * HASHSCALE3);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}
//  3 out, 1 in...
float3 Hash31(float p)
{
    float3 p3 = frac(p * HASHSCALE3);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xxy + p3.yzz) * p3.zyx);
}
///  3 out, 2 in...
float3 Hash32(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * HASHSCALE3);
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yzz) * p3.zyx);
}
///  3 out, 3 in...
float3 Hash33(float3 p3)
{
    p3 = frac(p3 * HASHSCALE3);
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yxx) * p3.zyx);

}
// 4 out, 1 in...
float4 Hash41(float p)
{
    float4 p4 = frac(p * HASHSCALE4);
    p4 += dot(p4, p4.wzxy + 33.33);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
    
}
// 4 out, 2 in...
float4 Hash42(float2 p)
{
    float4 p4 = frac(float4(p.xyxy) * HASHSCALE4);
    p4 += dot(p4, p4.wzxy + 33.33);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);

}
// 4 out, 3 in...
float4 Hash43(float3 p)
{
    float4 p4 = frac(float4(p.xyzx) * HASHSCALE4);
    p4 += dot(p4, p4.wzxy + 33.33);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
}
// 4 out, 4 in...
float4 Hash44(float4 p4)
{
    p4 = frac(p4 * HASHSCALE4);
    p4 += dot(p4, p4.wzxy + 33.33);
    return frac((p4.xxyz + p4.yzzw) * p4.zywx);
}

//数学函数
//use for FBMR
#define _m1 1.0
#define _m2 (float2x2(0.8,-0.6,0.6,0.8))
#define _m3 (float3x3( 0.00,  0.80,  0.60, -0.80,  0.36, -0.48, -0.60, -0.48,  0.64 ))
//归一化(不太懂原理)

float2 mod289(float2 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}
float3 mod289(float3 x)
{
    return x - floor(x * (1.0 / 289.0)) * 289.0;
}
float3 permute(float3 x)
{
    return mod289(((x * 34.0) + 10.0) * x);
}

//简化dot
float dot2(float2 v)
{
    return dot(v, v);
};
float dot2(float3 v)
{
    return dot(v, v);
};
float ndot(float2 a, float2 b)
{
    return a.x * b.x - a.y * b.y;
};

//在三个值间进行插值, value1 -> value2 -> value3， offset用于中间值(value2)的偏移
float Interpolation3(float v1, float v2, float v3, float x, float o = 0.5)
{
    o = clamp(o, 0.0001, 0.9999);
    return lerp(lerp(v1, v2, min(x, o) / o), v3, max(0, x - o) / (1.0 - o));
}
//在三个值间进行插值, value1 -> value2 -> value3， offset用于中间值(value2)的偏移
float3 Interpolation3(float3 v1, float3 v2, float3 v3, float x, float o = 0.5)
{
    o = clamp(o, 0.0001, 0.9999);
    return lerp(lerp(v1, v2, min(x, o) / o), v3, max(0, x - o) / (1.0 - o));
}

float length2(float2 p)
{
    return sqrt(p.x * p.x + p.y * p.y);
}
float length6(float2 p)
{
    p = p * p * p;
    p = p * p;
    return pow(p.x + p.y, 1.0 / 6.0);
}
float length8(float2 p)
{
    p = p * p;
    p = p * p;
    p = p * p;
    return pow(p.x + p.y, 1.0 / 8.0);
}


//重映射
float Remap(float o, float o_min, float o_max, float n_min, float n_max)
{
    return n_min + ((o - o_min) / (o_max - o_min)) * (n_max - n_min);
}
//设置范围
float setRange(float v, float l, float h)
{
    return saturate((v - l) / (h - l));
}
float3 setRangesSigned(float3 v, float l, float h)
{
    return (v - l) / (h - l);
}
#endif