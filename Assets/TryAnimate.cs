using DG.Tweening;
using UnityEngine;

public class TryAnimate : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Material material = GetComponent<MeshRenderer>().material;
        DOTween.To(() => material.GetColor("_BaseColor").a, x =>
        {
            Color temp = material.GetColor("_BaseColor");
            temp.a = x;
            material.SetColor("_BaseColor", temp);
        }, 0.5f, 5f).SetLoops(-1, LoopType.Yoyo);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
