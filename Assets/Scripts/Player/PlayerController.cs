 using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private CharacterController cc;
    private Camera mainCam;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        mainCam = Camera.main;
    }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 카메라 기준 벡터(수평 투영)
        Vector3 camF = Vector3.ProjectOnPlane(mainCam.transform.forward, Vector3.up).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(mainCam.transform.right,   Vector3.up).normalized;

        Vector3 moveDir;

        // 카메라가 거의 수직(탑다운)이라면 월드 기준으로 폴백
        if (camF.sqrMagnitude < 0.01f || camR.sqrMagnitude < 0.01f)
        {
            moveDir = new Vector3(h, 0f, v);          // 월드 기준
        }
        else
        {
            moveDir = camF * v + camR * h;            // 카메라 기준
        }

        // 입력 있을 때만 바라보기 갱신
        if (moveDir.sqrMagnitude > 0.0001f)
            transform.forward = moveDir.normalized;

        cc.Move(moveDir * moveSpeed * Time.deltaTime);
    }
}
