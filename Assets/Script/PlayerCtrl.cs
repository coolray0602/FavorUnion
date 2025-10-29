using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;   // ✅ 為 WebGL 用的 JS 呼叫

public class PlayerCtrl : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern string GetUserAgent();   // ✅ WebGL 呼叫 JS 方法

    public GameObject eye;
    private Rigidbody rb;
    private Animator animator;

    public float moveSpeed = 10f;
    public float rotateSpeed = 80f;
    public float jumpSpeed = 0.01f;

    private float pitch = 0f;

    private bool isGround = false;
    private bool isJumpPreparing = false;
    private bool isJumping = false;
    private bool isLanding = false;

    public Joystick joystick;  // 手機虛擬搖桿引用
    private bool isMobile = false;

    // 手機滑動視角用變數
    private Vector2 lastTouchPos;
    private bool isTouching = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

#if UNITY_EDITOR || UNITY_STANDALONE
        isMobile = false;
#elif UNITY_IOS || UNITY_ANDROID
        isMobile = true;
#elif UNITY_WEBGL
        isMobile = IsMobileWebGL();    // ✅ 判斷 WebGL 瀏覽器是否為手機
#else
        isMobile = Input.touchSupported;
#endif

        if (!isMobile)
            HideCursor();
    }

    void Update()
    {
        // ---- 檢查是否在地面上 ----
        isGround = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f);

        HandleJump();
        HandleMovementAndRotation();

        // ---- 落地檢查 ----
        if (isGround && isJumping)
        {
            isJumping = false;
            isJumpPreparing = false;
            animator.ResetTrigger("jump");
        }
    }

    void HandleJump()
    {
        if (!isMobile)
        {
            // ---- 電腦鍵盤 ----
            if (Input.GetKeyDown(KeyCode.Space) && isGround && !isJumpPreparing && !isJumping)
            {
                isJumpPreparing = true;
                animator.SetTrigger("jump");
            }
            if (Input.GetKeyDown(KeyCode.Escape))
                ShowCursor();
        }
        else
        {
            // 手機上由 UI 按鈕呼叫 JumpButton()
        }
    }

    void HandleMovementAndRotation()
    {
        if (isJumpPreparing || isLanding)
        {
            animator.SetBool("isWalking", false);
            return;
        }

        float h, v;

        if (isMobile)
        {
            // ---- 手機移動（虛擬搖桿）----
            h = joystick.Horizontal * Time.deltaTime * moveSpeed;
            v = joystick.Vertical * Time.deltaTime * moveSpeed;

            HandleTouchLook();  // ---- 手機滑動視角控制 ----
        }
        else
        {
            // ---- 電腦移動（WASD）----
            h = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
            v = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

            // ---- 電腦滑鼠旋轉 ----
            float rx = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float ry = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

            transform.eulerAngles += new Vector3(0, rx, 0);
            pitch -= ry;
            pitch = Mathf.Clamp(pitch, -40f, 40f);
            eye.transform.localEulerAngles = new Vector3(pitch, 0, 0);
        }

        // ---- 移動與動畫 ----
        transform.Translate(h, 0, v);
        animator.SetBool("isWalking", (h != 0 || v != 0));
    }

    void HandleTouchLook()
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);

            // 只在右半邊控制視角
            if (t.position.x > Screen.width / 2)
            {
                if (t.phase == TouchPhase.Began)
                {
                    lastTouchPos = t.position;
                    isTouching = true;
                }
                else if (t.phase == TouchPhase.Moved && isTouching)
                {
                    Vector2 delta = t.deltaPosition * 0.2f; // 調整靈敏度
                    transform.Rotate(0, -delta.x, 0);

                    pitch -= delta.y * 0.2f;
                    pitch = Mathf.Clamp(pitch, -40f, 40f);
                    eye.transform.localEulerAngles = new Vector3(pitch, 0, 0);
                }
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    isTouching = false;
                }
            }
        }
    }

    // ✅ 跳躍動畫事件：角色起跳瞬間呼叫
    public void OnJumpForce()
    {
        rb.AddForce(Vector3.up * jumpSpeed, ForceMode.Impulse);
        isJumpPreparing = false;
        isJumping = true;
    }

    // ✅ 動畫事件：落地瞬間
    public void OnLanding()
    {
        isLanding = true;
    }

    // ✅ 動畫事件：落地結束
    public void OnReady()
    {
        isLanding = false;
    }

    // ✅ 供手機 UI 按鈕呼叫跳躍
    public void JumpButton()
    {
        if (isGround && !isJumpPreparing && !isJumping)
        {
            isJumpPreparing = true;
            animator.SetTrigger("jump");
        }
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ✅ WebGL 裝置判斷
    bool IsMobileWebGL()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            string ua = GetUserAgent();
            ua = ua.ToLower();

            if (ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("android") || ua.Contains("mobile"))
                return true;
        }
        catch (System.Exception)
        {
            // WebGL 無法取到 userAgent 時，預設為桌面
        }
#endif
        return false;
    }

    // ✅ 顯示偵測狀態（方便除錯）
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 40), "isMobile = " + isMobile);
    }
}
