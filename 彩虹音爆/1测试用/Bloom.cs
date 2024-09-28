using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Bloom : MonoBehaviour
{
    public Shader BloomShader;
    private Material BloomMaterial = null;

    [Range(1, 8)]
    public int DownSample = 1;
    [Range(1, 6)]
    public int DieDai = 1;
    [Range(1.0f, 10.0f)]
    public float SampleFanWei = 1.0f;
    private void OnEnable()
    {
        BloomMaterial = new Material(BloomShader);
    }
    private void OnRenderImage(RenderTexture src, RenderTexture des)
    {
        if(BloomMaterial != null)
        {
            int rtW = src.width / DownSample;
            int rtH = src.height / DownSample;
            RenderTexture buffer0 = RenderTexture.GetTemporary(rtW, rtH, 0);

            buffer0.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, buffer0, BloomMaterial, 0);

            for(int i = 0; i < DieDai; i++)
            {
                BloomMaterial.SetFloat("_SampleFanWei", SampleFanWei + i);
                RenderTexture buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);
                Graphics.Blit(buffer0, buffer1, BloomMaterial, 1);
                RenderTexture.ReleaseTemporary(buffer0);
                buffer0 = buffer1;
                buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);
                Graphics.Blit(buffer0, buffer1, BloomMaterial, 2);
                RenderTexture.ReleaseTemporary(buffer0);
                buffer0 = buffer1;
            }
            BloomMaterial.SetTexture("_Bloom", buffer0);
            Graphics.Blit(src, des, BloomMaterial, 3);
            RenderTexture.ReleaseTemporary(buffer0);
        }
        else
        {
            Graphics.Blit(src, des);
        }
    }
}
