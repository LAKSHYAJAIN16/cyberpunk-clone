using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class CarController : MonoBehaviour
{
    //Look AT Position
    public Transform CameraFollowPosition;

    //Input
    float x, y;

    //Force Values
    public float accelerateForce = 10f;
    public float TurnSpeed = 1f;
    float offset = 100f;

    //Rigidbody
    private Rigidbody rb;

    //Equipped
    private bool Equipped = false;

    //Friction
    [Range(0f, 1f)]
    public float DynamicFriction, StaticFriction;
    public float CounterMovement;

    //Types
    public Rotation RotationType;
    public Direction DirectionType;

    private void Start()
    {
        //Get Rigidbody and BoxCOl
        rb = GetComponent<Rigidbody>();
        BoxCollider bc = GetComponent<BoxCollider>();
        if (rb == null || bc == null) throw new System.NotImplementedException();

        //Create Physics Material with frictiona and apply it
        PhysicMaterial Mat = new PhysicMaterial();
        Mat.dynamicFriction = DynamicFriction;
        Mat.staticFriction = StaticFriction;
        bc.material = Mat;

        Equip();
    }

    private void FixedUpdate()
    {
        if (Equipped){
            GetInput();
            TurnCar();
            DriveCar();
        }
    }

    public void Equip()
    {
        Equipped = true;
        PlayerMovement.Instance.enabled = false;
        WallRun.Instance.enabled = false;
        MoveCamera.Instance.ChangeTarget(CameraFollowPosition);
        Canvas cancan = FindObjectOfType<Canvas>();
        cancan.transform.gameObject.SetActive(false);
    }

    private void GetInput()
    {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
    }

    private void DriveCar()
    {
        float force = accelerateForce * y * Time.deltaTime * offset;

        //Apply force
        rb.AddForce(GetDirection() * force * offset * Time.deltaTime);
    }

    private void TurnCar()
    {
        //Get Turn Amount
        float t = GetTurnAmount();

        //Get Vector Turn
        Vector3 init = GetTurn(t);

        //Rotate Wheels with that
        Vector3 rot = transform.rotation.eulerAngles, yRot = CameraFollowPosition.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(rot + init);
        CameraFollowPosition.rotation = Quaternion.Euler(yRot + init);
    }

    private float GetTurnAmount()
    {
        float t = TurnSpeed * Time.fixedDeltaTime * offset * x;
        return t;
    }

    private Vector3 GetTurn(float x)
    {
        if (RotationType == Rotation.xAxis) return new Vector3(x, 0f, 0f);
        else if (RotationType == Rotation.yAxis) return new Vector3(0f, x, 0f);
        else if (RotationType == Rotation.zAxis) return new Vector3(0f, 0f, x);

        return (Vector3.zero);
    }

    private Vector3 GetDirection()
    {
        if (DirectionType == Direction.forward) return transform.forward;
        else if (DirectionType == Direction.backwards) return -transform.forward;
        else if (DirectionType == Direction.right) return transform.right;
        else if (DirectionType == Direction.left) return -transform.right;
        else if (DirectionType == Direction.up) return transform.up;
        else if (DirectionType == Direction.down) return -transform.up;

        return Vector3.zero;
    }
}

public enum Rotation
{
    xAxis,
    yAxis,
    zAxis
}

public enum Direction
{
    forward,
    backwards,
    right,
    left,
    up,
    down
}
