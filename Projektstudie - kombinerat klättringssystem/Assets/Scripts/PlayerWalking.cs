using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PlayerWalking : PlayerStateInterface
{
    private Vector3 m_moveSpeed;
    private Vector3 move;
    private Transform transform;
    private Camera m_cam;
    private float m_maxClimbEnterAngle, m_normalOffset;

    private PlayerMovementController m_moveBase;

    private bool m_isGrounded, m_hasInitiated, m_hasExited;
    private Rigidbody m_rigid;

    public PlayerWalking(PlayerMovementController moveBase, Transform transform, Vector3 moveSpeed, Camera cam, float enterAngle, Rigidbody rigid, float normalOffset)
    {
        this.m_moveBase = moveBase;
        this.transform = transform;
        this.m_moveSpeed = moveSpeed;
        this.m_cam = cam;
        this.m_maxClimbEnterAngle = enterAngle;
        this.m_rigid = rigid;
        this.m_normalOffset = normalOffset;
    }

    public void Init()
    {
        m_hasExited = false;
        Vector3 rotation = transform.eulerAngles;
        rotation.x = 0;
        transform.rotation = Quaternion.Euler(rotation);
        m_hasInitiated = true;
    }

    public bool HasBeenInitiated()
    {
        return m_hasInitiated;
    }

    public void Update()
    {
        RotateWithCamera();
        NormalMove();
        NullifyRigid();
    }

    public void FixedUpdate()
    {

    }

    void NormalMove()
    {
        if (m_isGrounded)
        {
            move = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                move += transform.forward * m_moveSpeed.z;
            }
            if (Input.GetKey(KeyCode.S))
            {
                move -= transform.forward * m_moveSpeed.z;
            }
            if (Input.GetKey(KeyCode.A))
            {
                move -= transform.right * m_moveSpeed.x;
            }
            if (Input.GetKey(KeyCode.D))
            {
                move += transform.right * m_moveSpeed.x;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                move.y = m_moveSpeed.y;
                m_isGrounded = false;
            }
        }
        else
        {
            move.y -= -Physics.gravity.y * Time.deltaTime;
        }

        transform.position += move * Time.deltaTime;

        TryEnterClimbingState();
        CheckForFloor();
    }

    void RotateWithCamera()
    {
        //Vector3 forward = m_cam.transform.TransformDirection(m_cam.transform.forward);
        //forward = transform.InverseTransformDirection(forward);
        //Quaternion lookRotation = Quaternion.LookRotation(forward, Vector3.up);
        //transform.rotation = lookRotation;

        Vector3 rotation = transform.eulerAngles;
        rotation.y = m_cam.transform.eulerAngles.y;
        rotation.x = 0;
        transform.rotation = Quaternion.Euler(rotation);
    }

    void NullifyRigid()
    {
        m_rigid.velocity = Vector3.zero;
        m_rigid.angularVelocity = Vector3.zero;
    }

    public void OnCollisionEnter(Collision hit)
    {
        //Setting is_grounded to true in both cases because it didn't get reset when re-entering the walking state if the player was falling due to jumping,
        //which caused the player to get pushed into the floor when climbing down manually
        if (hit.transform.CompareTag("Floor"))
        {
            // move = Vector3.zero;
            // m_isGrounded = true;
        }
        if (hit.transform.CompareTag("Climb"))
        {
            // Vector3 point = hit.GetContact(0).point;
            // Vector3 normal = hit.GetContact(0).normal;
            // if (!VerifyWallEnterAngle(normal))
            //     return;
            // move = Vector3.zero;
            // m_isGrounded = true;
            // ReorientToWall(point, normal);
            // ChangeState(m_moveBase.climbingState);
        }
    }

    public void OnCollisionStay(Collision hit)
    {

    }

    public void OnCollisionExit(Collision hit)
    {

    }

    void ReorientToWall(Vector3 point, Vector3 normal)
    {
        // Vector3 newDir = (point - transform.position);
        // //Fast fix because hit.point wasn't checked from the transforms positional y-value
        // newDir = Vector3.ProjectOnPlane(newDir, Vector3.up);
        // Quaternion lookRotation = Quaternion.LookRotation(newDir);
        // transform.rotation = lookRotation;

        //Faster fix, why project vector onto a plane when you can make a new vector with a proper y-value & use that vector for direction-calculation instead
        //Vector3 newPoint = new Vector3(point.x, transform.position.y, point.z);
        Vector3 newDir = (point - transform.position).normalized;
        //Fast fix because hit.point wasn't checked from the transforms positional y-value
        //Secondly, works better because it covers slanted walls better by aligning the direction with the wall-normal instead of the up-vector
        newDir = Vector3.Project(newDir, normal);
        Quaternion lookRotation = Quaternion.LookRotation(newDir);
        transform.rotation = lookRotation;
        Vector3 newPos = point + (-newDir * (1.0f - m_normalOffset));//new Vector3(point.x, transform.position.y, point.z);
        transform.position = newPos + transform.up * 0.5f;
    }

    //Raycast replacement for ReorientToNewWall ^
    void TryEnterClimbingState()
    {
        Ray rayForward = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Vector3.Dot(move, transform.forward) < 0.9f)
            return;
        if (Physics.Raycast(rayForward, out hit, 1f))
        {
            if (hit.transform.CompareTag("Climb"))
            {
                if (VerifyWallEnterAngle(hit.normal))
                {
                    move = Vector3.zero;
                    m_isGrounded = true;
                    Vector3 direction = (hit.point - transform.position).normalized;
                    direction = Vector3.Project(direction, hit.normal);
                    Quaternion look = Quaternion.LookRotation(direction);
                    transform.rotation = look;
                    transform.position = hit.point + hit.normal * 0.5f;
                    ChangeState(m_moveBase.climbingState);
                }
            }
        }
    }

    void CheckForFloor()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, 1f))
        {
            if (hit.transform.CompareTag("Floor"))
            {
                move = Vector3.zero;
                m_isGrounded = true;
            }
        }
    }

    bool VerifyWallEnterAngle(Vector3 normal)
    {
        return Vector3.Dot(transform.up, normal) >= m_maxClimbEnterAngle;
    }

    public void Exit()
    {
        m_hasInitiated = false;
        m_hasExited = true;
    }

    public bool HasExited()
    {
        return m_hasExited;
    }

    public void ChangeState(PlayerStateInterface newState)
    {
        m_moveBase.SetCamState(newState);
        m_moveBase.m_currentState = newState;
    }

    public void OnDrawGizmos()
    {

    }
}
