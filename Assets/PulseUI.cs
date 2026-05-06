using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseUI : MonoBehaviour
{
    public float speed = 2f;     // 呼吸速度
    public float scaleAmount = 0.05f; // 放大幅度（5%）

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float scale = 1 + Mathf.Sin(Time.time * speed) * scaleAmount;
        transform.localScale = baseScale * scale;
    }
}