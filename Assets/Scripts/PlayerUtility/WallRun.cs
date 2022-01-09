using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Player;

public class WallRun : MonoBehaviour
{
    public static WallRun Instance { get; set; }
    [Header("Other")]
    public Transform orientation;
    public PlayerMovement playerMovement;

    [Header("Detection")]
    public LayerMask whatIsWall;
    public float wallDistance = 1f;

    [Header("Wall Running")]
    public float wallRunGravity = 3f;
    public float wallRunJumpForce = 4f;
    public float wallHopForce = 15f;
    public float WallRunAheadForce = 12f;
    public float WallStickForce = 500f;
    public float maxSpeed = 30;
    public float maxWallrunAngle = 35f;
    public float wallJumpCooldown = 0.25f;
    public float hopCooldown = 0.25f;

    //Input
    private float x;
    private float y;

    [Header("CounterWallMovement")]
    public float LeftAndRight = 10f;
    public float UpAndDown = 10f;
    public float LandCounter = 5f;
    public float Sensitivity = 100f;
    [Range(0, 10)]
    public float Smoothness = 10f;
    [Range(0, 1)]
    public float dampness;


    //Cam stuff
    [Header("Camera")]
    public Transform cameraHolder;
    public Camera cam;
    public float tilt { get; private set; }
    private float upwardsAndDownwardsTilt;
    public float maxcamTilt = 20f;
    public float camTiltSpeed = 2f;

    //bools to check which side r we on
    private bool wallLeft = false;
    private bool wallRight = false;
    private bool wallAhead = false;
    private bool wallBack = false;

    //More Bools aaaaaaaaaaaaaaaaaah
    [HideInInspector] public bool isWallrunning = false;
    private bool canWallrun;
    private bool cancellingwall;
    private bool canwallJump = true;
    private bool canHop = true;

    //RaycastHits
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    RaycastHit aheadWallHit;
    RaycastHit backWallHit;

