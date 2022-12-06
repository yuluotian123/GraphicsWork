#ifndef YU_FBM_MACRO_H
#define YU_FBM_MACRO_H
//宏编程哥们根本不会，所以随便写写
#define FuncHead_COMMON(NOISE_TYPE,FBM_TYPE,outparam,inparam) float##outparam FBM##FBM_TYPE##NOISE_TYPE(float##inparam p,float iterNum)

#define FuncBody_COMMON(NOISE_TYPE,outparam,inparam)\
{\
    float##outparam f = 0.0;\
    float amp = 0.5;\
    float sum = 0.0;\
    for (int i = 0; i < iterNum; i++)\
    {\
        f += amp * NOISE_TYPE(p );\
        p =mul(_m##inparam,p)* 2.;\
        sum += amp;\
        amp *= 0.5;\
    }\
    return f / sum;\
}\


//普通版本FBM
#define FuncHead(NOISE_TYPE,outparam,inparam) FuncHead_COMMON(NOISE_TYPE,,outparam,inparam)
#define FuncBody(NOISE_TYPE,outparam) FuncBody_COMMON(NOISE_TYPE,outparam,1)

#define _EMP_FBM(outparam,inparam) FuncHead(,outparam,inparam) FuncBody(Noise,outparam)
#define _FBM(NOISE_TYPE,outparam,inparam) FuncHead(NOISE_TYPE,outparam,inparam) FuncBody(NOISE_TYPE,outparam)

#define _DEFAULT_FBM() _EMP_FBM(1,2) _EMP_FBM(1,3)
#define __FBM(NOISE_TYPE) _FBM(NOISE_TYPE,1,2) _FBM(NOISE_TYPE,1,3)

//FBMR
#define FuncHeadR(NOISE_TYPE,outparam,inparam) FuncHead_COMMON(NOISE_TYPE,R,outparam,inparam)
#define FuncBodyR(NOISE_TYPE,outparam,inparam) FuncBody_COMMON(NOISE_TYPE,outparam,inparam)

#define _FBMR(NOISE_TYPE,outparam,inparam) FuncHeadR(NOISE_TYPE,outparam,inparam) FuncBodyR(NOISE_TYPE,outparam,inparam)
#define _EMP_FBMR(outparam,inparam) FuncHeadR(,outparam,inparam) FuncBodyR(Noise,outparam,inparam)

#define _DEFAULT_FBMR() _EMP_FBMR(1,2) _EMP_FBMR(1,3)
#define __FBMR(NOISE_TYPE) _FBMR(NOISE_TYPE,1,2) _FBMR(NOISE_TYPE,1,3)

//一个简单的平铺噪声(没有用2n维计算2维这种牛逼办法)
#define FuncHead_TILE(NOISE_TYPE,outparam,inparam,addparam) float##outparam FBM##NOISE_TYPE(float##inparam p,float##addparam freq,float iterNum)
#define FuncBodyT(NOISE_TYPE,outparam,inparam)\
{\
    float##outparam f = 0.;\
    float amp = .5;\
    float sum = 0.0;\
    for (int i = 0; i < iterNum; ++i)\
    {\
        f += amp * NOISE_TYPE(p, freq);\
        freq *= 2.;\
        sum += amp;\
        amp *= 0.5;\
    }\
    return f/sum;\
}\

#define FuncBodyT_4to2(NOISE_TYPE,outparam,inparam)\
{\
}\

#define _FBMT(NOISE_TYPE,outparam,inparam,addparam) FuncHead_TILE(NOISE_TYPE,outparam,inparam,addparam) FuncBodyT(NOISE_TYPE,outparam,inparam)

#ifndef _4DSEAMLESS
#define __FBMT(NOISE_TYPE) _FBMT(NOISE_TYPE,1,2,1) _FBMT(NOISE_TYPE,1,3,1)
#else
#endif

#endif