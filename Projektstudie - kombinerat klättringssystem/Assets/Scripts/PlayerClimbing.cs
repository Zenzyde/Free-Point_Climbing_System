using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClimbing : PlayerStateInterface
{
    private Vector3 m_moveSpeed;
    private Vector3 move;
    private Transform transform;
    private Camera m_cam;
    private float m_maxLerpCheck, m_maxClimbEnterAngle, m_normalOffset, m_maxClimbAroundAngle;
    private Rigidbody m_rigid;
    private Transform m_jumpVisual;

    private PlayerMovementController m_moveBase;

    private bool m_hasInitiated, m_hasExited, m_jumpDownToFloorOverride, m_isLerping;
    private Vector3 m_lastMove;
    private Transform wall;

    Vector3 start;
    Vector3 b;
    Vector3 a;
    Vector3 c;
    Vector3 target;

    public PlayerClimbing(PlayerMovementController moveBase, Transform transform, Vector3 moveSpeed, Camera cam, float lerpCheck, Rigidbody rigid, Transform jumpVisual,
    float climbEnterAngle, float normalOffset, float climbAroundAngle)
    {
        this.m_moveBase = moveBase;
        this.transform = transform;
        this.m_moveSpeed = moveSpeed;
        this.m_cam = cam;
        this.m_maxLerpCheck = lerpCheck;
        this.m_rigid = rigid;
        this.m_jumpVisual = jumpVisual;
        this.m_maxClimbEnterAngle = climbEnterAngle;
        this.m_normalOffset = normalOffset;
        this.m_maxClimbAroundAngle = climbAroundAngle;
    }

    public void Init()
    {
        m_hasExited = false;
        m_hasInitiated = true;
    }

    public bool HasBeenInitiated()
    {
        return m_hasInitiated;
    }

    public void Update()
    {
        Movement();
        NullifyRigid();
        VisualizeJumpVisual();
    }

    public void FixedUpdate()
    {

    }

    void Movement()
    {
        move = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            move += transform.up * m_moveSpeed.z;
        }
        if (Input.GetKey(KeyCode.S))
        {
            move -= transform.up * m_moveSpeed.z;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                m_lastMove = move.normalized;
                JumpDown();
                return;
            }
        }
        if (Input.GetKey(KeyCode.A))
        {
            move -= transform.right * m_moveSpeed.x;
        }
        if (Input.GetKey(KeyCode.D))
        {
            move += transform.right * m_moveSpeed.x;
        }

        m_lastMove = move.normalized;

        TryEnterWalkingState();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            DetachFromWall();
            return;
        }

        if (!CheckForWallRays())
        {
            if (Vector3.Dot(move, transform.up) >= 0.9f)
            {
                bool floorFound = CheckForFloor();
                if (floorFound)
                {
                    AlignToFloor();
                    return;
                }
            }
            bool attemptStatus = TryGoAroundWall();
            if (!attemptStatus)
                return;
        }
        else
        {
            AlignToWall();
            transform.position += move * Time.deltaTime;
        }
    }

    void NullifyRigid()
    {
        m_rigid.velocity = Vector3.zero;
        m_rigid.angularVelocity = Vector3.zero;
    }

    void VisualizeJumpVisual()
    {
        Vector3 target = DetermineClosestLerpableWall();//CheckForLerpableWallOrFloor();
        if (target == Vector3.zero)
        {
            m_jumpVisual.gameObject.SetActive(false);
        }
        else
        {
            m_jumpVisual.gameObject.SetActive(true);
            m_jumpVisual.position = target;
        }
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

    public void OnCollisionEnter(Collision other)
    {
        if (other.transform.CompareTag("Floor"))
        {
            //move = Vector3.zero;
            // Vector3 camForward = Vector3.ProjectOnPlane(m_cam.transform.forward, Vector3.up);
            // Quaternion look = Quaternion.LookRotation(camForward, Vector3.up);
            // transform.rotation = look;
            //ChangeState(m_moveBase.walkingState);
        }
        else if (other.transform.CompareTag("Climb"))
        {
            // Vector3 point = other.GetContact(0).point;
            // Vector3 normal = other.GetContact(0).normal;
            // if (!VerifyWallEnterAngle(normal))
            //     return;
            // move = Vector3.zero;
            // ReorientToNewWall(point, normal);
        }
    }

    public void OnCollisionStay(Collision hit)
    {

    }

    public void OnCollisionExit(Collision hit)
    {

    }

    bool CheckForWallRays()
    {
        Ray rayDirection = new Ray(transform.position, move.normalized);
        Vector3 playerToForward = (transform.position + (transform.position + move.normalized + transform.forward)) / 2.0f;
        Vector3 angledDirection = (playerToForward - (transform.position + move.normalized)).normalized;
        Ray rayAngled = new Ray(transform.position + move.normalized, angledDirection);
        RaycastHit hit;
        if (Physics.Raycast(rayDirection.origin, rayDirection.direction, out hit, 1f))
        {
            if (hit.transform.CompareTag("Climb"))// && VerifyWallClimbEnteringAngle(hit.normal))
            {
                return true;
            }
        }
        if (Physics.Raycast(rayAngled.origin, rayAngled.direction, out hit, 1f))
        {
            if (hit.transform.CompareTag("Climb"))// && VerifyWallClimbEnteringAngle(hit.normal))
            {
                return true;
            }
        }
        transform.position -= move * Time.deltaTime;
        return false;
    }

    bool CheckForFloor()
    {
        Ray rayTopDown = new Ray(transform.position + transform.up * 1.2f + transform.forward * 0.5f, -transform.up);
        RaycastHit hit;

        //Safety check in case there is something above, blocking the character from climbing up
        if (Physics.Raycast(transform.position, transform.up, 1.2f))
        {
            return false;
        }

        if (Physics.Raycast(rayTopDown.origin, rayTopDown.direction, out hit, 1.2f))
        {
            if (Vector3.Dot(Vector3.up, hit.normal) >= 0.9f)
            {
                return true;
            }
        }
        transform.position -= move * Time.deltaTime;
        return false;
    }

    void ReorientToNewWall(Vector3 point, Vector3 normal)
    {
        //Faster fix, why project vector onto a plane when you can make a new vector with a proper y-value & use that vector for direction-calculation instead
        //Vector3 newPoint = new Vector3(point.x, transform.position.y, point.z);
        Vector3 newDir = (point - transform.position).normalized;
        Debug.DrawRay(transform.position, newDir, Color.magenta, 1000f);
        //Fast fix because hit.point wasn't checked from the transforms positional y-value
        //Secondly, works better because it covers slanted walls better by aligning the direction with the wall-normal instead of the up-vector
        newDir = Vector3.Project(newDir, normal);
        Debug.DrawRay(transform.position, newDir, Color.cyan, 1000f);
        Quaternion lookRotation = Quaternion.LookRotation(newDir);
        transform.rotation = lookRotation;
        //Changed to the exact point to cover angled walls
        //Subtract the direction because it's pointing the wrong way
        Vector3 newPos = point + (-newDir * (1.0f - m_normalOffset));//new Vector3(point.x, transform.position.y, point.z);
        transform.position = newPos + transform.up * 0.5f;
    }

    //Raycast replacement for ReorientToNewWall ^
    void AlignToWall()
    {
        Ray rayDirection = new Ray(transform.position, move.normalized);
        Ray rayForward = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(rayDirection, out hit, 1.1f))
        {
            if (hit.transform.CompareTag("Climb"))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) < 1.0f)
                {
                    Quaternion look = Quaternion.LookRotation(-hit.normal, hit.transform.up);
                    transform.rotation = look;
                    transform.position = hit.point + hit.normal * 0.5f;
                    if (hit.transform != wall)
                        wall = hit.transform;
                    return;
                }
            }
        }
        if (Physics.Raycast(rayForward, out hit, 1f))
        {
            if (hit.transform.CompareTag("Climb"))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) < 1.0f)
                {
                    Quaternion look = Quaternion.LookRotation(-hit.normal, hit.transform.up);
                    transform.rotation = look;
                    transform.position = hit.point + hit.normal * 0.5f;
                    if (hit.transform != wall)
                        wall = hit.transform;
                    return;
                }
            }
        }
    }

    void AlignToFloor()
    {
        Ray rayTopDown = new Ray(transform.position + transform.up * 1.2f + transform.forward * 0.7f, -transform.up);
        RaycastHit hit;
        if (Physics.Raycast(rayTopDown, out hit))
        {
            if (Vector3.Dot(Vector3.up, hit.normal) >= 0.9f)
            {
                Vector3 forwards = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
                Quaternion up = Quaternion.LookRotation(forwards, Vector3.up);
                transform.rotation = up;
                transform.position = hit.point + hit.normal;
                ChangeState(m_moveBase.walkingState);
            }
        }
    }

    void TryEnterWalkingState()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1f))
        {
            if (hit.transform.CompareTag("Climb") && Vector3.Dot(move, Vector3.down) < 0.7f)
            {
                return;
            }
        }
        if (Physics.Raycast(transform.position, -transform.up, out hit, 1.2f))
        {
            if (hit.transform.CompareTag("Floor"))
            {
                Vector3 forwards = Vector3.ProjectOnPlane(m_cam.transform.forward, Vector3.up);
                Quaternion up = Quaternion.LookRotation(forwards, Vector3.up);
                transform.rotation = up;
                transform.position = hit.point + hit.normal;
                move = Vector3.zero;
                m_jumpVisual.gameObject.SetActive(false);
                ChangeState(m_moveBase.walkingState);
            }
        }
    }

    bool TryGoAroundWall()
    {
        RaycastHit hit;
        Ray rayDirection = new Ray(transform.position, move.normalized);
        Vector3 playerToForward = (transform.position + (transform.position + move.normalized + transform.forward)) / 2.0f;
        Vector3 angledDirection = (playerToForward - (transform.position + move.normalized)).normalized;
        Ray rayAngled = new Ray(transform.position + move.normalized, angledDirection);

        if (Physics.Raycast(rayDirection, out hit, 1f))
        {
            //If normal compared to player forward isn't too steep
            if (VerifyClimbAroundWallAngle(hit.normal))//Vector3.Dot(transform.forward, hit.normal) < 0.75f)
            {
                transform.position = Vector3.Lerp(transform.position, hit.point + hit.normal * m_normalOffset, 0.7f);
                Quaternion look = Quaternion.LookRotation(-hit.normal, transform.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 0.7f);
                return true;
            }
        }
        if (Physics.Raycast(rayAngled, out hit, 1f))
        {
            //If normal compared to player forward isn't too steep
            if (VerifyClimbAroundWallAngle(hit.normal))//Vector3.Dot(transform.forward, hit.normal) < 0.75f)
            {
                transform.position = Vector3.Lerp(transform.position, hit.point + hit.normal * m_normalOffset, 0.7f);
                Quaternion look = Quaternion.LookRotation(-hit.normal, transform.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 0.7f);
                return true;
            }
        }
        return false;
    }

    bool VerifyWallClimbEnteringAngle(Vector3 normal)
    {
        return Vector3.Dot(transform.up, normal) >= m_maxClimbEnterAngle;
    }

    bool VerifyClimbAroundWallAngle(Vector3 normal)
    {
        return Vector3.Dot(transform.forward, -normal) >= m_maxClimbAroundAngle;
    }

    public void ChangeState(PlayerStateInterface newState)
    {
        m_moveBase.SetCamState(newState);
        m_moveBase.m_currentState = newState;
    }

    public void OnDrawGizmos()
    {
        Ray rayDirection = new Ray(transform.position, move.normalized);
        Vector3 playerToForward = (transform.position + (transform.position + move.normalized + transform.forward)) / 2.0f;
        Vector3 angledDirection = (playerToForward - (transform.position + move.normalized)).normalized;
        Ray rayAngled = new Ray(transform.position + move.normalized, angledDirection);
        Gizmos.DrawRay(rayDirection.origin, rayDirection.direction);
        Gizmos.DrawRay(rayAngled.origin, rayAngled.direction);

        Gizmos.color = Color.cyan;
        KeyValuePair<float, Vector3>[] points = ScanForLerpableWallDirections().ToArray();
        if (points.Length != 0)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawLine(transform.position, points[i].Value);
            }
        }

        Gizmos.color = Color.magenta;
        Vector3 closest = DetermineClosestLerpableWall();
        if (closest != Vector3.zero)
            Gizmos.DrawSphere(closest, 1f);

        // if (m_useSpherecastApproach)
        // {
        //     Gizmos.color = Color.cyan;
        //     KeyValuePair<Vector3, Vector3>[] points = ScanForLerpableWallDirections().ToArray();
        //     if (points.Length != 0)
        //     {
        //         for (int i = 0; i < points.Length; i++)
        //         {
        //             Gizmos.DrawLine(transform.position, points[i].Value);
        //         }
        //     }

        //     Gizmos.color = Color.magenta;
        //     Vector3 closest = DetermineClosestLerpableWall();
        //     if (closest != Vector3.zero)
        //         Gizmos.DrawSphere(closest, 1f);
        // }
        // else
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawLine(transform.position, CheckForLerpableWallOrFloor());
        //     if (target != Vector3.zero)
        //         Gizmos.DrawCube(target, Vector3.one);
        // }
    }

    void JumpDown()
    {
        if (m_isLerping)
            return;
        if (Vector3.Dot(transform.forward, m_cam.transform.forward) >= 0.75f)
        {
            m_jumpDownToFloorOverride = true;
            //transform.position -= transform.forward * 1.5f;
            RaycastHit hit;
            if (Physics.Raycast(transform.position - transform.forward * 2f, Vector3.down, out hit))
            {
                m_moveBase.StartCoroutine(Co_LerpToWall((hit.point + hit.normal * m_normalOffset)));
            }
            else
            {
                m_moveBase.StartCoroutine(Co_LerpToWall((transform.position - transform.forward * 5f - Vector3.up * 2f)));
            }
        }
    }

    void DetachFromWall()
    {
        if (m_isLerping)
            return;
        if (Vector3.Dot(transform.forward, m_cam.transform.forward) < 0.75f)
        {

        }
        Vector3 camDir = m_cam.transform.forward;//Vector3.Dot(transform.right, m_cam.transform.forward) > 0.0f ? m_cam.transform.right : -m_cam.transform.right;

        //transform.position += camDir * 2.5f;

        m_moveBase.StartCoroutine(Co_LerpToWall((transform.position + camDir * 5f)));
    }

    #region Sphere-Raycast-Assassin-Merge

    #region Sphere-Assassin

    //For more Assassin's Creed-esqué wall-jumping/lerping
    List<KeyValuePair<float, Vector3>> ScanForLerpableWallDirections()
    {
        Collider[] colliders = Physics.OverlapSphere(m_cam.transform.position, m_maxLerpCheck);
        if (colliders.Length == 0)
            return null;
        List<KeyValuePair<float, Vector3>> lerpPoints = new List<KeyValuePair<float, Vector3>>();

        Vector3 desiredMovement = Vector3.ProjectOnPlane(m_lastMove, m_cam.transform.forward).normalized;
        Vector3 positionForDesiredMeasurement = transform.position + desiredMovement;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (Vector3.Distance(transform.position, colliders[i].ClosestPoint(transform.position)) <= 1.0f)
            {
                continue;
            }
            if (colliders[i].CompareTag("Climb") || colliders[i].CompareTag("Floor"))
            {
                //Consider setting the closest point to the camera instead of the player
                //Changed in favor of checking the distance & finding the closest point with that instead fo checking dot-product since it's finicky as heck

                Vector3 closestPoint = colliders[i].ClosestPoint(transform.position);
                if (IsPointObstructed(closestPoint, colliders[i]))
                {
                    closestPoint = TryFindClosestUnobstructedPoint(colliders[i]);
                }

                lerpPoints.Add(
                    new KeyValuePair<float, Vector3>(
                        (closestPoint - positionForDesiredMeasurement).magnitude, closestPoint));
            }
        }
        return lerpPoints;
    }

    bool IsPointObstructed(Vector3 point, Collider collider)
    {
        Ray rayToPoint = new Ray(transform.position, (point - transform.position).normalized);
        RaycastHit hit;
        Physics.Raycast(rayToPoint, out hit);
        return hit.transform == collider.transform ? false : true;
    }

    Vector3 TryFindClosestUnobstructedPoint(Collider collider)
    {
        Vector3 closestPoint = collider.ClosestPoint(transform.position);
        Vector3 colliderPoint = collider.ClosestPoint(collider.transform.position);
        while (Vector3.Distance(colliderPoint, closestPoint) > 0.25f && IsPointObstructed(closestPoint, collider))
        {
            closestPoint = Vector3.MoveTowards(closestPoint, colliderPoint, 1f);
        }
        return closestPoint;
    }

    //For getting the closest lerpable wall for the more Assassin's Creed-esqué approach
    Vector3 DetermineClosestLerpableWall()
    {
        KeyValuePair<float, Vector3>[] possibleLerpDistances = ScanForLerpableWallDirections().ToArray();
        if (possibleLerpDistances.Length == 0 || possibleLerpDistances == null)
            return Vector3.zero;
        //Vector3 desiredMovement = Vector3.ProjectOnPlane(m_lastMove, m_cam.transform.forward).normalized;
        float closestDistance = Mathf.Infinity;
        int closestIndex = 0;

        //Sphere/Raycast-Assassin-Merge START
        //Assume player is looking for a manual wall/floor-position since player is facing away from the wall quite a bit
        // if (Vector3.Dot(m_cam.transform.forward, transform.forward) <= 0.25f)
        // {
        //     return CheckForLerpableWallOrFloor();
        // }
        //ALT: do a raycast & check if the hit is the same wall as the player is currently on, if it is then do the for-loop below, otherwise set the hit as the target
        //Going with the alt for now
        RaycastHit hit;
        if (Physics.Raycast(m_cam.transform.position, m_cam.transform.forward, out hit))
        {
            if (hit.transform != wall)
            {
                return CheckForLerpableWallOrFloor();
            }
        }
        //Sphere/Raycast-Assassin-Merge PAUSE

        for (int i = 0; i < possibleLerpDistances.Length; i++)
        {
            //Sphere/Raycast-Assassin-Merge RESUME
            /*if (Vector3.Dot(desiredMovement, transform.forward) <= -0.5f)
            {
                //Assume player wants to move/jump/lerp backwards since the camera is focused a great deal away from the wall, most likely to look at the wall behind the player
                if (Vector3.Dot(possibleLerpDirections[i].Key, desiredMovement) > 0.5f)
                {
                    closestIndex = i;
                    break;
                }
                continue;
            }
            else */
            //Sphere/Raycast-Assassin-Merge END
            //Changed in favor of checking closest distance to a point in a direction instead of checking the dot-product
            //if (Vector3.Distance(possibleLerpDistances[i].Key, desiredMovement) > closestDistance || closestDistance == -1f)
            if (possibleLerpDistances[i].Key < closestDistance)
            {
                //Assume player is looking more towards the wall & use the desiredMovement to judge where the player wants to move
                //closestDistance = Vector3.Dot(possibleLerpDistances[i].Key, desiredMovement);
                closestDistance = possibleLerpDistances[i].Key;
                closestIndex = i;
            }
        }
        return possibleLerpDistances[closestIndex].Value;
    }

    #endregion

    #region Raycast-Assassin

    Vector3 CheckForLerpableWallOrFloor()
    {
        if (Vector3.Dot(transform.forward, m_cam.transform.forward) >= 0.75f)
            return Vector3.zero;
        Vector3 camBasedDir = m_cam.transform.forward;
        //Vector3.Dot(transform.right, m_cam.transform.forward) > 0.0f ? m_cam.transform.right : -m_cam.transform.right;
        //Vector3.ProjectOnPlane(move.normalized, m_cam.transform.forward);
        Ray ray = new Ray(m_cam.transform.position, m_cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, m_maxLerpCheck))
        {
            if (hit.transform.CompareTag("Climb") && Vector3.Distance(transform.position, hit.collider.ClosestPoint(transform.position)) > 1.0f)
            {
                return hit.point + hit.normal * 0.2f;
            }
            if (hit.transform.CompareTag("Floor"))
            {
                return hit.point + hit.normal * 0.2f;
            }
        }
        return Vector3.zero;
    }

    #endregion

    #endregion

    bool IsSameWall(RaycastHit hit)
    {
        return Vector3.Distance(hit.collider.ClosestPoint(transform.position), transform.position) <= 1.0f && hit.transform.CompareTag("Climb");
    }

    void DetermineBezierSpots(Vector3 _target)
    {
        //Resets before calculations
        start = Vector3.zero;
        a = Vector3.zero;
        b = Vector3.zero;
        c = Vector3.zero;
        target = Vector3.zero;

        //Check for & create endpoint
        //Vector3 endPoint = m_useSpherecastApproach ? DetermineClosestLerpableWall() : CheckForLerpableWallOrFloor();
        Vector3 endPoint = DetermineClosestLerpableWall();
        endPoint = endPoint == Vector3.zero || m_jumpDownToFloorOverride ? _target : endPoint;

        //Mark whether a wall has been detected or not
        //m_detectedWall = endPoint == _target ? false : true;

        //Start point -> start
        start = transform.position;

        //Middle point -> b
        Vector3 halfway = ((start + endPoint) / 2.0f);
        b = halfway + Vector3.up * 2.0f;

        //Target point -> target
        target = endPoint;
    }

    IEnumerator Co_LerpToWall(Vector3 _target)
    {
        float t = 0.0f;
        DetermineBezierSpots(_target);
        m_isLerping = true;

        Quaternion look = Quaternion.LookRotation((_target - transform.position).normalized);
        transform.rotation = look;

        while (Vector3.Distance(transform.position, c) > 0.25f && t < 1.0f)
        {
            t += Time.deltaTime;
            a = Vector3.Lerp(start, b, t);
            c = Vector3.Lerp(b, target, t);
            transform.position = Vector3.Lerp(a, c, t);
            yield return null;
        }
        target = Vector3.zero;
        m_jumpDownToFloorOverride = false;
        m_isLerping = false;
        TryEnterWalkingState();
        // if (!m_detectedWall)
        //     ChangeState(m_moveBase.walkingState);
        // else
        //     m_detectedWall = false;
        //m_detectedWall = false;
    }
}
