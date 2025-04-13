using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZhongZhuiToHouZhui;

public class Houzhuibdsceshi : MonoBehaviour
{
    public int num = 10;
    public bool BoLanOrNor;
    public string shizi;

    private ZhongZhuiToHouZhuiMathf aaa;

    private void Start()
    {
        aaa = gameObject.GetComponent<ZhongZhuiToHouZhuiMathf>();
    }
    void Update()
    {
        if (BoLanOrNor)
        {
            for (int i = 0; i < num; i++)
            {
                Debug.Log(aaa.Evalute(shizi));
            }
        }
        else
        {
            for (int i = 0; i < num; i++)
            {
                Debug.Log(6.6f * ((((0.9f * 10) * 2.9f - (12 * 16.54f + 19)) * (9.5f * 0.53f + 7.7f)) * 0.2f + 0.95f * ((((5 * 6) - 7) + 121) - (97 * 91 - (555 - 139) * 0.6f))));
            }
        }
    }
}
