using Cinemachine;
using System;
using System.Collections.Generic;
//using System.Collections;
//using System.Collections.Generic;
//using System.Xml.Serialization;
//using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
//using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Input System")]
    public string runBtnName;
    public string spSelfBtnName;
    public string spCastBtnName;

    [Header("ControlVCam")]
    public CinemachineVirtualCamera vcamDefault;
    public CinemachineVirtualCamera vcamAim;
    public Transform cameraTarget;
    [Range(0, 10)] public float mouseXSensitivity;
    [Range(0, 10)] public float touchXSensitivity;

    [Header("Move")]
    public float moveSpeed;
    public float runSpeed;

    [Header("Audio clip")]
    public AudioClip sfxClip_Hit;

    //--- [Components]
    CharacterController _chrCtrl;
    StatController _statCtrl;
    //[HideInInspector] public WeaponController _rWpnCtrl;
    private AnimController _animCtrl;
    Animator _anim;

    //--- [Input System]
    private float hAxis = 0.0f;
    private float vAxis = 0.0f;
    private float mouseX = 0.0f;
    private float mouseY = 0.0f;

    //--- [Cameras]
    CinemachineVirtualCamera vcamCurrent;
    RectTransform hrCurrent;
    RectTransform hrPrev;

    //--- [Move]
    //ANDROID
    float joystickMoveLen = 0f;
    Vector3 joystickInput = Vector3.zero;
    Vector3 smoothedJoystickInput = Vector3.zero;
    Vector3 joystickInputVelocity = Vector3.zero;
    //COMMON
    float deltaAnimWalkToRun = 0.0f;

    //--- [Look]
    //WINDOWS
    private float vcamYAxis;
    //ANDROID
    private Vector2 dragMoveDir = Vector2.zero;
    private Vector2 lastTouchPosition;
    //COMMON
    private float smoothTime = 0.12f;
    private Vector3 vcamHRotation;
    private Vector3 curVelocity;

    //--- [NAttack]
    GameObject slashEffect;

    //--- [Flags]
    [HideInInspector] public bool isRun;
    [HideInInspector] public bool isNAttack;
    [HideInInspector] public bool isSAttack;
    [HideInInspector] public bool isSpSelf;
    [HideInInspector] public bool isSpCast;
    [HideInInspector] public Touch touch;
    [HideInInspector] public bool isOnDrag;
    [HideInInspector] public bool inputEnabled;
    private bool isDragFromUI = false;

    // Start is called before the first frame update
    void Start()
    {
        _chrCtrl = GetComponent<CharacterController>();
        _statCtrl = GetComponent<StatController>();
        _animCtrl = GetComponent<AnimController>();
        _anim = GetComponent<Animator>();

        inputEnabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!inputEnabled) return;

        GetInput();
        UpdateDelta();
    }

    void FixedUpdate()
    {
        if (!inputEnabled) return;
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

    void LateUpdate()
    {
        UpdateAnimIdle();
    }

    #region --- [Update func]

    private void GetInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        //--- [MOVE]
        hAxis = Input.GetAxis("Horizontal");
        vAxis = Input.GetAxis("Vertical");
        isRun = Input.GetButton(runBtnName);
        //--- [LOOK]
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (!StageUIManager.Inst.IsPauseWindowOnDisplay)
            isNAttack = Input.GetMouseButton(0);
        isSAttack = Input.GetMouseButton(1) || _animCtrl.IsStillSA;

        isSpSelf = Input.GetButtonDown(spSelfBtnName);  //Q
        isSpCast = Input.GetButtonDown(spCastBtnName);  //E
#elif UNITY_ANDROID || UNITY_IOS
        //--- [MOVE]
        smoothedJoystickInput = Vector3.SmoothDamp
            (smoothedJoystickInput,
            joystickInput,
            ref joystickInputVelocity,
            0.001f);
        isRun = smoothedJoystickInput.magnitude > 0.33f ? true : false;
        //--- [LOOK]
        GetInputTouch();
#endif

        //if (!StageUIManager.Inst.IsPauseWindowOnDisplay)
        //    isNAttack = Input.GetMouseButton(0);
        //isSAttack = Input.GetMouseButton(1) || _animCtrl.isStillSA;

        //isSpSelf = Input.GetButtonDown(spSelfBtnName);  //Q
        //isSpCast = Input.GetButtonDown(spCastBtnName);  //E
    }

    private void UpdateDelta()
    {
        deltaAnimWalkToRun += Time.deltaTime * (isRun ? 2.0f : -2.0f);
        if (deltaAnimWalkToRun > 1.0f) deltaAnimWalkToRun = 1.0f;
        else if (deltaAnimWalkToRun < 0.0f) deltaAnimWalkToRun = 0.0f;
    }

    private void GetInputTouch()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 1)
        {
            touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;

                //Check is touch started from UIs.
                if (EventSystem.current != null &&
                    EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    isDragFromUI = true;
                    return;
                }
                else
                {
                    isDragFromUI = false;
                    isOnDrag = true;
                }
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

            //Ignore touch over UIs.
            if (EventSystem.current != null &&
                /*EventSystem.current.IsPointerOverGameObject()*/
                IsPointerOverUIUntouchable(mousePos))
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

    private bool IsPointerOverUIUntouchable(Vector2 screenPosition)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log($"[Raycast Result Count] {results.Count}");

        foreach (var item in results)
        {
            Debug.Log($"UI Hit: {item.gameObject.name}");

            //if (item.gameObject.CompareTag("UITouchable"))
            //{
            //    continue;
            //}

            // 부모 포함해서 UITouchable 컴포넌트가 있는지 확인
            if (HasUITouchableComponent(item.gameObject))
            {
                continue;
            }

            //Return true if there's UI with no tag 'UITouchable'.
            return true;
        }

        //Return false if there's no UI or UI with tag 'UITouchable'.
        return false;
    }


    private bool HasUITouchableComponent(GameObject obj)
    {
        Transform current = obj.transform;
        while (current != null)
        {
            if (current.GetComponent<UITouchable>() != null)
            {
                Debug.Log("Find");
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    #endregion

    #region --- [FixedUpdate func]

    private void ControlVCamMoveByMouse()
    {
        vcamYAxis = vcamYAxis + mouseX * mouseXSensitivity * 0.2f;

        vcamHRotation = Vector3.SmoothDamp(vcamHRotation,
                                            new Vector3(0.0f, vcamYAxis),
                                            ref curVelocity,
                                            smoothTime);

        transform.eulerAngles = vcamHRotation;
    }

    private void ControlVCamMoveByTouch()
    {
        float normalizedX = dragMoveDir.x / Screen.width * 2f;
        vcamYAxis = vcamYAxis + normalizedX * touchXSensitivity * 10f;

        vcamHRotation = Vector3.SmoothDamp(vcamHRotation,
                                            new Vector3(0.0f, vcamYAxis),
                                            ref curVelocity,
                                            smoothTime);

        transform.eulerAngles = vcamHRotation;
    }

    private void ControlVCamZoom()
    {
        bool isAnimSA = _animCtrl.m_PreState == AnimController.AnimState.SAttack;
        vcamDefault.Priority = isAnimSA ? 1 : 10;
        vcamAim.Priority = isAnimSA ? 10 : 1;

        hrCurrent = (isAnimSA ? vcamAim : vcamDefault)
                    .GetComponent<VCamController>().hitRange;
        hrPrev = (isAnimSA ? vcamDefault : vcamAim)
                .GetComponent<VCamController>().hitRange;
        hrCurrent.GetComponent<RawImage>().color = new Color(1, 1, 1, 0.5f);
        hrPrev.GetComponent<RawImage>().color = new Color(1, 1, 1, 0);
    }

    public void ResetVCamRotation()
    {
        // 현재 카메라 y축 각도 가져오기
        float currentYAngle = transform.eulerAngles.y;

        // 입력값 기준을 현재 방향으로 맞춤
        vcamYAxis = currentYAngle;

        // 스무딩 회전 초기화
        vcamHRotation = new Vector3(0.0f, currentYAngle);
        curVelocity = Vector3.zero;
    }

    private void MoveByKeyboard()
    {
        if (vAxis != 0f || hAxis != 0f)
        {
            Vector3 moveDir = Vector3.forward * vAxis + Vector3.right * hAxis;
            if (1.0f < moveDir.magnitude)
                moveDir.Normalize();

            if (_chrCtrl != null)
            {
                moveDir = transform.TransformDirection(moveDir);
                _chrCtrl.Move(moveDir * Time.deltaTime * (isRun ? runSpeed : moveSpeed));
            }
        }
    }

    private void MoveByJoystick()
    {
        //Return if there's keyboard input.
        if (vAxis != 0f || hAxis != 0f) return;

        if (joystickMoveLen > 0f)
        {
            Vector3 moveDir = joystickInput;
            if (1.0f < moveDir.magnitude)
                moveDir.Normalize();

            if (_chrCtrl != null)
            {
                moveDir = transform.TransformDirection(moveDir);
                _chrCtrl.Move(moveDir * Time.deltaTime * (isRun ? runSpeed : moveSpeed));
            }
        }
    }

    #endregion

    #region --- [LateUpdate func]

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

    #endregion

    #region --- [Event Func]

    public void OnJoystickMove(Vector2 joystickDir, float distanceRatio)
    {
        Vector3 temp = new Vector3(joystickDir.x, 0f, joystickDir.y);
        if ((joystickMoveLen = joystickDir.magnitude) > 1f)
            joystickInput = temp.normalized;
        else
            joystickInput = temp;
    }

    public void OnDamage(Calculator.DamageInfo info)
    {
        //Decrease HP.
        _statCtrl.HPCurrent -= info.Value;

        if (_statCtrl.HPCurrent > 0f)
        {
            //Play SFX: Hit.
            AudioPlayerPoolManager.Instance.PlaySFXClipOnce
            (AudioPlayerPoolManager.SFXPoolType.Player,
            sfxClip_Hit,
            0.66f);

            /*Set on damaged*/
            /*Test*/
            Debug.Log($"[남은 체력: {_statCtrl.HPCurrent}] {info.Value}의 데미지를 입었습니다.");
            /*Test*/

            //1. Display blood overlays.
            UIManager.Inst.DisplayBloodOverlay();

            //2. Instantiate damage text.
            GameObject obj = DamageTextPoolManager.Inst.Get(info);
            DamageTextController dtc = obj.GetComponent<DamageTextController>();
            StartCoroutine(dtc.Init(DamageTextPoolManager.Inst.GetRoot(transform), 0f));
        }
        else
        {
            /*사망 처리*/
            /*Test*/
            Debug.Log($"[!!사망!!] {info.Value}의 데미지를 입었습니다.");
            /*Test*/

            _statCtrl.HPCurrent = 0f;
            StartCoroutine(StageManager.Inst.GameOver(false));
        }
    }

    #endregion

    public RectTransform GetHRCurrentRect()
    {
        return hrCurrent;
    }
}
