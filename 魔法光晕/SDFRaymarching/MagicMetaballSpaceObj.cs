﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicMetaballSpaceObj : MonoBehaviour
{
    //[Tooltip("不同颜色之间的过渡强度")]
    //[Range(0.0f, 0.5f)]
    //public float ColorControlSmooth = 0.25f;
    //private static readonly int ColorControlSmoothID = Shader.PropertyToID("_ColorControlSmooth");
    [Tooltip("正常情况下的颜色，RenderFeature获取，如果有多个物体合在一起，则默认用距离主相机最近的颜色，这时候建议全换成一样的颜色")]
    public Color normalColor = Color.white;
    [Tooltip("至少两个， 最多两个（尝试更多）")]
    public ColorControlPoint[] colorControlPoint = new ColorControlPoint[2];
    private int colorControlPointNum_old;
    private int colorControlPointNum_new;
    [System.Serializable]
    public class ColorControlPoint
    {
        [Tooltip("控制点位置")]
        public Transform colConPoint;
        [Tooltip("相应颜色")]
        public Color colConColor = Color.white;
    }
    private List<Vector4> ColorControlPosWS = new();
    private static readonly int ColorControlPosWSID = Shader.PropertyToID("_ColorControlPosWS");
    private List<Vector4> ColorControlColor = new();
    private static readonly int ColorControlColorID = Shader.PropertyToID("_ColorControlColor");

    void Start()
    {
        ClaerConListProperties();

        colorControlPointNum_old = colorControlPoint.Length;
    }
    void Update()
    {
        colorControlPointNum_new = colorControlPoint.Length;
        if (colorControlPointNum_new != colorControlPointNum_old)
        {
            ClaerConListProperties();

            colorControlPointNum_old = colorControlPointNum_new;
        }
    }
    private void ClaerConListProperties()
    {
        ConPointClear();
        for (int i = 0; i < colorControlPoint.Length; i++)
        {
            if (colorControlPoint[i].colConPoint != null && colorControlPoint[i].colConColor != null)
            {
                ColorControlPosWS.Add(new(colorControlPoint[i].colConPoint.position.x, colorControlPoint[i].colConPoint.position.y, colorControlPoint[i].colConPoint.position.z, 1.0f));
                ColorControlColor.Add(new(colorControlPoint[i].colConColor.r, colorControlPoint[i].colConColor.g, colorControlPoint[i].colConColor.b, colorControlPoint[i].colConColor.a));
            }
        }
    }
    private void OnDisable()
    {
        ConPointClear();
    }
    //控制点
    private void ConPointClear()
    {
        ColorControlPosWS?.Clear();
        ColorControlPosWS?.TrimExcess();
        ColorControlColor?.Clear();
        ColorControlColor?.TrimExcess();
    }
}