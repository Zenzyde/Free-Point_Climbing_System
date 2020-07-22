using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTester : MonoBehaviour
{
    [SerializeField] private Transform target;

    // Update is called once per frame
    void Update()
    {
        Vector3 look = (target.position - transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(look);
        transform.rotation = rotation;
    }
}
