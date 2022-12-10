using UnityEngine;

public class ShaderPlayerPosition : MonoBehaviour
{
    readonly int pos = Shader.PropertyToID("_PositionMoving");
    private void Update()
    {
        GetShaderPlayerPosition();
    }

    private void GetShaderPlayerPosition()
    {
        Shader.SetGlobalVector(pos, transform.position);
    }
}