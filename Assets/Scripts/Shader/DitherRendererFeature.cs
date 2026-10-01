using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class DitherRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material ditherMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        [Range(1f, 16f)] public float ditherScale = 4f;
        [Range(2f, 32f)] public float colorLevels = 4f;
        [Range(0f, 2f)] public float ditherStrength = 1f;
    }

    public Settings settings = new Settings();
    private DitherPass _pass;

    public override void Create()
    {
        _pass = new DitherPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.ditherMaterial == null) return;
        renderer.EnqueuePass(_pass);
    }

    class DitherPass : ScriptableRenderPass
    {
        private Settings _settings;
        private static readonly int DitherScaleID = Shader.PropertyToID("_DitherScale");
        private static readonly int ColorLevelsID = Shader.PropertyToID("_ColorLevels");
        private static readonly int DitherStrengthID = Shader.PropertyToID("_DitherStrength");

        private class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        public DitherPass(Settings settings)
        {
            _settings = settings;
            renderPassEvent = settings.renderPassEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var mat = _settings.ditherMaterial;
            mat.SetFloat(DitherScaleID, _settings.ditherScale);
            mat.SetFloat(ColorLevelsID, _settings.colorLevels);
            mat.SetFloat(DitherStrengthID, _settings.ditherStrength);

            var source = resourceData.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "_DitherTarget";
            desc.clearBuffer = false;
            var destination = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Dither Pass", out var passData))
            {
                passData.material = mat;
                passData.source = source;
                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            resourceData.cameraColor = destination;
        }
    }
}