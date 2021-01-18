using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.InputSystem;

    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]

    public class PlayerMover : MonoBehaviour
    {

        [Tooltip("How fast the player moves.")]
        public float movementSpeed = 7.0f;
        [Tooltip("Units per second acceleration")]
        public float accelRate = 20.0f;
        [Tooltip("Units per second deceleration")]
        public float decelRate = 20.0f;
        [Tooltip("Acceleration the player has in mid-air")]
        public float airborneAccel = 5.0f;
        [Tooltip("The velocity applied to the player when the jump button is pressed")]
        public float jumpSpeed = 7.0f;
        [Tooltip("Extra units added to the player's fudge height")]
        // Extra units added to the player's fudge height... if you're rocketting off
        // ramps or feeling too loosely attached to the ground, increase this.
        // If you're being yanked down to stuff too far beneath you, lower this.
        // Thid can't be modified during runtime
        public float fudgeExtra = 0.5f;
        [Tooltip("Maximum slope the player can walk up")]
        public float maximumSlope = 45.0f;

        private bool isGrounded = false;
        public bool IsGrounded { get => isGrounded; }

        //Unity Components
        private Rigidbody rigidbody;
        private CapsuleCollider capsuleCollider;

        // Temp vars
        private float inputX;
        private float inputY;
        private Vector2 movementInput;
        private Vector3 movementVector;

        // Acceleration or deceleration
        private float acceleration;

        /*
         * Keep track of falling
         */
        private bool isFalling;
        public bool IsFalling { get => isFalling; }

        private float fallSpeed;
        public float FallSpeed { get => fallSpeed; }

        /*
         * Jump state var:
         * 0 = hit ground since last jump, can jump if grounded = true
         * 1 = jump button pressed, try to jump during fixedupdate
         * 2 = jump force applied, waiting to leave the ground
         * 3 = jump was successful, haven't hit the ground yet (this state is to ignore fudging)
        */
        private byte jumpState;

        // Average normal of the ground i'm standing on
        private Vector3 groundNormal;
        public Vector3 GroundNormal { get => groundNormal; }

        // If we're touching a dynamic object, don't prevent idle sliding
        private bool touchingDynamic;

        // Was i grounded last frame? used for fudging
        private bool groundedLastFrame;

        // The objects i'm colliding with
        private List<GameObject> collisions;

        // All of the collision contact points
        private Dictionary<int, ContactPoint[]> contactPoints;

        /*
         * Temporary calculations
         */
        private float halfPlayerHeight;
        private float fudgeCheck;
        private float bottomCapsuleSphereOrigin; // transform.position.y - this variable = the y coord for the origin of the capsule's bottom sphere
        private float capsuleRadius;

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();

            movementVector = Vector3.zero;

            isGrounded = false;
            groundNormal = Vector3.zero;
            touchingDynamic = false;
            groundedLastFrame = false;

            collisions = new List<GameObject>();
            contactPoints = new Dictionary<int, ContactPoint[]>();

            // do our calculations so we don't have to do them every frame
            Debug.Log(capsuleCollider);
            halfPlayerHeight = capsuleCollider.height * 0.5f;
            fudgeCheck = halfPlayerHeight + fudgeExtra;
            bottomCapsuleSphereOrigin = halfPlayerHeight - capsuleCollider.radius;
            capsuleRadius = capsuleCollider.radius;

            var controllerMat = new PhysicMaterial();
            controllerMat.bounciness = 0.0f;
            controllerMat.dynamicFriction = 0.0f;
            controllerMat.staticFriction = 0.0f;
            controllerMat.bounceCombine = PhysicMaterialCombine.Minimum;
            controllerMat.frictionCombine = PhysicMaterialCombine.Minimum;
            capsuleCollider.material = controllerMat;

            // just in case this wasn't set in the inspector
            rigidbody.freezeRotation = true;
        }

        void FixedUpdate()
        {
            // check if we're grounded0
            RaycastHit hit;
            isGrounded = false;
            groundNormal = Vector3.zero;

            foreach (var contacts in contactPoints.Values)
            {
                for (var i = 0; i < contacts.Length; i++)
                {
                    if (contacts[i].point.y <= rigidbody.position.y - bottomCapsuleSphereOrigin &&
                        Physics.Raycast(contacts[i].point + Vector3.up, Vector3.down, out hit, 1.1f, ~0) &&
                        Vector3.Angle(hit.normal, Vector3.up) <= maximumSlope)
                    {
                        isGrounded = true;
                        groundNormal += hit.normal;

                    }
                }
            }

            if (isGrounded)
            {
                // average the summed normals
                groundNormal.Normalize();

                if (jumpState == 3)
                    jumpState = 0;
            }
            else if (jumpState == 2)
                jumpState = 3;

            // get player input
            inputX = movementInput.x;
            inputY = movementInput.y;

            // limit the length to 1.0f
            float length = 0;

            if (isGrounded && jumpState != 3)
            {
                if (isFalling)
                {
                    // we just landed from a fall
                    isFalling = false;
                    this.DoFallDamage(Mathf.Abs(fallSpeed));
                }

                // align our movement vectors with the ground normal (ground normal = up)
                var newForward = transform.forward;
                Vector3.OrthoNormalize(ref groundNormal, ref newForward);

                var targetSpeed = Vector3.Cross(groundNormal, newForward) * inputX * movementSpeed +
                                  newForward * inputY * movementSpeed;

                length = targetSpeed.magnitude;
                var difference = length - rigidbody.velocity.magnitude;

                // avoid divide by zero
                if (Mathf.Approximately(difference, 0.0f))
                    movementVector = Vector3.zero;

                else
                {
                    // determine if we should accelerate or decelerate
                    if (difference > 0.0f)
                        acceleration = Mathf.Min(accelRate * Time.deltaTime, difference);

                    else
                        acceleration = Mathf.Max(-decelRate * Time.deltaTime, difference);

                    // normalize the difference vector and store it in movement
                    difference = 1.0f / difference;
                    movementVector = (targetSpeed - rigidbody.velocity) * difference * acceleration;
                }

                if (jumpState == 1)
                {
                    // jump button was pressed, do jump  
                    movementVector.y = jumpSpeed - rigidbody.velocity.y;
                    jumpState = 2;
                }
                else if (!touchingDynamic && Mathf.Approximately(inputX + inputY, 0.0f) && jumpState < 2)
                    // prevent sliding by countering gravity... this may be dangerous
                    movementVector.y -= Physics.gravity.y * Time.deltaTime;

                rigidbody.AddForce(movementVector, ForceMode.VelocityChange);
                groundedLastFrame = true;
            }
            else
            {
                // not grounded, so check if we need to fudge and do air accel

                // fudging
                if (groundedLastFrame && jumpState != 3 && !isFalling)
                {
                    // see if there's a surface we can stand on beneath us within fudgeCheck range
                    if (Physics.Raycast(transform.position, Vector3.down, out hit, fudgeCheck +
                        (rigidbody.velocity.magnitude * Time.deltaTime), ~0) &&
                        Vector3.Angle(hit.normal, Vector3.up) <= maximumSlope)
                    {
                        groundedLastFrame = true;

                        // catches jump attempts that would have been missed if we weren't fudging
                        if (jumpState == 1)
                        {
                            movementVector.y += jumpSpeed;
                            jumpState = 2;
                            return;
                        }

                        // we can't go straight down, so do another raycast for the exact distance towards the surface
                        // i tried doing exsec and excsc to avoid doing another raycast, but my math sucks and it failed
                        // horribly. if anyone else knows a reasonable way to implement a simple trig function to bypass
                        // this raycast, please contribute to the thread!
                        if (Physics.Raycast(new Vector3(transform.position.x,
                            transform.position.y - bottomCapsuleSphereOrigin,
                            transform.position.z), -hit.normal, out hit, hit.distance, ~0))
                        {
                            rigidbody.AddForce(hit.normal * -hit.distance, ForceMode.VelocityChange);
                            return; // skip air accel because we should be grounded
                        }
                    }
                }

                // if we're here, we're not fudging so we're defintiely airborne
                // thus, if falling isn't set, set it
                if (!isFalling)
                    isFalling = true;

                fallSpeed = rigidbody.velocity.y;

                // air accel
                if (!Mathf.Approximately(inputX + inputY, 0.0f))
                {
                    // note, this will probably malfunction if you set the air accel too high...
                    // this code should be rewritten if you intend to do so

                    // get direction vector
                    movementVector = transform.TransformDirection(new Vector3(inputX * airborneAccel * Time.deltaTime,
                        0.0f, inputY * airborneAccel * Time.deltaTime));

                    // add up our accel to the current velocity to check if it's too fast
                    var a = movementVector.x + rigidbody.velocity.x;
                    var b = movementVector.z + rigidbody.velocity.z;

                    // check if our new velocity will be too fast
                    length = Mathf.Sqrt(a * a + b * b);
                    if (length > 0.0f)
                    {
                        if (length > movementSpeed)
                        {
                            // normalize the new movement vector
                            length = 1.0f / Mathf.Sqrt(movementVector.x * movementVector.x +
                                movementVector.z * movementVector.z);
                            movementVector.x *= length;
                            movementVector.z *= length;

                            // normalize our current velocity (before accel)
                            length = 1.0f / Mathf.Sqrt(rigidbody.velocity.x * rigidbody.velocity.x +
                                rigidbody.velocity.z * rigidbody.velocity.z);
                            var rigidbodyDirection = new Vector3(rigidbody.velocity.x * length, 0.0f,
                                rigidbody.velocity.z * length);

                            // dot product of accel unit vector and velocity unit vector, clamped above 0 and inverted (1-x)
                            length = (1.0f - Mathf.Max(movementVector.x * rigidbodyDirection.x +
                                movementVector.z * rigidbodyDirection.z, 0.0f)) * airborneAccel * Time.deltaTime;
                            movementVector.x *= length;
                            movementVector.z *= length;
                        }

                        // and finally, add our force
                        rigidbody.AddForce(new Vector3(movementVector.x, 0.0f, movementVector.z),
                            ForceMode.VelocityChange);
                    }
                }

                groundedLastFrame = false;
            }
        }

        void DoFallDamage(float fallSpeed) // fallSpeed will be positive
        {
            // do your fall logic here using fallSpeed to determine how hard we hit the ground
            Debug.Log("Hit the ground at " + fallSpeed.ToString() + " units per second");
        }

        void OnCollisionEnter(Collision collision)
        {
            // keep track of collision objects and contact points
            collisions.Add(collision.gameObject);
            contactPoints.Add(collision.gameObject.GetInstanceID(), collision.contacts);

            // check if this object is dynamic
            if (!collision.gameObject.isStatic)
                touchingDynamic = true;

            // reset the jump state if able
            if (jumpState == 3)
                jumpState = 0;
        }

        void OnCollisionStay(Collision collision)
        {
            // update contact points
            contactPoints[collision.gameObject.GetInstanceID()] = collision.contacts;
        }

        void OnCollisionExit(Collision collision)
        {
            touchingDynamic = false;

            // remove this collision and its associated contact points from the list
            // don't break from the list once we find it because we might somehow have duplicate entries,
            // and we need to recheck groundedOnDynamic anyways
            for (var i = 0; i < collisions.Count; i++)
            {
                if (collisions[i] == collision.gameObject)
                    collisions.RemoveAt(i--);

                else if (!collisions[i].isStatic)
                    touchingDynamic = true;
            }

            contactPoints.Remove(collision.gameObject.GetInstanceID());
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            movementInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (groundedLastFrame)
                jumpState = 1;
        }
       
    }
}