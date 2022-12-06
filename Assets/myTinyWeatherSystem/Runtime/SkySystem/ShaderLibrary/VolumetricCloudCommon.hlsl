#ifndef YU_VOLUMETRICCLOUD_COMMON_INCLUDE
#define YU_VOLUMETRICCLOUD_COMMON_INCLUDE
#include "SkyAtmosphereCommon.hlsl"

TEXTURE2D_FLOAT(_CloudMapTexture);
TEXTURE2D_FLOAT(_CloudLutTexture);
TEXTURE3D_FLOAT(_BaseNoise);
TEXTURE3D_FLOAT(_ErosionNoise);
SAMPLER(s_trilinear_repeat_sampler);

void EvaluateCloudProperties()
{
    
    
}

#endif