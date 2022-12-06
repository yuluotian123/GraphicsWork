using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class DebugGenerator : ComputeTexture2D
{
    public override RenderTexture CreateRenderTexture()
    {
        return base.CreateRenderTexture();
    }
    public override void OnPreGenerateGUI()
    {
        
    }
    public override void OnGenerate()
    {
       
    }
}
