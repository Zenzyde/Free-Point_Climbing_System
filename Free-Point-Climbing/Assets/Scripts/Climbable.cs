using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Climbable : MonoBehaviour
{
    [SerializeField] private Climbable[] nearbyWalls;

    public Climbable[] NearbyWalls { get { return nearbyWalls; } }

    public Vector3[] GetNearbyDirections(Vector3 player)
    {
        Vector3[] directions = new Vector3[nearbyWalls.Length];
        for (int i = 0; i < nearbyWalls.Length; i++)
        {
            directions[i] = (nearbyWalls[i].transform.position - player).normalized;
        }
        return directions;
    }
}
