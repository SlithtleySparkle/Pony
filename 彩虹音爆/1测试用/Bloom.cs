using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Bloom : MonoBehaviour
{
    public bool RainbowBloom = false;
    public float RainbowChaJu = 5;

    public Shader BloomShader;
    private Material BloomMaterial = null;

    [Range(1, 8)]
    public int DownSample = 1;
    [Range(1, 6)]
    public int DieDai = 1;
    [Range(1.0f, 10.0f)]
    public float SampleFanWei = 1.0f;

    private int RainbowChaJuID;
    private int SampleFanWeiID;

    private void Start()
    {
        RainbowChaJuID = Shader.PropertyToID("_RainbowChaJu");
        SampleFanWeiID = Shader.PropertyToID("_SampleFanWei");
    }
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
            if(RainbowBloom == false)
            {
                Graphics.Blit(src, buffer0, BloomMaterial, 0);
            }
            else
            {
                BloomMaterial.SetFloat(RainbowChaJuID, RainbowChaJu);
                Graphics.Blit(src, buffer0, BloomMaterial, 1);
            }

            for(int i = 0; i < DieDai; i++)
            {
                BloomMaterial.SetFloat(SampleFanWeiID, SampleFanWei + i);
                RenderTexture buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);
                Graphics.Blit(buffer0, buffer1, BloomMaterial, 2);
                RenderTexture.ReleaseTemporary(buffer0);
                buffer0 = buffer1;
                buffer1 = RenderTexture.GetTemporary(rtW, rtH, 0);
                Graphics.Blit(buffer0, buffer1, BloomMaterial, 3);
                RenderTexture.ReleaseTemporary(buffer0);
                buffer0 = buffer1;
            }
            BloomMaterial.SetTexture("_Bloom", buffer0);

            if (RainbowBloom == false)
            {
                Graphics.Blit(src, des, BloomMaterial, 4);
            }
            else
            {
                Graphics.Blit(src, des, BloomMaterial, 5);
            }
            RenderTexture.ReleaseTemporary(buffer0);
        }
        else
        {
            Graphics.Blit(src, des);
        }
    }
}
