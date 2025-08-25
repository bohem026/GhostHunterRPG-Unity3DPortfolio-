using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // ====== Input (names bound in Input Manager) ======
    [Header("Input System")]
    public string runBtnName;
    public string spSelfBtnName; // Q
    public string spCastBtnName; // E

    // ====== Camera / Aim ======
    [Header("ControlVCam")]
    public CinemachineVirtualCamera vcamDefault;
    public CinemachineVirtualCamera vcamAim;
    public Transform cameraTarget; // (외부에서 사용할 수 있어 보존)
    [Range(0, 10)] public float mouseXSensitivity = 5f;
    [Range(0, 10)] public float touchXSensitivity = 5f;

    // ====== Move ======
    [Header("Move")]
    public float moveSpeed = 4f;
    public float runSpeed = 6f;

    // ====== Audio ======
    [Header("Audio clip")]
    public AudioClip sfxClip_Hit;

    // ====== Components ======
    CharacterController _chrCtrl;
    StatController _statCtrl;
    AnimController _animCtrl;
    Animator _anim;

    // ====== Input state ======
    float hAxis, vAxis;
    float mouseX, mouseY; // mouseY는 현재 미사용이지만 입력 체계 유지 위해 보존

    // ====== Hit-range(crosshair) / Cameras ======
    RectTransform hrCurrent;
    RectTransform hrPrev;

    // ====== Move (Android) ======
    float joystickMoveLen = 0f;
    Vector3 joystickInput = Vector3.zero;
    Vector3 smoothedJoystickInput = Vector3.zero;
    Vector3 joystickInputVelocity = Vector3.zero;

    // ====== Move common ======
    float deltaAnimWalkToRun = 0.0f;

    // ====== Look ======
    // PC: yaw 누적 / Mobile: 드래그 누적
    float vcamYAxis;
    Vector2 dragMoveDir = Vector2.zero;
    Vector2 lastTouchPosition;
    float smoothTime = 0.12f;
    Vector3 vcamHRotation;
    Vector3 curVelocity;

    // ====== Flags ======
    [HideInInspector] public bool isRun;
    [HideInInspector] public bool isNAttack;
    [HideInInspector] public bool isSAttack;
    [HideInInspector] public bool isSpSelf;
    [HideInInspector] public bool isSpCast;
    [HideInInspector] public Touch touch;
    [HideInInspector] public bool isOnDrag;
    [HideInInspector] public bool inputEnabled; // Stage 시작 시점에 true로 전환
    bool isDragFromUI = false;

    /// <summary>
    /// 컴포넌트 캐싱 및 초기 입력 비활성화.
    /// </summary>
    void Start()
    {
        _chrCtrl = GetComponent<CharacterController>();
        _statCtrl = GetComponent<StatController>();
        _animCtrl = GetComponent<AnimController>();
        _anim = GetComponent<Animator>();
        inputEnabled = false;
    }

    /// <summary>
    /// 입력 수집 및 런/워크 보간 갱신.
    /// </summary>
    void Update()
    {
        if (!inputEnabled) return;
        GetInput();
        UpdateDelta();
    }

    /// <summary>
    /// 플랫폼별 이동/시점 제어 및 줌 전환.
    /// </summary>
    void FixedUpdate()
    {
        if (!inputEnabled) return;

        // 시전 중에는 이동/시점 입력 차단
        if (_animCtrl.m_PreState == AnimController.AnimState.SpSelf ||
            _animCtrl.m_PreState == AnimController.AnimState.SpCast)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        MoveByKeyboard();
        ControlVCamMoveByMouse();
#elif UNITY_ANDROID || UNITY_IOS
        MoveByJoystick();
        ControlVCamMoveByTouch();
#endif
        ControlVCamZoom();
    }

    /// <summary>
    /// 애니메이션 파라미터를 프레임 말단에서 갱신.
    /// </summary>
    void LateUpdate()
    {
        UpdateAnimIdle();
    }

    // ====== Update helpers ======

    /// <summary>
    /// 플랫폼별 입력 수집(이동/러닝/공격/스킬/시점).
    /// </summary>
    private void GetInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // MOVE
        hAxis = Input.GetAxis("Horizontal");
        vAxis = Input.GetAxis("Vertical");
        isRun = Input.GetButton(runBtnName);

        // LOOK
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        // ATTACK / SKILL
        if (!StageUIManager.Inst.IsPauseWindowOnDisplay)
            isNAttack = Input.GetMouseButton(0);
        isSAttack = Input.GetMouseButton(1) || _animCtrl.IsStillSA;

        isSpSelf = Input.GetButtonDown(spSelfBtnName);  // Q
        isSpCast = Input.GetButtonDown(spCastBtnName);  // E
