using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Yu_Weather
{
    public class ViewSkyInfo
    {
        const float mToSkyUnit = 0.001f;			// meters to Kilometers
        const float skyUnitToM = 1.0f / 0.001f;

        //Camera
        public RenderTargetIdentifier cameraColorTarget;
        public CameraData cameraData;
        public bool IsPlanarReflection;

        public Vector3 ViewOrigin;        //如果不是reflection的话 PreViewTranslation = -ViewOrigin = -CameraWorldPos
        public Vector3 PreViewTranslation;
        public Matrix4x4 TranslatedViewMatrix;        //去除了相机位移（？）之后的VP矩阵
        public Matrix4x4 InvTranslatedViewMatrix;
        public Matrix4x4 TranslatedViewProjectionMatrix;
        public Matrix4x4 InvTranslatedViewProjectionMatrix;
        public Vector3 skyCameraTranslatedPos;//相机的TranslatedWorld坐标 当相机位于水平线以下时 将会把他拉到水平线之上 
        public Vector4 skyTranslatedPlanetCenterAndViewHeight;        //TranslatedWorld状态下的地心坐标和相机距离中心的高度
        public Matrix4x4 skyViewLutReferential;        //skyViewLut的Referential
        public Vector4 ScreenRect;

        public float BottomRadiusKm;
        public Vector3 PlanetCenterKm;

        //skyAtmosphere
        public float AerialPerspectiveStartDepthKm;
        public float AerialPespectiveViewDistanceScale;

        public RenderTexture skyViewLut;
        public RenderTexture aerialPerspectiveLut;

        public void Update(ref CameraData data,SkyAtmosphereController skyAC)
        {
            cameraData = data;

            cameraColorTarget = cameraData.renderer.cameraColorTarget;
            if (cameraData.camera.name == "Planar Camera")
                IsPlanarReflection = true;
            else
                IsPlanarReflection = false;

            ViewOrigin = data.worldSpaceCameraPos;
            PreViewTranslation = -ViewOrigin;

            TranslatedViewMatrix = data.GetViewMatrix() * Matrix4x4.Translate(ViewOrigin);
            //不然不对
            if (IsPlanarReflection)
            {
                TranslatedViewMatrix.SetColumn(1,- TranslatedViewMatrix.GetColumn(1));
            }

            InvTranslatedViewMatrix = TranslatedViewMatrix.inverse;

            TranslatedViewProjectionMatrix = data.GetGPUProjectionMatrix() * TranslatedViewMatrix;
            InvTranslatedViewProjectionMatrix = InvTranslatedViewMatrix * data.GetGPUProjectionMatrix().inverse;

            ScreenRect = new Vector4((float)cameraData.camera.pixelRect.width, (float)cameraData.camera.pixelRect.height);

            //skyAtmosphere
            if(skyAC != null)
            {
                BottomRadiusKm = skyAC.BottomRadiusKm;
                PlanetCenterKm = new Vector3(0, -BottomRadiusKm, 0);

                AerialPerspectiveStartDepthKm = Mathf.Max(skyAC.AerialPerspectiveStartDepth, cameraData.camera.nearClipPlane * mToSkyUnit);
                AerialPespectiveViewDistanceScale = skyAC.AerialPespectiveViewDistanceScale;
            }
            else
            {
                BottomRadiusKm = 6360.0f;
                PlanetCenterKm = new Vector3(0, -BottomRadiusKm, 0);

                AerialPerspectiveStartDepthKm = Mathf.Max(0.1f, cameraData.camera.nearClipPlane * mToSkyUnit);
                AerialPespectiveViewDistanceScale = 1;
            }

            ComputeViewData(ViewOrigin, PreViewTranslation, data.camera.transform.forward, cameraData.camera.transform.right,
               out skyCameraTranslatedPos, out skyTranslatedPlanetCenterAndViewHeight, out skyViewLutReferential);

            SetViewGlobalVariables();
        }

        public void SetViewGlobalVariables()
        {
            Shader.SetGlobalMatrix(_Translated_V_Matrix, TranslatedViewMatrix);
            Shader.SetGlobalMatrix(_Inv_Translated_V_Matrix, InvTranslatedViewMatrix);
            Shader.SetGlobalMatrix(_Translated_VP_Matrix, TranslatedViewProjectionMatrix);
            Shader.SetGlobalMatrix(_Inv_Translated_VP_Matrix, InvTranslatedViewProjectionMatrix);

            Shader.SetGlobalVector(_TranslatedWorldCameraOrigin, ViewOrigin + PreViewTranslation);
            Shader.SetGlobalMatrix(_SkyViewLutReferential, skyViewLutReferential);
            Shader.SetGlobalVector(_SkyCameraTranslatedWorldOrigin, skyCameraTranslatedPos);
            Shader.SetGlobalVector(_SkyPlanetTranslatedWorldCenterAndViewHeight, skyTranslatedPlanetCenterAndViewHeight);
            Shader.SetGlobalVector(_ScreenRect, ScreenRect);
        }

        //Translated World：去除了WorldToView的平移变换的世界空间（在这里可以简化，但是能抄代码谁不抄呢）
        public void ComputeViewData
            (Vector3 WorldCameraOrigin, Vector3 PreViewTranslation, Vector3 ViewForward, Vector3 ViewRight,
           out Vector3 SkyCameraTranslatedWorldOriginTranslatedWorld, out Vector4 SkyPlanetTranslatedWorldCenterAndViewHeight, out Matrix4x4 SkyViewLutReferential)
        {
            const float PlanetRadiusOffset = 0.001f;//KM

            const float Offset = PlanetRadiusOffset * skyUnitToM;
            float BottomRadiusWorld = BottomRadiusKm * skyUnitToM;
            Vector3 PlanetCenterWorld = PlanetCenterKm * skyUnitToM;
            //转换到Translated World
            Vector3 PlanetCenterTranslatedWorld = PlanetCenterWorld + PreViewTranslation;
            Vector3 WorldCameraOriginTranslatedWorld = WorldCameraOrigin + PreViewTranslation;
            Vector3 PlanetCenterToCameraTranslatedWorld = WorldCameraOriginTranslatedWorld - PlanetCenterTranslatedWorld;//cameraWorldPos - PlanetCenterWorld
            float DistanceToPlanetCenterTranslatedWorld = PlanetCenterToCameraTranslatedWorld.magnitude;

            // 如果相机在地平线下方，把他移到上方来。
            SkyCameraTranslatedWorldOriginTranslatedWorld =
                DistanceToPlanetCenterTranslatedWorld < (BottomRadiusWorld + Offset) ?
                PlanetCenterTranslatedWorld + (BottomRadiusWorld + Offset) * (PlanetCenterToCameraTranslatedWorld / DistanceToPlanetCenterTranslatedWorld)
                : WorldCameraOriginTranslatedWorld;

            SkyPlanetTranslatedWorldCenterAndViewHeight =
                new Vector4(PlanetCenterTranslatedWorld.x, PlanetCenterTranslatedWorld.y, PlanetCenterTranslatedWorld.z, (SkyCameraTranslatedWorldOriginTranslatedWorld - PlanetCenterTranslatedWorld).magnitude);

            //referential for the SkyView LUT
            SkyViewLutReferential = Matrix4x4.identity;
            Vector3 PlanetCenterToWorldCameraPos = (SkyCameraTranslatedWorldOriginTranslatedWorld - PlanetCenterTranslatedWorld) * mToSkyUnit;
            Vector3 Up = PlanetCenterToWorldCameraPos;
            Up.Normalize();
            Vector3 Forward = ViewForward;
            Vector3 Left = Vector3.Cross(Forward, Up);
            Left.Normalize();
            float DotMainDir = Mathf.Abs(Vector3.Dot(Up, Forward));
            //x:Forward y:Up -z:Left in Unity
            //x:Forward z:Up y:Left in UE
            if (DotMainDir > 0.999f)
            {
                // When it becomes hard to generate a referential, generate it procedurally.
                // [ Duff et al. 2017, "Building an Orthonormal Basis, Revisited" ]
                float Sign = Up.z >= 0.0f ? 1.0f : -1.0f;
                float a = -1.0f / (Sign + Up.z);
                float b = Up.x * Up.y * a;
                Forward = new Vector3(1 + Sign * a * Mathf.Pow(Up.x, 2.0f), Sign * b, -Sign * Up.x);
                Left = new Vector3(b, Sign + a * Mathf.Pow(Up.y, 2.0f), -Up.y);

                SkyViewLutReferential.SetColumn(0, Forward);
                SkyViewLutReferential.SetColumn(1, Up);
                SkyViewLutReferential.SetColumn(2, -Left);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
            else
            {
                // This is better as it should be more stable with respect to camera forward.
                Forward = Vector3.Cross(Up, Left);
                Forward.Normalize();
                SkyViewLutReferential.SetColumn(0, Forward);
                SkyViewLutReferential.SetColumn(1, Up);
                SkyViewLutReferential.SetColumn(2, -Left);
                SkyViewLutReferential = SkyViewLutReferential.transpose;
            }
        }

        public void Release()
        {
            if (skyViewLut)
                RenderTexture.ReleaseTemporary(skyViewLut);
            if (aerialPerspectiveLut)
                RenderTexture.ReleaseTemporary(aerialPerspectiveLut);
        }

        //PropertyToID
        public readonly int _SkyViewTexture = Shader.PropertyToID("_SkyViewTexture");
        public readonly int _AerialPerspectiveTexture = Shader.PropertyToID("_AerialPerspectiveTexture");

        public readonly int _APStartDepthKm = Shader.PropertyToID("_AerialPerspectiveStartDepthKm");
        public readonly int _AerialPespectiveViewDistanceScale = Shader.PropertyToID("_AerialPespectiveViewDistanceScale");

        public readonly int _Translated_V_Matrix = Shader.PropertyToID("_Translated_V_Matrix");
        public readonly int _Inv_Translated_V_Matrix = Shader.PropertyToID("_Inv_Translated_V_Matrix");
        public readonly int _Translated_VP_Matrix = Shader.PropertyToID("_Translated_VP_Matrix");
        public readonly int _Inv_Translated_VP_Matrix = Shader.PropertyToID("_Inv_Translated_VP_Matrix");
        public readonly int _TranslatedWorldCameraOrigin = Shader.PropertyToID("_TranslatedWorldCameraOrigin");
        public readonly int _SkyViewLutReferential = Shader.PropertyToID("_SkyViewLutReferential");
        public readonly int _SkyCameraTranslatedWorldOrigin = Shader.PropertyToID("_SkyCameraTranslatedWorldOrigin");
        public readonly int _SkyPlanetTranslatedWorldCenterAndViewHeight = Shader.PropertyToID("_SkyPlanetTranslatedWorldCenterAndViewHeight");
        public readonly int _ScreenRect = Shader.PropertyToID("_ScreenRect");
    }
}