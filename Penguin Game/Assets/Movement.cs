using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;


/**
 * Represents the current governing behavior of the penguin.
 */
enum MovementState
{
    Grounded,
    Sliding,
    Jumping,
    Flying,
    AnticipateLand,
    Landing
}

class GroundedMovementRegime
{
    private readonly BoxCollider2D collider;
    readonly Rigidbody2D rigidbody;
    readonly Transform transform;

    readonly float maxGroundedSpeed;

    public GroundedMovementRegime(BoxCollider2D collider, Rigidbody2D rigidbody, Transform transform, float maxGroundedSpeed)
    {
        this.collider = collider;
        this.rigidbody = rigidbody;
        this.transform = transform;

        this.maxGroundedSpeed = maxGroundedSpeed;
    }
    
    public MovementState FixedUpdate(float delta, float horizontalAxis)
    {
        var contactPoints = new List<ContactPoint2D>();
        this.rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Jumping;

        var anticipatedGround = Physics2D.Raycast(
            new Vector2(this.transform.position.x, this.transform.position.y) + 0.5f * horizontalAxis * new Vector2(1.0f, 1.0f),
            Vector2.down,
            100.0f,
            LayerMask.GetMask("Ground")
        );

        this.rigidbody.velocity = horizontalAxis * this.maxGroundedSpeed * new Vector2(anticipatedGround.normal.y, -anticipatedGround.normal.x);
        
        return MovementState.Grounded;
    }

    public MovementState EnterFlying()
    {
        this.collider.size *= new Vector2(1.0f, 0.1f);
        this.rigidbody.velocity += new Vector2(0.0f, 2.0f);

        return MovementState.Flying;
    }
}


class FlyingMovementRegime
{
    private readonly Rigidbody2D rigidbody;
    private readonly Transform transform;

    public FlyingMovementRegime(Rigidbody2D rigidbody, Transform transform)
    {
        this.rigidbody = rigidbody;
        this.transform = transform;
    }
    
    public MovementState FixedUpdate()
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count > 0) return MovementState.Sliding;

        transform.up = rigidbody.velocity;

        return MovementState.Flying;
    }
}


class SlidingMovementRegime
{
    private readonly BoxCollider2D collider;
    private readonly Rigidbody2D rigidbody;
    private readonly Transform transform;

    private readonly float maxSlidingSpeed;
    private readonly float slidingAcceleration;

    public SlidingMovementRegime(BoxCollider2D collider, Rigidbody2D rigidbody, Transform transform, float maxSlidingSpeed, float slidingAcceleration)
    {
        this.collider = collider;
        this.rigidbody = rigidbody;
        this.transform = transform;

        this.maxSlidingSpeed = maxSlidingSpeed;
        this.slidingAcceleration = slidingAcceleration;
    }
    
    public MovementState FixedUpdate(float delta)
    {
        var contactPoints = new List<ContactPoint2D>();
        this.rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Flying;

        var groundSlope = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            100.0f,
            LayerMask.GetMask("Ground")
        ).normal;

        // Increase penguin's speed if not too fast.
        if (this.rigidbody.velocity.magnitude < this.maxSlidingSpeed)
            this.rigidbody.velocity += this.slidingAcceleration * groundSlope.x * delta * new Vector2(this.transform.up.x, this.transform.up.y);

        // Slide headfirst.
        this.transform.right = -groundSlope;

        return MovementState.Sliding;
    }


    public MovementState EnterFlying()
    {
        
        
        return MovementState.Flying;
    }

    public MovementState EnterGrounded()
    {
        this.collider.size *= new Vector2(1.0f, 10.0f);
        this.transform.up = Vector2.up;

        return MovementState.Grounded;
    }
}


class AnticipateLandMovementRegime
{
    private readonly Rigidbody2D rigidbody;
    private readonly Transform transform;

    public AnticipateLandMovementRegime(Rigidbody2D rigidbody, Transform transform)
    {
        this.rigidbody = rigidbody;
        this.transform = transform;
    }
    
    public MovementState FixedUpdate()
    {
        var contactPoints = new List<ContactPoint2D>();
        this.rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count > 0) return MovementState.Landing;

        this.transform.up = this.rigidbody.velocity;

