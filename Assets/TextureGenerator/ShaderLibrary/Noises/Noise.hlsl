#ifndef YU_NOISE_H
#define YU_NOISE_H
#if defined(_PERLIN_NOISE) 
#define Noise perlin_noise
#elif defined(_VALUE_NOISE)
#define Noise value_noise 
#elif defined(_SIMPLEX_NOISE) 
#define Noise simplex_noise
#elif defined(_WORLEY_NOISE) 
#define Noise worley_noise
#else 
#define Noise perlin_noise
#endif 
#include "Math.hlsl"
#include "FBM_Macro.hlsl"

//Gradient Noise by InigoQuilez - iq/2013(使用的时候需要归一化（*0.5+0.5）)
// https://www.shadertoy.com/view/XdXGW8
//基于晶格的噪声最好将Hash映射到[-1，1]，不然的话产生的向量方向太单一
//对于value noise则无所谓，因为产生的本来就只是一个值，但是为了统一也做了映射处理
//但是同样的，在输出值的时候范围为[-1，1] 要把结果归一化到[0,1]
float grad12(float2 p)
{
    return Hash12(p) * 2. - 1;
}
float2 grad22(float2 p)
{
    return Hash22(p) * 2. - 1.;
}
float3 grad33(float3 p)
{
    return Hash33(p) * 2. - 1;
}