#elif UNITY_ANDROID || UNITY_IOS
        // MOVE
        smoothedJoystickInput = Vector3.SmoothDamp(
            smoothedJoystickInput,
            joystickInput,
            ref joystickInputVelocity,
            0.001f
        );
        isRun = smoothedJoystickInput.magnitude > 0.33f;

        // LOOK
        GetInputTouch();
#endif
    }

    /// <summary>
    /// 런/워크 애니 블렌드 값 보간.
    /// </summary>
    private void UpdateDelta()
    {
        deltaAnimWalkToRun += Time.deltaTime * (isRun ? 2.0f : -2.0f);
        if (deltaAnimWalkToRun > 1.0f) deltaAnimWalkToRun = 1.0f;
        else if (deltaAnimWalkToRun < 0.0f) deltaAnimWalkToRun = 0.0f;
    }

    /// <summary>
    /// 터치/마우스 드래그 입력을 수집(모바일/PC 공용 로직).
    /// UI 위에서 시작한 드래그는 무시한다.
    /// </summary>
    private void GetInputTouch()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1)
        {
            touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;

                // UI 상에서 시작한 터치는 카메라 드래그로 취급하지 않음
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    isDragFromUI = true;
                    return;
                }
                isDragFromUI = false;
                isOnDrag = true;
            }
            else if (touch.phase == TouchPhase.Moved && isOnDrag && !isDragFromUI)
            {
                dragMoveDir = touch.position - lastTouchPosition;
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isOnDrag = false;
                isDragFromUI = false;
            }
        }
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            // UI 위 클릭은 카메라 드래그로 취급하지 않음
            if (EventSystem.current != null && IsPointerOverUIUntouchable(mousePos))
            {
                isDragFromUI = true;
            }
            else
            {
                isDragFromUI = false;
                lastTouchPosition = Input.mousePosition;
                isOnDrag = true;
            }
        }
        else if (Input.GetMouseButton(0) && isOnDrag && !isDragFromUI)
        {
            Vector2 currentMousePosition = Input.mousePosition;
            dragMoveDir = currentMousePosition - lastTouchPosition;
            lastTouchPosition = currentMousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isOnDrag = false;
            isDragFromUI = false;
        }