        return MovementState.AnticipateLand;
    }

    public MovementState EnterFlying()
    {
        float xVelocity = this.rigidbody.velocity.x;
        this.rigidbody.velocity = new Vector2(0.6f * xVelocity, this.rigidbody.velocity.y);
        
        return MovementState.Flying;
    }
}


class LandingMovementRegime
{
    private readonly BoxCollider2D collider;
    private readonly Transform transform;
    
    private float landingTimeElapsed = 0.0f;

    public LandingMovementRegime(BoxCollider2D collider, Transform transform)
    {
        this.transform = transform;
        this.collider = collider;
    }
    
    public MovementState FixedUpdate(float delta)
    {
        this.landingTimeElapsed += delta;

        if (this.landingTimeElapsed > 0.7f)
        {
            return this.EnterGrounded();
        }

        return MovementState.Landing;
    }

    private MovementState EnterGrounded()
    {
        this.collider.size *= new Vector2(1.0f, 10.0f);
        this.transform.up = Vector2.up;

        this.landingTimeElapsed = 0.0f;

        return MovementState.Grounded;
    }
}


class JumpingMovementRegime
{
    private readonly Rigidbody2D rigidbody;

    public JumpingMovementRegime(Rigidbody2D rigidbody)
    {
        this.rigidbody = rigidbody;
    }
    
    public MovementState FixedUpdate()
    {
        var contactPoints = new List<ContactPoint2D>();
        this.rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count > 0) return MovementState.Grounded;
        return MovementState.Jumping;
    }
}


class Movement: MonoBehaviour
{
    private MovementState movementState = MovementState.Grounded;

    private GroundedMovementRegime groundedMovementRegime;
    private JumpingMovementRegime jumpingMovementRegime;
    private FlyingMovementRegime flyingMovementRegime;
    private SlidingMovementRegime slidingMovementRegime;
    private AnticipateLandMovementRegime anticipateLandMovementRegime;
    private LandingMovementRegime landingMovementRegime;

    [SerializeField] private KeyCode jumpKey;
    [SerializeField] private KeyCode slideKey;

    [SerializeField] private float maxGroundSpeed;
    [SerializeField] private float maxSlidingSpeed;
    [SerializeField] private float slidingAcceleration;

    private float HorizontalAxis => Input.GetAxis("Horizontal");

    private float Delta => Time.fixedDeltaTime;

    private void Start()
    {
        var collider = this.GetComponent<BoxCollider2D>();
        var transform = this.GetComponent<Transform>();
        var rigidbody = this.GetComponent<Rigidbody2D>();
        
        this.groundedMovementRegime = new GroundedMovementRegime(collider, rigidbody, transform, maxGroundSpeed);
        this.jumpingMovementRegime = new JumpingMovementRegime(rigidbody);
        this.flyingMovementRegime = new FlyingMovementRegime(rigidbody, transform);
        this.slidingMovementRegime = new SlidingMovementRegime(collider, rigidbody, transform, maxSlidingSpeed, slidingAcceleration);
        this.anticipateLandMovementRegime = new AnticipateLandMovementRegime(rigidbody, transform);
        this.landingMovementRegime = new LandingMovementRegime(collider, transform);
    }

    private void Update()
    {
        if (Input.GetKeyDown(slideKey)) OnSlideDown();
        if (Input.GetKeyUp(slideKey)) OnSlideUp();

        if (Input.GetKeyDown(jumpKey)) OnJumpDown();
    }

    private void FixedUpdate()
    {
        switch (this.movementState)
        {
            case MovementState.Grounded:
                this.movementState = this.groundedMovementRegime.FixedUpdate(Delta, HorizontalAxis);
                break;
            case MovementState.Jumping:
                this.movementState = this.jumpingMovementRegime.FixedUpdate();
                break;
            case MovementState.Flying:
                this.movementState = this.flyingMovementRegime.FixedUpdate();
                break;
            case MovementState.Sliding:
                this.movementState = this.slidingMovementRegime.FixedUpdate(Delta);
                break;
            case MovementState.AnticipateLand:
                this.movementState = this.anticipateLandMovementRegime.FixedUpdate();
                break;
            case MovementState.Landing:
                this.movementState = this.landingMovementRegime.FixedUpdate(Delta);
                break;
        }
        
        Debug.Log(this.movementState);
    }

