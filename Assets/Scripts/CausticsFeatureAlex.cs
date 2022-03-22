using UnityEngine;
using UnityEngine.Rendering.Universal;
public class CausticsFeatureAlex : ScriptableRendererFeature
{
    [System.Serializable]
    public class CausticsSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material causticsMaterial;
    }

    CausticsPass pass;
    //public CausticsSettings settings = new();
    public CausticsSettings settings = new CausticsSettings();

    public override void Create()
    {
        pass = new CausticsPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }
}