using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public static MoveCamera Instance { get; set; }
    public Transform FollowPoint;
    private void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = FollowPoint.position;   
    }

    public void ChangeTarget(Transform wt)
    {
        FollowPoint = wt;
        transform.rotation = wt.rotation;
    }
}