    private void OnSlideDown()
    {
        switch (this.movementState)
        {
            case MovementState.Grounded: 
                this.movementState = this.groundedMovementRegime.EnterFlying();
                break;
            case MovementState.AnticipateLand:
                this.movementState = this.anticipateLandMovementRegime.EnterFlying();
                break;
        }
    }

    private void OnSlideUp()
    {
        switch (this.movementState)
        {
            case MovementState.Flying:
                this.movementState = MovementState.AnticipateLand;
                break;
            case MovementState.Sliding:
                this.movementState = this.slidingMovementRegime.EnterGrounded();
                break;
        }
    }

    private void OnJumpDown()
    {
        switch (this.movementState)
        {
            case MovementState.Sliding:
                this.movementState = MovementState.Flying;
                break;
        }
    }
}


/*
public class Movement : MonoBehaviour
{
    
    private Rigidbody2D rigidbody;
    
    [SerializeField] private float maxGroundedSpeed;
    [SerializeField] private float maxSlidingSpeed;

    [SerializeField] private float slidingAcceleration;
    
    private void Start()
    {
        this.rigidbody = GetComponent<Rigidbody2D>();
    }
    
    private MovementState movementState = MovementState.Jumping;

    private void FixedUpdate()
    {
        this.FixedUpdate(this.movementState, Time.fixedDeltaTime, Input.GetAxis("Horizontal"));
    }

    /**
     * Called by Penguin.FixedUpdate().
     
    private void FixedUpdate(MovementState state, float delta, float horizontalAxis)
    {

        switch (state)
        {
            case MovementState.Grounded:
                this.movementState = this.GroundedFixedUpdate(delta, horizontalAxis);
                break;
            case MovementState.Jumping:
                this.movementState = this.JumpingFixedUpdate(delta);
                break;
            case MovementState.Sliding:
                this.movementState = this.SlidingFixedUpdate(delta);
                break;
            case MovementState.Flying:
                this.movementState = this.FlyingFixedUpdate(delta);
                break;
        }

    }

    private MovementState GroundedFixedUpdate(float delta, float horizontalAxis)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Jumping;

        var anticipatedGroundSlope = Physics2D.Raycast(
            new Vector2(transform.position.x, transform.position.y) + 0.5f * horizontalAxis * new Vector2(1.0f, 1.0f),
            Vector2.down,
            100.0f,
            LayerMask.GetMask("Ground")
        );

        if (anticipatedGroundSlope.point.y < 1.0f * anticipatedGroundSlope.point.y)
        {
            rigidbody.velocity = horizontalAxis * this.maxGroundedSpeed *
                                 new Vector2(anticipatedGroundSlope.normal.y, -anticipatedGroundSlope.normal.x);
        }
        else
        {
            rigidbody.velocity = horizontalAxis * this.maxGroundedSpeed * Vector2.right;
        }

        return MovementState.Grounded;
    }

    private MovementState JumpingFixedUpdate(float delta)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count > 0) return MovementState.Grounded;
        return MovementState.Jumping;
    }

    private MovementState SlidingFixedUpdate(float delta)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count == 0) return MovementState.Flying;

        var groundSlope = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            100.0f,
            LayerMask.GetMask("Ground")
        ).normal;

        // Increase penguin's speed if not too fast.
        if (this.rigidbody.velocity.magnitude < this.maxSlidingSpeed)
            this.rigidbody.velocity += slidingAcceleration * groundSlope.x * delta * new Vector2(transform.up.x, transform.up.y);

        // Slide headfirst.
        transform.right = -groundSlope;

        return MovementState.Sliding;
    }

    private MovementState FlyingFixedUpdate(float delta)
    {
        var contactPoints = new List<ContactPoint2D>();
        rigidbody.GetContacts(contactPoints);

        if (contactPoints.Count > 0) return MovementState.Sliding;

        transform.up = rigidbody.velocity;

        return MovementState.Flying;
    }

}
*/

   // Update is called once per frame
    /* void FixedUpdate()
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
    } */




    // GetKeyDown events have a tendency to fail in FixedUpdate code, so to solve this for the prototype, there are two
    // bool variables isJumping and isSliding which check for the left shift and down arrow keys respectively in the
    // Update method.
    /* private void Update()
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
    } */



