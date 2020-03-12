using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Tilemaps;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using UnityEngine.UI;
using UnityEngine.XR;

public class Movement : MonoBehaviour
{
    
    
    private Rigidbody2D rigidbody;
    private Tilemap tilemap;
    private TilemapCollider2D tileCollider;

    private float slidingSpeed;
    
    private float angle;

    private RaycastHit2D hit;
    
    [SerializeField] private float horizontalImpulse;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float jumpImpulse;

    [SerializeField] float slopeFriction;
    
    public bool isSliding = false;
    private bool isOnGround = false;
    private bool isJumping = false;

    private Text debugText;

    public bool isMovementDisabled = false;

    // Start is called before the first frame update
    void Start()
    {
        // DEBUG //
        // debugText = GameObject.Find("Debug Text").GetComponent<Text>();
        ///////////

        tilemap = GameObject.Find("Tilemap").GetComponent<Tilemap>();
        tileCollider = GameObject.Find("Tilemap").GetComponent<TilemapCollider2D>();
        rigidbody = GetComponent<Rigidbody2D>();
        angle = 0.0f;
        isSliding = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // This block of code determines the usual movement of the penguin while they aren't sliding. 
        // The variables hit (which is a member field of the movement script for some reason) and anticipatedHit 
        // are raycasts emanating downwards from the penguin's position and from a position a little way ahead
        // of them respectively.
        // While the "H" key isn't pressed (which was there to test different ways of walking on slopes), 
        // the script checks if the penguin is moving downwards by comparing the height of the first point hit
        // by the raycast "hit", and the height of the first point hit by "anticipatedHit". If it is, the
        // velocity given to the penguin points in the direction of the slope (calculated by turning the 
        // normal of the slope 90 degrees clockwise). Otherwise, the penguing is just given a horizontal velocity.
        if (!isMovementDisabled && Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f && !isSliding)
        {
            if (isOnGround)
            {
                hit = Physics2D.Raycast(transform.position, Vector2.down, 100.0f,
                    LayerMask.GetMask("Ground"));
                RaycastHit2D anticipatedHit = Physics2D.Raycast(
                    new Vector2(transform.position.x, transform.position.y) +
                    0.5f * Input.GetAxis("Horizontal") * new Vector2(1.0f, 1.0f),
                    Vector2.down, 100.0f, LayerMask.GetMask("Ground"));

                if (Input.GetKey(KeyCode.H))
                {
                    // This slope mechanism works best when walking on a convex environment
                    rigidbody.velocity = Input.GetAxis("Horizontal") * maxSpeed * new Vector2(anticipatedHit.normal.y,
                                             -anticipatedHit.normal.x);
                }
                else
                {
                    if (anticipatedHit.point.y < 1.0f * hit.point.y)
                    {
                        rigidbody.velocity = Input.GetAxis("Horizontal") * maxSpeed * new Vector2(anticipatedHit.normal.y,
                                                 -anticipatedHit.normal.x);
                    }
                    else
                    {
                        rigidbody.velocity = Input.GetAxis("Horizontal") * maxSpeed * Vector2.right;
                    }
                }
            }
        }

        // This code determines the movement of the penguin while they are sliding.
        // As before, it checks the slope of the ground below the penguin. It ensures the penguin is always facing
        // towards the ground, and accelerates them across the slope, with acceleration determined by the slope's
        // steepness. The variable slidingSpeed was probably used in the past, but it's been replaced with just
        // modifying the rigidbody's velocity directly.
        // If the penguin isn't on the ground, it points them in the direction of their velocity, making a nice little 
        // diving motion.
        if (isSliding)
        {
            hit = Physics2D.Raycast(transform.position, Vector2.down, 100.0f, LayerMask.GetMask("Ground"));

            if (hit != null)
            {
                if (isOnGround)
                {
                    slidingSpeed += hit.normal.x * 5.0f * Time.fixedDeltaTime;
                    Mathf.Clamp(slidingSpeed, 0.0f, 10.0f);
                    transform.right = -hit.normal;
                    
                    if (rigidbody.velocity.magnitude < 50.0f)
                    {
                        rigidbody.velocity += hit.normal.x * 20.0f * Time.fixedDeltaTime * new Vector2(transform.up.x, transform.up.y);
                    }
                }
                else
                {
                    transform.up = rigidbody.velocity;
                }
            }
        }
        else
        {
            transform.right = Vector3.right;
        }
        
        if (isOnGround)
        {
            hit = Physics2D.Raycast(transform.position, Vector2.down, 100.0f, LayerMask.GetMask("Ground"));
            
            if (hit.collider != null)
            {
                // DEBUG //
                // Debug.DrawLine(transform.position, hit.point, Color.red);
                // Debug.DrawLine(hit.point, hit.point + hit.normal, Color.green);
                // Debug.DrawLine(transform.position, transform.position + transform.right, Color.yellow);
                ///////////
                
//                rigidbody.velocity -= new Vector2(hit.normal.x * slopeFriction, 0.0f);

                transform.Translate(new Vector3(0.0f,
                    -hit.normal.x * Mathf.Abs(rigidbody.velocity.x) * Time.fixedDeltaTime *
                    (rigidbody.velocity.x - hit.normal.x > 0.0f ? 1 : -1), 0.0f));
            }

            if (isJumping)
            {
                rigidbody.velocity += 5.0f * Vector2.up;
                isJumping = false;
            }
        }
        
        // COLLISION //
        List<ContactPoint2D> contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        foreach (ContactPoint2D contactPoint in contactPoints)
        {
            var adjustedContact = contactPoint.point - 0.2f * contactPoint.normal;
            string tile = "";

            if (tilemap.HasTile(tilemap.WorldToCell(adjustedContact)))
            {
                tile = tilemap.GetTile(tilemap.WorldToCell(adjustedContact)).name;
            }

            // Code for breaking breakable blocks when sliding into them. They're not in the example level now, but they
            // have been in the past and they worked alright.
            if (tile == "Breakable" && isSliding && contactPoint.relativeVelocity.magnitude > 10.0f)
            {
                tilemap.SetTile(tilemap.WorldToCell(adjustedContact), null);
                tilemap.SetColliderType(tilemap.WorldToCell(adjustedContact), Tile.ColliderType.None);
                tileCollider.composite.GenerateGeometry();
            }

            if (Mathf.Abs(Vector2.Dot(contactPoint.normal, Vector2.right)) < 0.8f)
            {
                isOnGround = true;
                StopCoroutine("OnGround");
            }

            // DEBUG //
            // Debug.Log(tile);
            // Debug.DrawLine(contactPoint.point, contactPoint.point + contactPoint.normal, Color.magenta);
            ///////////
        }
    }