float perlin_noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    //smooth interpolation(just like smoothstep)
    //float2 u = f*f*(3.0-2.0*f);
    //Improved version(The 2nd derivative is 0)
    float2 u = f * f * f * (10 + f * (6 * f - 15));
    // Linearly interpolate the values of the four corners
    return lerp(lerp(dot(grad22(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                     dot(grad22(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                lerp(dot(grad22(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                     dot(grad22(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
}
float perlin_noise(float3 p)
{   
    float3 i = floor(p);
    float3 f = frac(p);
    //smooth interpolation(just like smoothstep)
    //float3 u = f*f*(3.0-2.0*f);
    //Improved version(The 2nd derivative is 0)
    float3 u = f * f * f * (10 + f * (6 * f - 15));

    return lerp(lerp(lerp(dot(grad33(i + float3(0.0, 0.0, 0.0)), f - float3(0.0, 0.0, 0.0)),
                          dot(grad33(i + float3(1.0, 0.0, 0.0)), f - float3(1.0, 0.0, 0.0)), u.x),
                     lerp(dot(grad33(i + float3(0.0, 1.0, 0.0)), f - float3(0.0, 1.0, 0.0)),
                          dot(grad33(i + float3(1.0, 1.0, 0.0)), f - float3(1.0, 1.0, 0.0)), u.x), u.y),
                lerp(lerp(dot(grad33(i + float3(0.0, 0.0, 1.0)), f - float3(0.0, 0.0, 1.0)),
                          dot(grad33(i + float3(1.0, 0.0, 1.0)), f - float3(1.0, 0.0, 1.0)), u.x),
                     lerp(dot(grad33(i + float3(0.0, 1.0, 1.0)), f - float3(0.0, 1.0, 1.0)),
                          dot(grad33(i + float3(1.0, 1.0, 1.0)), f - float3(1.0, 1.0, 1.0)), u.x), u.y), u.z);
}
//有一部分是抄的，一部分是自己看着差不多写的
float perlin_noise_tile(float2 x, float freq)
{
    x *= freq;
    
    float2 i = floor(x);
    float2 f = frac(x);
    //smooth interpolation(just like smoothstep)
    //float2 u = f*f*(3.0-2.0*f);
    //Improved version(The 2nd derivative is 0)
    float2 u = f * f * f * (10 + f * (6 * f - 15));
    // Linearly interpolate the values of the four corners
    return lerp(lerp(dot(grad22(fmod(i + float2(0.0, 0.0), freq)), f - float2(0.0, 0.0)),
                     dot(grad22(fmod(i + float2(1.0, 0.0), freq)), f - float2(1.0, 0.0)), u.x),
                lerp(dot(grad22(fmod(i + float2(0.0, 1.0), freq)), f - float2(0.0, 1.0)),
                     dot(grad22(fmod(i + float2(1.0, 1.0), freq)), f - float2(1.0, 1.0)), u.x), u.y);
}
float perlin_noise_tile(float3 x, float freq)
{
    x *= freq;
    // grid
    float3 p = floor(x);
    float3 w = frac(x);
    
    // quintic interpolant
    float3 u = w * w * w * (w * (w * 6. - 15.) + 10.);

    // gradients
    float3 ga = grad33(fmod(p + float3(0., 0., 0.), freq));
    float3 gb = grad33(fmod(p + float3(1., 0., 0.), freq));
    float3 gc = grad33(fmod(p + float3(0., 1., 0.), freq));
    float3 gd = grad33(fmod(p + float3(1., 1., 0.), freq));
    float3 ge = grad33(fmod(p + float3(0., 0., 1.), freq));
    float3 gf = grad33(fmod(p + float3(1., 0., 1.), freq));
    float3 gg = grad33(fmod(p + float3(0., 1., 1.), freq));
    float3 gh = grad33(fmod(p + float3(1., 1., 1.), freq));
    
    // projections
    float va = dot(ga, w - float3(0., 0., 0.));
    float vb = dot(gb, w - float3(1., 0., 0.));
    float vc = dot(gc, w - float3(0., 1., 0.));
    float vd = dot(gd, w - float3(1., 1., 0.));
    float ve = dot(ge, w - float3(0., 0., 1.));
    float vf = dot(gf, w - float3(1., 0., 1.));
    float vg = dot(gg, w - float3(0., 1., 1.));
    float vh = dot(gh, w - float3(1., 1., 1.));
	
    // interpolation
    return va +
           u.x * (vb - va) +
           u.y * (vc - va) +
           u.z * (ve - va) +
           u.x * u.y * (va - vb - vc + vd) +
           u.y * u.z * (va - vc - ve + vg) +
           u.z * u.x * (va - vb - ve + vf) +
           u.x * u.y * u.z * (-va + vb + vc - vd + ve - vf - vg + vh);
}
//带导数的perlin_noise
float3 perlin_noise_D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // quintic interpolation五次插值
    // 若能使多阶导数在插值点上为0，可以进一步平滑
    // 五次Hermit插值函数：S(x) = 6 x^5 - 15 x^4 + 10 x^3	
    float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);
    float2 du = 30.0 * f * f * (f * (f - 2.0) + 1.0);
    
    //random gradients
    float2 ga = grad22(i + float2(0.0, 0.0));
    float2 gb = grad22(i + float2(1.0, 0.0));
    float2 gc = grad22(i + float2(0.0, 1.0));
    float2 gd = grad22(i + float2(1.0, 1.0));
    
    //random values by random gradients
    float va = dot(ga, f - float2(0.0, 0.0));
    float vb = dot(gb, f - float2(1.0, 0.0));
    float vc = dot(gc, f - float2(0.0, 1.0));
    float vd = dot(gd, f - float2(1.0, 1.0));

    //即mix(mix(va, vb, u.x), mix(vc, vd, u.x), u.y);
    //与2to1的Perlin Noise除了S曲线外结果基本一致
    float value = va + u.x * (vb - va) + u.y * (vc - va) + u.x * u.y * (va - vb - vc + vd);
    
    //mix(mix(ga, gb, u.x), mix(gc, gd, u.x), u.y);
    float2 derivatives = ga + u.x * (gb - ga) + u.y * (gc - ga) + u.x * u.y * (ga - gb - gc + gd)
        + du * (u.yx * (va - vb - vc + vd) + float2(vb - va, vc - va));
        
    return float3(value, derivatives);
}
//value_noise 
//easier than perlin_noise 
float value_noise(float2 p)
{
    float2 pi = floor(p);
    float2 pf = frac(p);
    
    float2 w = pf * pf * (3.0 - 2.0 * pf);
    
    return lerp(lerp(grad12(pi + float2(0.0, 0.0)), grad12(pi + float2(1.0, 0.0)), w.x),
               lerp(grad12(pi + float2(0.0, 1.0)), grad12(pi + float2(1.0, 1.0)), w.x),
               w.y);
}
float value_noise(float3 p)
{
    float3 pi = floor(p);
    float3 pf = p - pi;
    
    float3 w = pf * pf * (3.0 - 2.0 * pf);
    
    return lerp(
                lerp(
                    lerp(Hash13(pi + float3(0, 0, 0)), Hash13(pi + float3(1, 0, 0)), w.x),
                    lerp(Hash13(pi + float3(0, 0, 1)), Hash13(pi + float3(1, 0, 1)), w.x),
                    w.z),
                lerp(
                    lerp(Hash13(pi + float3(0, 1, 0)), Hash13(pi + float3(1, 1, 0)), w.x),
                    lerp(Hash13(pi + float3(0, 1, 1)), Hash13(pi + float3(1, 1, 1)), w.x),
                    w.z),
                w.y);
}
//simplex_noise, copy from shadertoy 
//faster than perlin_noise 
float simplex_noise(float2 p)
{
    const float K1 = 0.366025404; // (sqrt(3)-1)/2;
    const float K2 = 0.211324865; // (3-sqrt(3))/6;
    
    float2 i = floor(p + (p.x + p.y) * K1);
    
    //求出p点到各个单形顶点的距离向量
    float2 a = p - (i - (i.x + i.y) * K2);
    float2 o = (a.x < a.y) ? float2(0.0, 1.0) : float2(1.0, 0.0);
    float2 b = a - (o - K2);
    float2 c = a - (1.0 - 2.0 * K2);
    
    //(r2−|dist|2)4×dot(dist,grad)：每个顶点的贡献度
    float3 h = max(0.5 - float3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
    float3 n = h * h * h * h * float3(dot(a, grad22(i)), dot(b, grad22(i + o)), dot(c, grad22(i + 1.0)));
    
    return dot(float3(70.0, 70.0, 70.0), n); //1/3⁴*1/√6*√2*2≈1/70 用于归一化
}
float simplex_noise(float3 p)
{
    const float K1 = 0.333333333;
    const float K2 = 0.166666667;
    
    float3 i = floor(p + (p.x + p.y + p.z) * K1);
    float3 d0 = p - (i - (i.x + i.y + i.z) * K2);
    
    // thx nikita: https://www.shadertoy.com/view/XsX3zB
    float3 e = step(float3(0.0, 0.0, 0.0), d0 - d0.yzx);
    float3 i1 = e * (1.0 - e.zxy);
    float3 i2 = 1.0 - e.zxy * (1.0 - e);
    
    float3 d1 = d0 - (i1 - 1.0 * K2);
    float3 d2 = d0 - (i2 - 2.0 * K2);
    float3 d3 = d0 - (1.0 - 3.0 * K2);
    
    float4 h = max(0.6 - float4(dot(d0, d0), dot(d1, d1), dot(d2, d2), dot(d3, d3)), 0.0);
    float4 n = h * h * h * h * float4(dot(d0, grad33(i)), dot(d1, grad33(i + i1)), dot(d2, grad33(i + i2)), dot(d3, grad33(i + 1.0)));
    
    return dot(float4(31.316, 31.316, 31.316, 31.316), n);
}
//Voronoi Worley 
//cost too much 
// use loop
float worley_noise(float2 p)
{
    float2 n = floor(p);
    float2 f = frac(p);

    float m = 8.0;
    for (int j = -1; j <= 1; j++)
        for (int i = -1; i <= 1; i++)
        {
            float2 g = float2(i, j);
            float2 h = Hash22(n + g);
            h += g;
            float2 r = h - f;

            // distance to cell		
            float d = length(r);
            
            // // do the smooth min for distances		
            //float h = smoothstep(-1.0, 1.0, (m - d) / SmoothFactor);
            //m = lerp(m, d, h) - h * (1.0 - h) * SmoothFactor / (1 + 3.0 * SmoothFactor); 
            
            m = min(m, d);
        }
	
     // inverted worley noise
    return 1.- m;
}
float worley_noise(float3 p)
{
    float3 n = floor(p);
    float3 f = frac(p);
    
    float m = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for (float y = -1.; y <= 1.; ++y)
        {
            for (float z = -1.; z <= 1.; ++z)
            {
                float3 g = float3(x, y, z);
                float3 h = Hash33(n + g);
                h += g;
                float3 r = h - f;
                float d = length(r);
                
                m = min(m, d);
            }
        }
    }
    
    // inverted worley noise
    return 1. - m;
}
//Tileable 3DNoises(抄的shaderToy加上自己乱写的)
//https://www.shadertoy.com/view/3dVXDc
float worley_noise_tile(float2 uv, float freq)
{
    uv *= freq;
    
    float2 id = floor(uv);
    float2 p = frac(uv);
    
    float minDist = 10000.f;
    for (float x = -1.; x <= 1.; ++x)
    {
        for (float y = -1.; y <= 1.; ++y)
        {
            float2 offset = float2(x, y);
            float2 h = Hash22(fmod(id + offset, freq));
            h += offset;
            float2 d = p - h;
            minDist = min(minDist, dot(d, d));
        }
    }
    
    // inverted worley noise
    return 1. - minDist;
}
float worley_noise_tile(float3 uv, float freq)
{
    uv *= freq;
    
    float3 id = floor(uv);
    float3 p = frac(uv);
    
    float minDist = 10000.;
    for (float x = -1.; x <= 1.; ++x)
    {
        for (float y = -1.; y <= 1.; ++y)
        {
            for (float z = -1.; z <= 1.; ++z)
            {
                float3 offset = float3(x, y, z);
                float3 h = Hash33(fmod(id + offset, freq));
                h += offset;
                float3 d = p - h;
                minDist = min(minDist, dot(d, d));
            }
        }
    }
    
    // inverted worley noise
    return 1. - minDist;
}
//3D version please ref to https://www.shadertoy.com/view/ldl3Dl
//to do

//定义常见的噪声的普通FBM函数 float(float2)和float（float3）版本
_DEFAULT_FBM()
_DEFAULT_FBMR()

__FBM(perlin_noise)
__FBM(value_noise)
__FBM(simplex_noise)
__FBM(worley_noise)
__FBMR(perlin_noise)
__FBMR(value_noise)
__FBMR(simplex_noise)
__FBMR(worley_noise)

__FBMT(perlin_noise_tile)
__FBMT(worley_noise_tile)

//湍流效果fbm
float turbulence(float2 p, float iterNum)
{
    float value = 0.0;
    float amplitude = .5;
    float frequency = 0.;
    //
    // Loop of octaves
    for (int i = 0; i < iterNum; i++)
    {
        value += amplitude * abs(perlin_noise(p));
        p *= 2.;
        amplitude *= .5;
    }
    return value;
}

//山脊效果fbm
float ridge(float h, float offset)
{
    h = abs(h); // create creases
    h = offset - h; // invert so creases are at top
    h = h * h; // sharpen creases
    return h;
}
float ridged(float2 p, float iterNum)
{
    float offset = 0.9;

    float sum = 0.0;
    float freq = 1.0, amp = 0.5;
    float prev = 1.0;
    for (int i = 0; i < iterNum; i++)
    {
        float n = ridge(perlin_noise(p * freq), offset);
        sum += n * amp;
        sum += n * amp * prev;
        prev = n;

        freq *= 2;
        amp *= .5;
    }

    return sum;
}

//CurlNoise
float3 curlNoise(float3 p)
{
    const float e = 0.1;

    float n1 = simplex_noise(float3(p.x, p.y + e, p.z));
    float n2 = simplex_noise(float3(p.x, p.y - e, p.z));
    float n3 = simplex_noise(float3(p.x, p.y, p.z + e));
    float n4 = simplex_noise(float3(p.x, p.y, p.z - e));
    float n5 = simplex_noise(float3(p.x + e, p.y, p.z));
    float n6 = simplex_noise(float3(p.x - e, p.y, p.z));

    float x = n2 - n1 - n4 + n3;
    float y = n4 - n3 - n6 + n5;
    float z = n6 - n5 - n2 + n1;


    const float divisor = 1.0 / (2.0 * e);
    return normalize(float3(x, y, z) * divisor);
}
#endif