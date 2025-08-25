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
    public Transform cameraTarget; // (�ܺο��� ����� �� �־� ����)
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
    float mouseX, mouseY; // mouseY�� ���� �̻�������� �Է� ü�� ���� ���� ����

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
    // PC: yaw ���� / Mobile: �巡�� ����
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
    [HideInInspector] public bool inputEnabled; // Stage ���� ������ true�� ��ȯ
    bool isDragFromUI = false;

    /// <summary>
    /// ������Ʈ ĳ�� �� �ʱ� �Է� ��Ȱ��ȭ.
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
    /// �Է� ���� �� ��/��ũ ���� ����.
    /// </summary>
    void Update()
    {
        if (!inputEnabled) return;
        GetInput();
        UpdateDelta();
    }

    /// <summary>
    /// �÷����� �̵�/���� ���� �� �� ��ȯ.
    /// </summary>
    void FixedUpdate()
    {
        if (!inputEnabled) return;

        // ���� �߿��� �̵�/���� �Է� ����
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
    /// �ִϸ��̼� �Ķ���͸� ������ ���ܿ��� ����.
    /// </summary>
    void LateUpdate()
    {
        UpdateAnimIdle();
    }

    // ====== Update helpers ======

    /// <summary>
    /// �÷����� �Է� ����(�̵�/����/����/��ų/����).
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
    /// ��/��ũ �ִ� ���� �� ����.
    /// </summary>
    private void UpdateDelta()
    {
        deltaAnimWalkToRun += Time.deltaTime * (isRun ? 2.0f : -2.0f);
        if (deltaAnimWalkToRun > 1.0f) deltaAnimWalkToRun = 1.0f;
        else if (deltaAnimWalkToRun < 0.0f) deltaAnimWalkToRun = 0.0f;
    }

    /// <summary>
    /// ��ġ/���콺 �巡�� �Է��� ����(�����/PC ���� ����).
    /// UI ������ ������ �巡�״� �����Ѵ�.
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

                // UI �󿡼� ������ ��ġ�� ī�޶� �巡�׷� ������� ����
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

            // UI �� Ŭ���� ī�޶� �巡�׷� ������� ����
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
    /// UI ��Ʈ �� 'UITouchable' ������Ʈ�� ���� ��ҿ� ��Ҵ��� �˻��Ѵ�.
    /// (�׷� ��� ī�޶� �巡�׸� ����)
    /// </summary>
    private bool IsPointerOverUIUntouchable(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var item in results)
        {
            // �θ� ü�� �� ��� ���̶� UITouchable�� ������ ��ġ ���
            if (HasUITouchableComponent(item.gameObject))
                continue;

            // UITouchable�� ���� UI�� ����ٸ� ����
            return true;
        }
        // UI�� ���ų� ��� UITouchable�̸� ���
        return false;
    }

    /// <summary>
    /// �θ� ü�ο� UITouchable ������Ʈ�� �ִ��� Ȯ��.
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
    /// ���콺 X �Է����� Yaw ȸ��.
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
    /// �巡�� X �Է����� Yaw ȸ��(�����).
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
    /// ���� ���¿� ���� �⺻/���� VCam �켱������ ��Ʈ ������ ��ȯ.
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
    /// ���� �ٶ󺸴� ������ �������� ���� ī�޶� ȸ�� �������� �缳��(���� ����).
    /// </summary>
    public void ResetVCamRotation()
    {
        float currentYAngle = transform.eulerAngles.y;
        vcamYAxis = currentYAngle;
        vcamHRotation = new Vector3(0.0f, currentYAngle);
        curVelocity = Vector3.zero;
    }

    /// <summary>
    /// Ű���� �̵�(WSAD) ó��.
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
    /// ���̽�ƽ �̵�(�����) ó��.
    /// </summary>
    private void MoveByJoystick()
    {
        // Ű���� �Է��� ������ ���̽�ƽ �Է� ����
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
    /// �ִϸ����� �Ķ����(H/V��, Run ����) ����.
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
    /// UI ���̽�ƽ �̵� �ݹ�. (�Ÿ� Ŭ���� �� ���� ����ȭ)
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
    /// �ǰ� ó��(HP ����, ����Ʈ, ������ �ؽ�Ʈ, ��� �� ���ӿ���).
    /// </summary>
    public void OnDamage(Calculator.DamageInfo info)
    {
        _statCtrl.HPCurrent -= info.Value;

        if (_statCtrl.HPCurrent > 0f)
        {
            // �ǰ� SFX
            AudioPlayerPoolManager.Instance.PlaySFXClipOnce(
                AudioPlayerPoolManager.SFXPoolType.Player,
                sfxClip_Hit,
                0.66f
            );

            // ���� ��������
            UIManager.Inst.DisplayBloodOverlay();

            // ������ �ؽ�Ʈ
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
