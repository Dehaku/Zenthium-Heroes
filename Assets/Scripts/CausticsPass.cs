using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CausticsPass : ScriptableRenderPass
{
    private CausticsFeatureAlex.CausticsSettings settings;

    public CausticsPass(CausticsFeatureAlex.CausticsSettings settings)
    {
        this.settings = settings;
        renderPassEvent = settings.renderPassEvent;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cam = renderingData.cameraData.camera;

        if (cam.cameraType == CameraType.Preview || !settings.causticsMaterial) return;

        var sunMatrix = RenderSettings.sun != null
                ? RenderSettings.sun.transform.localToWorldMatrix
                : Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-45f, 45f, 0f), Vector3.one);

        settings.causticsMaterial.SetMatrix("_MainLightDirection", sunMatrix);
    }
}