    // GetKeyDown events have a tendency to fail in FixedUpdate code, so to solve this for the prototype, there are two
    // bool variables isJumping and isSliding which check for the left shift and down arrow keys respectively in the
    // Update method.
    private void Update()
    {
        // DEBUG //
        // debugText.text = "Velocity: " + rigidbody.velocity.magnitude + "\nisOnGround: " + isOnGround;
        ///////////
        
        // The left shift key handles jumping. While on the ground, jumping is handled in FixedUpdate and it's probably
        // the clunkiest and worst implemented mechanic in the game. Sliding, however, works great. By design, the faster
        // the penguin is sliding, the higher they jump.
        if (!isMovementDisabled && Input.GetKeyDown(KeyCode.LeftShift) && isOnGround)
        {
            if (isSliding)
            {
                float xvelocity = rigidbody.velocity.x;
                rigidbody.velocity = new Vector2(0.6f * xvelocity, rigidbody.velocity.y);
            
                rigidbody.AddForce(1.5f * rigidbody.velocity.magnitude * Vector2.up , ForceMode2D.Impulse);
            }
            else
            {
                isJumping = true;
            }
        }

        if (!isMovementDisabled && Input.GetKeyUp(KeyCode.LeftShift))
        {
            isJumping = false;
        }

        // Pressing the down arrow key makes the penguin jump up a little bit to initiate a diving motion.
        if (!isMovementDisabled && Input.GetKeyDown(KeyCode.DownArrow))
        {
            GetComponent<BoxCollider2D>().size *= new Vector2(1.0f, 0.1f);
            slopeFriction = 0.0f;
            rigidbody.drag = 0.0f;
            rigidbody.velocity += new Vector2(0.0f, 2.0f);
            isSliding = true;
        }

        if (!isMovementDisabled && Input.GetKeyUp(KeyCode.DownArrow))
        {
            isSliding = false;
            GetComponent<BoxCollider2D>().size *= new Vector2(1.0f, 10.0f);
            slopeFriction = 0.1f;
            rigidbody.drag = 0.1f;
            slidingSpeed = 0.0f;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (Mathf.Abs(Vector2.Dot(other.GetContact(0).normal, Vector2.up)) < 0.1f)
        {
            rigidbody.AddForce(-1.0f * other.GetContact(0).normal, ForceMode2D.Impulse);
        }
        
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        StartCoroutine("OnGround");
    }

    // This method exists to make sure small deviations from the ground (such as by stray pixels or the slope mechanic 
    // being a bit chunky) don't affect the player when they choose to jump while sliding, since jumping only works
    // when they're on the ground. If deviations did affect the player, then they could go tumbling into a chasm through
    // no fault of their own.
    private IEnumerator OnGround()
    {
        yield return new WaitForSeconds(0.1f);

        isOnGround = false;
    }
    
    
    /**
     * Represents the current governing behavior of the penguin.
     * Is capable of changing between states according to this diagram:
     *
     *           pressing "jump"
     * Grounded -----------------> Jumping
     *    ^ ^
     *    |  \
     *    |    \ releasing "slide"
     *    |      \
     *    |        \
     *    |          \
     *    |            \
     *    |              \
     *    |                \
     *    |                  \
     *    | pressing "slide"   \
     *    v                      \
     *  Flying <-------------> Sliding
     * 
     */
    enum MovementState
    {
        Grounded,
        Sliding,
        Jumping,
        Flying
    }
    
    private MovementState movementState;
    
    /**
     * Called by Penguin.FixedUpdate().
     */
    void FixedUpdate(MovementState state)
    {
        
        float horizontalAxis = Input.GetAxis("Horizontal");

        switch (state)
        {
            case MovementState.Grounded:
                this.movementState = this.GroundedFixedUpdate(Time.deltaTime, horizontalAxis);
                break;
            default: 
                break; 
        }

    }

    MovementState GroundedFixedUpdate(float deltaTime, float horizontalAxis)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Jumping;
        return MovementState.Grounded;
    }

    MovementState JumpingFixedUpdate(float deltaTime)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Jumping;
        return MovementState.Flying;
    }

    MovementState SlidingFixedUpdate(float deltaTime)
    {
        return MovementState.Sliding;
    }
    
    MovementState FlyingFixedUpdate(float deltaTime)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Jumping;
        return MovementState.Flying;
    }
}



