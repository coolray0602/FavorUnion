using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public TMP_Text plateNumber1;
    public TMP_Text plateNumber2;

    [HideInInspector]
    public string plateNumber;

    // =========================
    // ⭐ 初始狀態（自動記錄）
    // =========================
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;

    void Awake()
    {
        // 記住「場景裡擺好的狀態」
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.2f, 0); // 往下移
    }
    // =========================
    // 原有功能（完全保留）
    // =========================
    public void SetPlateNumber(string number)
    {
        plateNumber = number;
        plateNumber1.text = number;
        plateNumber2.text = number;
    }



    // =========================
    // ⭐ 給 GameManager 呼叫
    // =========================
    public void ResetState()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }
}