    private Rigidbody rb;

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
    }

    void CheckWall()
    {
        //CheckBooleans to make sure we can wallrun;
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance);
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance);
        wallAhead = Physics.Raycast(transform.position, orientation.forward, out aheadWallHit, wallDistance);
        wallRight = Physics.Raycast(transform.position, orientation.right, out backWallHit, wallDistance);

        //Stuff idduno man
        if (canWallrun)
        {
            //We Don't want to Wallrun if we're grounded
            if (playerMovement.Grounded()){
                StopWallRun();
            }
            else if (!playerMovement.Grounded()){
                if (wallAhead || wallBack || wallLeft || wallRight) StartWallRun();

                else StopWallRun();
            }
        }
        else
        {
            StopWallRun();
        }
    }

    private void Update()
    {
        CheckWall();
        CheckForWallRunInput();
    }

    private void CheckForWallRunInput()
    {
        //Input axis
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        //Get the Vector
        Vector2 mag = PlayerMovement.Instance.FindVelRelativeToLook();
        float xMag = mag.x;
        float yMag = mag.y;

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Hop stuff
        if (!canHop) return;

        //SidewardsWallHop
        if (isWallrunning && wallLeft && Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.Space)) WallHopLeft();
        if (isWallrunning && wallRight && Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.Space)) WallHopRight();

        //Upwards WallHop
        if (isWallrunning && wallAhead && Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.Space)) WallHopAhead();
        if (isWallrunning && wallAhead && Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.Space)) WallHopBack();
    }

    private void WallHopLeft()
    {
        rb.AddForce(-orientation.up * wallHopForce * Time.deltaTime);
        rb.AddForce(-orientation.right * wallHopForce * Time.deltaTime * 100f);
        canHop = false;
        Invoke(nameof(ResetHop), hopCooldown);

    }

    private void WallHopRight()
    {
        rb.AddForce(-orientation.up * wallHopForce * Time.deltaTime);
        rb.AddForce(orientation.right * wallHopForce * Time.deltaTime * 100f);
        canHop = false;
        Invoke(nameof(ResetHop), hopCooldown);
    }

    private void WallHopAhead()
    {
        rb.AddForce(-orientation.up * wallHopForce * Time.deltaTime);
        rb.AddForce(orientation.forward * wallHopForce * Time.deltaTime * 100f);
        canHop = false;
        Invoke(nameof(ResetHop), hopCooldown);
    }

    private void WallHopBack()
    {
        rb.AddForce(-orientation.up * wallHopForce * Time.deltaTime);
        rb.AddForce(-orientation.forward * wallHopForce * Time.deltaTime * 100f);
        canHop = false;
        Invoke(nameof(ResetHop), hopCooldown);
    }

    void ResetHop()
    {
        canHop = true;
    }

    void Counter()
    {
        //Our current Speed
        float speed = Math.Abs(rb.velocity.magnitude);

        //Some Multipliers
        float multiplier = 0f;
        float multiplierV = 0f;

        //Calculations regarding Multipliers
        if (!canHop && !canwallJump)
        {
            multiplier = 1f;
            multiplierV = 0.5f;
        }
        if (canwallJump && canHop)
        {
            multiplier = 2f;
            multiplierV = 1f;
        }
        if (!canHop && canwallJump || !canwallJump && canHop)
        {
            multiplier = 1.5f;
            multiplierV = 0.75f;
        }

        //We calculate our final speed for calculations
        float currentSpeed = speed;

        //Now a bunch of if statements
        if (currentSpeed >= 5f)
        {
            if (wallRight || wallLeft) rb.AddForce(-orientation.up * Time.deltaTime * LeftAndRight * 2 * 100f * multiplier);
            if (wallAhead || wallBack) rb.AddForce(-orientation.up * Time.deltaTime * UpAndDown * 2 * 100f * multiplierV) ;
            return;
        }
        if (currentSpeed >= 3f)
        {
            if (wallRight || wallLeft) rb.AddForce(-orientation.up * Time.deltaTime * LeftAndRight * 100f * multiplier);
            if (wallAhead || wallBack) rb.AddForce(-orientation.up * Time.deltaTime * UpAndDown * 100f * multiplierV);
            return;
        }
        if (currentSpeed >= 2f)
        {
            if (wallRight || wallLeft) rb.AddForce(-orientation.up * Time.deltaTime * LeftAndRight / 2 * 100f * multiplier);
            if (wallAhead || wallBack) rb.AddForce(-orientation.up * Time.deltaTime * UpAndDown / 2 * 100f * multiplierV);
            return;
        }
        if (currentSpeed >= 1f)
        {
            if (wallRight || wallLeft) rb.AddForce(-orientation.up * Time.deltaTime * LeftAndRight / 4 * 100f * multiplier);
            if (wallAhead || wallBack) rb.AddForce(-orientation.up * Time.deltaTime * UpAndDown / 4 * 100f * multiplierV);
            return;
        }

        else
        {
            if (wallRight || wallLeft) rb.AddForce(-orientation.up * Time.deltaTime * LeftAndRight / 10 * 100f);
            if (wallAhead || wallBack) rb.AddForce(-orientation.up * Time.deltaTime * UpAndDown / 10 * 100f);
            return;
        }
    }

    void StartWallRun()
    {
        rb.useGravity = false;
        isWallrunning = true;
        CheckForWallRotation();

        //Force Calculations
        if (wallLeft || wallRight)
        {
            //Always add some downwardsforce if we're wallrunning left to right
            rb.AddForce(-orientation.up * wallRunGravity, ForceMode.Force);

            //Also Some Forwards Force so that you actually move XD
            rb.AddForce(orientation.forward * WallRunAheadForce * Time.deltaTime * 100f);
        }

        //Fixing da bugs hahahaha   
        if (wallRight)
        {
            isWallrunning = true;
            canWallrun = true;
            CancelInvoke(nameof(StopWall));
        }

        //WallrunJump
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!canwallJump) return;
            else if (canwallJump) WallJump();
        }

        if (wallRight)
        {
            isWallrunning = true;
            canWallrun = true;
        }

        if (wallRight)
        {
            isWallrunning = true;
            canWallrun = true;
        }
    }

    void CheckForWallRotation()
    {
        if (wallLeft)
            tilt = Mathf.Lerp(tilt, -maxcamTilt, camTiltSpeed * Time.deltaTime);
        else if (wallRight)
            tilt = Mathf.Lerp(tilt, maxcamTilt, camTiltSpeed * Time.deltaTime);
        else if (wallAhead)
            upwardsAndDownwardsTilt = Mathf.Lerp(upwardsAndDownwardsTilt, maxcamTilt, camTiltSpeed * Time.deltaTime);
        else if (wallBack)
            upwardsAndDownwardsTilt = Mathf.Lerp(upwardsAndDownwardsTilt, -maxcamTilt, camTiltSpeed * Time.deltaTime);
    }

    void WallJump()
    {
        if (wallLeft)
        {
            Vector3 wallRunJumpDirection = transform.up + leftWallHit.normal;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(wallRunJumpDirection * wallRunJumpForce * 100, ForceMode.Force);
        }
        else if (wallRight)
        {
            Vector3 wallRunJumpDirection = transform.up + rightWallHit.normal;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(wallRunJumpDirection * wallRunJumpForce * 100, ForceMode.Force);
        }
        else if (wallAhead)
        {
            //Multipliers
            float multiplier = 1.5f;
            Vector3 wallRunJumpDirection = transform.up + aheadWallHit.normal;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(wallRunJumpDirection * wallRunJumpForce * 100 * multiplier, ForceMode.Force);
        }
        else if (wallBack)
        {
            //Multipliers
            float multiplier = 1.5f;
            Vector3 wallRunJumpDirection = transform.up + backWallHit.normal;
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(wallRunJumpDirection * wallRunJumpForce * 100 * multiplier, ForceMode.Force);
        }

        //Invoke Reset Since we can't calculate normals with Unity's refresh
        canwallJump = false;
        Invoke(nameof(ResetJump), wallJumpCooldown);

        Invoke(nameof(Check), 0.2f);

        //loop through this statement x number of times
        rb.AddForce(-orientation.up * wallRunJumpForce / 3);
        rb.AddForce(-orientation.up * wallRunJumpForce / 3);
        rb.AddForce(-orientation.up * wallRunJumpForce / 3);
        rb.AddForce(-orientation.up * wallRunJumpForce / 3);

    }

    void Check()
    {
        // Add some downwards force in the left to right ones
        if (wallLeft && !wallRight) rb.AddForce(-orientation.up * wallRunJumpForce / 3);
        if (!wallLeft && wallRight) rb.AddForce(-orientation.up * wallRunJumpForce / 3);
    }

    void ResetJump()
    {
        canwallJump = true;
    }

    void StopWallRun()
    {
        rb.useGravity = true;
        isWallrunning = false;

        //Lerps hahahahahaha
        tilt = Mathf.Lerp(tilt, 0, camTiltSpeed * Time.deltaTime);
        upwardsAndDownwardsTilt = Mathf.Lerp(upwardsAndDownwardsTilt, 0, camTiltSpeed * Time.deltaTime);
    }

    /// <summary>
    /// WallCheck
    /// </summary>
    void OnCollisionStay(Collision other)
    {
        int layer = other.gameObject.layer;
        if (whatIsWall != (whatIsWall | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            //Wall detection
            if (IsWall(normal))
            {
                canWallrun = true;
                cancellingwall = false;
                CancelInvoke(nameof(StopWall));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingwall)
        {
            cancellingwall = true;
            Invoke(nameof(StopWall), Time.deltaTime * delay);
        }
    }

    bool IsWall(Vector3 v)
    {
        return maxWallrunAngle > 0;
    }

    void StopWall()
    {
        canWallrun = false;
    }

    //Bools to return;
    public bool isOnWall()
    {
        return isWallrunning;
    }

    public bool isWallLeft()
    {
        return wallLeft;
    }

    public bool isWallRight()
    {
        return wallRight;
    }

    public bool isWallAhead()
    {
        return wallAhead;
    }

    public bool isWallBack()
    {
        return wallBack;
    }

    public bool iswall()
    {
        return canWallrun;
    }
}