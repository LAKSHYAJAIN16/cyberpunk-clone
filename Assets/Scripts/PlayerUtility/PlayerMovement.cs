using System;
using UnityEngine;
using Player;

namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        //Instance
        public static PlayerMovement Instance { get; set; }

        //Assingables
        public Transform playerCam;
        public Transform orientation;

        //Other
        private Rigidbody rb;

        //Rotation and look
        private float xRotation;
        private float sensitivity = 50f;
        private float normSensitivity;
        private float sensMultiplier = 1f;

        //Movement
        public float moveSpeed = 4500;
        public float maxSpeed = 20;
        public bool grounded;

        public float counterMovement = 0.175f;
        private float threshold = 0.01f;
        public float maxSlopeAngle = 35f;

        //Jumping
        private bool readyToJump = true;
        private float jumpCooldown = 0.25f;
        public float jumpForce = 550f;
        private float jumpMultiplier = 1f;

        //Input
        float x, y;
        bool jumping, sprinting;

        //Sliding
        private Vector3 normalVector = Vector3.up;
        private Vector3 wallNormalVector;

        //Sprinting
        public float sprintFOVIncrement = 20f, sprintSpeed = 10f, fovLerpSpeed = 2f;
        float currentFOV, normalFOV, sprintMaxFOV;
        private Camera mainCam;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            Instance = this;
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            mainCam = Camera.main;
            normalFOV = mainCam.fieldOfView;
            sprintMaxFOV = normalFOV + sprintFOVIncrement;
            currentFOV = normalFOV;
            normSensitivity = sensitivity * sensMultiplier;
        }


        private void FixedUpdate()
        {
            Movement();
        }

        private void Update()
        {
            MyInput();
            Look();
        }

        /// <summary>
        /// Find user input. Should put this in its own class but im lazy
        /// </summary>
        private void MyInput()
        {
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
            jumping = Input.GetButton("Jump");
            sprinting = Input.GetKey(KeyCode.LeftControl);
        }

        private void Movement()
        {
            //Extra gravity just to make sure that the ground check works cause Unity is kinda buggy
            rb.AddForce(Vector3.down * Time.deltaTime * 10);

            //Find actual velocity relative to where player is looking
            Vector2 mag = FindVelRelativeToLook();
            float xMag = mag.x, yMag = mag.y;

            //Counteract sliding and sloppy movement
            CounterMovement(x, y, mag);

            //If we arfe sprinting and holding jump AND we are ready to jump, increase jumpMultiplier
            if (readyToJump && jumping && sprinting) jumpMultiplier = 1.5f;

            //If holding jump && ready to jump, then jump
            if (readyToJump && jumping) Jump();

            //Get Raw Velocity
            Vector3 vel = GetVelocity();

            //Set values
            float xVel = vel.x, yVel = vel.y;

            //Some Max Speed multipliers
            float sprintMax = 1f, maxMulti = 1f;

            //If our yVel AND xVel are zero, cancel out input
            if (xVel == 0f && yVel == 0f) maxMulti = 0f;

            //If we are sprinting and not grounded, apply extra gravity to counteract movement
            if (sprinting && !grounded) rb.AddForce(-Vector3.up * 30f * Time.deltaTime * Math.Abs(yVel));

            //If we are sprinting, increase max speed
            if (sprinting && Math.Abs(yMag) > 0f && Math.Abs(xMag) > 0f || sprinting && Math.Abs(threshold * yVel) > xVel) sprintMax = sprintSpeed;

            //Set final max speed
            float maxSpeed = this.maxSpeed * maxMulti * sprintMax;

            //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
            if (x > 0 && xMag > maxSpeed) x = 0;
            if (x < 0 && xMag < -maxSpeed) x = 0;
            if (y > 0 && yMag > maxSpeed) y = 0;
            if (y < 0 && yMag < -maxSpeed) y = 0;

            //Some multipliers
            float multiplier = 1f, multiplierV = 1f, sprintMultiplier = 1f;

            // Movement in air
            if (!grounded)
            {
                multiplier = 0.5f;
                multiplierV = 0.5f;
            }

            //If not grounded And Sprinting, limit movement QUITE A LOT
            if (!grounded && sprinting)
            {
                multiplier = 0.2f;
                multiplierV = 0.2f;
            }

            //Movement while sprinting
            if (sprinting)
                sprintMultiplier = sprintSpeed;

            //Apply forces to move player
            rb.AddForce(orientation.transform.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV * sprintMultiplier);
            rb.AddForce(orientation.transform.right * x * moveSpeed * Time.deltaTime * multiplier * sprintMultiplier);

            //FOV calcs
            if (sprinting)
                currentFOV = Mathf.Lerp(currentFOV, sprintMaxFOV, Time.deltaTime * fovLerpSpeed);
            else if (!sprinting)
                currentFOV = Mathf.Lerp(currentFOV, normalFOV, Time.deltaTime * fovLerpSpeed);

            //Apply FOV
            mainCam.fieldOfView = Math.Abs(currentFOV);
        }

        private void Jump()
        {
            if (grounded && readyToJump)
            {
                readyToJump = false;

                //Add jump forces
                rb.AddForce(Vector2.up * jumpForce * 1.5f * jumpMultiplier);
                rb.AddForce(normalVector * jumpForce * 0.5f * jumpMultiplier);

                //If jumping while falling, reset y velocity.
                Vector3 vel = rb.velocity;
                if (rb.velocity.y < 0.5f)
                    rb.velocity = new Vector3(vel.x, 0, vel.z);
                else if (rb.velocity.y > 0)
                    rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

                //If jumping while sprinting, limit x and z velocity and add forwards and backwards force
                if (sprinting)
                {
                    rb.velocity = new Vector3(vel.x / 2f, vel.y, vel.z / 2f);
                    rb.AddForce(transform.forward * x * sprintSpeed * 10f * Time.deltaTime);
                }

                Invoke(nameof(ResetJump), jumpCooldown);
            }
        }

        private void ResetJump()
        {
            readyToJump = true;
            jumpMultiplier = 1f;
        }

        private float desiredX;
        private void Look()
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * sensMultiplier;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * sensMultiplier;

            //Find current look rotation
            Vector3 rot = playerCam.transform.localRotation.eulerAngles;
            desiredX = rot.y + mouseX;

            //Rotate, and also make sure we dont over- or under-rotate.
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            //Perform the rotations
            playerCam.transform.localRotation = Quaternion.Euler(xRotation, desiredX, WallRun.Instance.tilt);
            orientation.transform.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        }

        public void ChangeSens(float increment){
            sensitivity += increment;
        }

        public void ResetSens(){
            sensitivity = normSensitivity;
        }

        private void CounterMovement(float x, float y, Vector2 mag)
        {
            if (!grounded || jumping) return;

            //Counter movement
            if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
            }
            if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0))
            {
                rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
            }

            //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
            if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
            {
                float fallspeed = rb.velocity.y;
                Vector3 n = rb.velocity.normalized * maxSpeed;
                rb.velocity = new Vector3(n.x, fallspeed, n.z);
            }
        }

        /// <summary>
        /// Find the velocity relative to where the player is looking
        /// Useful for vectors calculations regarding movement and limiting movement
        /// </summary>
        /// <returns></returns>
        public Vector2 FindVelRelativeToLook()
        {
            float lookAngle = orientation.transform.eulerAngles.y;
            float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

            float u = Mathf.DeltaAngle(lookAngle, moveAngle);
            float v = 90 - u;

            float magnitue = rb.velocity.magnitude;
            float yMag = magnitue * Mathf.Cos(u * Mathf.Deg2Rad);
            float xMag = magnitue * Mathf.Cos(v * Mathf.Deg2Rad);

            return new Vector2(xMag, yMag);
        }

        public Vector3 FindLook()
        {
            return orientation.rotation.eulerAngles;
        }

        private Vector3 GetVelocity()
        {
            if (!grounded)
                return new Vector3(rb.velocity.x / 2, rb.velocity.y, rb.velocity.z / 2);
            else
                return rb.velocity;
        }

        private bool IsFloor(Vector3 v)
        {
            float angle = Vector3.Angle(Vector3.up, v);
            return angle < maxSlopeAngle;
        }

        private bool cancellingGrounded;

        /// <summary>
        /// Handle ground detection
        /// </summary>
        private void OnCollisionStay(Collision other)
        {
            //Iterate through every collision in a physics update
            for (int i = 0; i < other.contactCount; i++)
            {
                Vector3 normal = other.contacts[i].normal;
                //FLOOR
                if (IsFloor(normal))
                {
                    grounded = true;
                    cancellingGrounded = false;
                    normalVector = normal;
                    CancelInvoke(nameof(StopGrounded));
                }
            }

            //Invoke ground/wall cancel, since we can't check normals with CollisionExit for some reason idduno its bugged
            float delay = 3f;
            if (!cancellingGrounded)
            {
                cancellingGrounded = true;
                Invoke(nameof(StopGrounded), Time.deltaTime * delay);
            }
        }

        private void StopGrounded()
        {
            grounded = false;
        }

        //Return bools
        public bool Grounded()
        {
            return grounded;
        }

        public float GetFallSpeed()
        {
            return rb.velocity.y;
        }

        public bool isMoving()
        {
            if (rb.velocity != Vector3.zero)
                return true;
            else
                return false;
        }

        public float GetMagnitude()
        {
            return rb.velocity.magnitude;
        }
    }
}