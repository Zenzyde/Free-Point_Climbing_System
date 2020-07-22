using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private Vector3 m_moveSpeed;
    [SerializeField] private float m_maxLerpCheck, m_maxClimbEnterAngle, m_maxClimbAroundWallAngle, m_normalOffset = 0.2f;
    [SerializeField] private Transform m_climbingCamOffset, m_jumpVisual;
    [SerializeField] private CinemachineFreeLook m_freelookCam;

    private Camera m_cam;
    private Vector3 move;

    public PlayerStateInterface m_currentState;
    public PlayerWalking walkingState;
    public PlayerClimbing climbingState;

    // Start is called before the first frame update
    void Start()
    {
        m_cam = Camera.main;

        walkingState = new PlayerWalking(this, transform, m_moveSpeed, m_cam, m_maxClimbEnterAngle, GetComponent<Rigidbody>(), m_normalOffset);
        climbingState = new PlayerClimbing(this, transform, m_moveSpeed, m_cam, m_maxLerpCheck, GetComponent<Rigidbody>(), m_jumpVisual, m_maxClimbEnterAngle, m_normalOffset,
                            m_maxClimbAroundWallAngle);
        m_currentState = walkingState;
        m_currentState.Init();
    }

    // Update is called once per frame
    void Update()
    {
        m_currentState.Update();
    }

    void FixedUpdate()
    {
        m_currentState.FixedUpdate();
    }

    void OnCollisionEnter(Collision hit)
    {
        m_currentState.OnCollisionEnter(hit);
    }

    void OnCollisionStay(Collision hit)
    {
        m_currentState.OnCollisionStay(hit);
    }

    void OnCollisionExit(Collision hit)
    {
        m_currentState.OnCollisionExit(hit);
    }

    void OnDrawGizmos()
    {
        m_currentState.OnDrawGizmos();
    }

    public void SetCamState(PlayerStateInterface state)
    {
        if (state == climbingState)
        {
            m_freelookCam.LookAt = m_climbingCamOffset;
            m_freelookCam.Follow = m_climbingCamOffset;
        }
        else
        {
            m_freelookCam.LookAt = transform;
            m_freelookCam.Follow = transform;
        }
    }
}