#endif
    }

    /// <summary>
    /// UI 히트 중 'UITouchable' 컴포넌트가 없는 요소에 닿았는지 검사한다.
    /// (그런 경우 카메라 드래그를 차단)
    /// </summary>
    private bool IsPointerOverUIUntouchable(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var item in results)
        {
            // 부모 체인 중 어느 곳이라도 UITouchable이 있으면 터치 허용
            if (HasUITouchableComponent(item.gameObject))
                continue;

            // UITouchable이 없는 UI를 맞췄다면 차단
            return true;
        }
        // UI가 없거나 모두 UITouchable이면 허용
        return false;
    }

    /// <summary>
    /// 부모 체인에 UITouchable 컴포넌트가 있는지 확인.
    /// </summary>
    private bool HasUITouchableComponent(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.GetComponent<UITouchable>() != null)
                return true;
            current = current.parent;
        }
        return false;
    }

    // ====== FixedUpdate helpers ======

    /// <summary>
    /// 마우스 X 입력으로 Yaw 회전.
    /// </summary>
    private void ControlVCamMoveByMouse()
    {
        vcamYAxis += mouseX * mouseXSensitivity * 0.2f;
        vcamHRotation = Vector3.SmoothDamp(
            vcamHRotation,
            new Vector3(0.0f, vcamYAxis),
            ref curVelocity,
            smoothTime
        );
        transform.eulerAngles = vcamHRotation;
    }

    /// <summary>
    /// 드래그 X 입력으로 Yaw 회전(모바일).
    /// </summary>
    private void ControlVCamMoveByTouch()
    {
        float normalizedX = dragMoveDir.x / Screen.width * 2f;
        vcamYAxis += normalizedX * touchXSensitivity * 10f;
        vcamHRotation = Vector3.SmoothDamp(
            vcamHRotation,
            new Vector3(0.0f, vcamYAxis),
            ref curVelocity,
            smoothTime
        );
        transform.eulerAngles = vcamHRotation;
    }

    /// <summary>
    /// 공격 상태에 따라 기본/에임 VCam 우선순위와 히트 범위를 전환.
    /// </summary>
    private void ControlVCamZoom()
    {
        bool isAnimSA = _animCtrl.m_PreState == AnimController.AnimState.SAttack;
        vcamDefault.Priority = isAnimSA ? 1 : 10;
        vcamAim.Priority = isAnimSA ? 10 : 1;

        hrCurrent = (isAnimSA ? vcamAim : vcamDefault).GetComponent<VCamController>().hitRange;
        hrPrev = (isAnimSA ? vcamDefault : vcamAim).GetComponent<VCamController>().hitRange;

        hrCurrent.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        hrPrev.GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// 현재 바라보는 방향을 기준으로 내부 카메라 회전 누적값을 재설정(점프 방지).
    /// </summary>
    public void ResetVCamRotation()
    {
        float currentYAngle = transform.eulerAngles.y;
        vcamYAxis = currentYAngle;
        vcamHRotation = new Vector3(0.0f, currentYAngle);
        curVelocity = Vector3.zero;
    }

    /// <summary>
    /// 키보드 이동(WSAD) 처리.
    /// </summary>
    private void MoveByKeyboard()
    {
        if (vAxis != 0f || hAxis != 0f)
        {
            Vector3 moveDir = Vector3.forward * vAxis + Vector3.right * hAxis;
            if (moveDir.magnitude > 1.0f) moveDir.Normalize();

            if (_chrCtrl != null)
            {
                moveDir = transform.TransformDirection(moveDir);
                _chrCtrl.Move(moveDir * Time.deltaTime * (isRun ? runSpeed : moveSpeed));
            }
        }
    }

    /// <summary>
    /// 조이스틱 이동(모바일) 처리.
    /// </summary>
    private void MoveByJoystick()
    {
        // 키보드 입력이 있으면 조이스틱 입력 무시
        if (vAxis != 0f || hAxis != 0f) return;

        if (joystickMoveLen > 0f)
        {
            Vector3 moveDir = joystickInput;
            if (moveDir.magnitude > 1.0f) moveDir.Normalize();

            if (_chrCtrl != null)
            {
                moveDir = transform.TransformDirection(moveDir);
                _chrCtrl.Move(moveDir * Time.deltaTime * (isRun ? runSpeed : moveSpeed));
            }
        }
    }

    // ====== LateUpdate helpers ======

    /// <summary>
    /// 애니메이터 파라미터(H/V축, Run 보간) 갱신.
    /// </summary>
    private void UpdateAnimIdle()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        _anim.SetFloat("HAxis", hAxis);
        _anim.SetFloat("VAxis", vAxis);
#elif UNITY_ANDROID || UNITY_IOS
        _anim.SetFloat("HAxis", smoothedJoystickInput.x);
        _anim.SetFloat("VAxis", smoothedJoystickInput.z);
#endif
        _anim.SetFloat("IsRun", deltaAnimWalkToRun);
    }

    // ====== Events ======

    /// <summary>
    /// UI 조이스틱 이동 콜백. (거리 클램프 및 벡터 정규화)
    /// </summary>
    public void OnJoystickMove(Vector2 joystickDir, float distanceRatio)
    {
        Vector3 temp = new Vector3(joystickDir.x, 0f, joystickDir.y);
        if ((joystickMoveLen = joystickDir.magnitude) > 1f)
            joystickInput = temp.normalized;
        else
            joystickInput = temp;
    }

    /// <summary>
    /// 피격 처리(HP 감소, 이펙트, 데미지 텍스트, 사망 시 게임오버).
    /// </summary>
    public void OnDamage(Calculator.DamageInfo info)
    {
        _statCtrl.HPCurrent -= info.Value;

        if (_statCtrl.HPCurrent > 0f)
        {
            // 피격 SFX
            AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
                AudioPlayerPoolManager.SFXPoolType.Player,
                sfxClip_Hit,
                0.66f
            );

            // 혈흔 오버레이
            UIManager.Inst.DisplayBloodOverlay();

            // 데미지 텍스트
            GameObject obj = DamageTextPoolManager.Inst.Get(info);
            var dtc = obj.GetComponent<DamageTextController>();
            StartCoroutine(dtc.Init(DamageTextPoolManager.Inst.GetRoot(transform), 0f));
        }
        else
        {
            _statCtrl.HPCurrent = 0f;
            StartCoroutine(StageManager.Inst.GameOver(false));
        }
    }

    // ====== Getter ======

    public RectTransform GetHRCurrentRect() => hrCurrent;
}
