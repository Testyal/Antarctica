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
        debugText = GameObject.Find("Debug Text").GetComponent<Text>();
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
        if (!isMovementDisabled && Mathf.Abs(Input.GetAxis("Horizontal")) > 0.05f && !isSliding)
        {
            if (isOnGround)
            {
                hit = Physics2D.Raycast(transform.position, Vector2.down, 100.0f,
                    LayerMask.GetMask("Ground"));
                RaycastHit2D anticipatedHit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y) + 0.5f * Input.GetAxis("Horizontal") * Vector2.Perpendicular(hit.normal), 
                    Vector2.down, 100.0f, LayerMask.GetMask("Ground"));

                if (Input.GetKey(KeyCode.H))
                {
                    // This slope mechanism works best when walking on a convex environment
                    rigidbody.velocity = Input.GetAxis("Horizontal") * maxSpeed * new Vector2(anticipatedHit.normal.y, 
                                            -anticipatedHit.normal.x);
                }
                else
                {
                    // Whereas this works best when walking on a concave environment
                    rigidbody.velocity = Input.GetAxis("Horizontal") * maxSpeed * new Vector2(anticipatedHit.normal.y, 
                                                  -anticipatedHit.normal.x);
                }
            }
        }
        
        
        
        
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
                Debug.DrawLine(transform.position, hit.point, Color.red);
                Debug.DrawLine(hit.point, hit.point + hit.normal, Color.green);
                Debug.DrawLine(transform.position, transform.position + transform.right, Color.yellow);
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
            Debug.Log(tile);
            Debug.DrawLine(contactPoint.point, contactPoint.point + contactPoint.normal, Color.magenta);
            ///////////
        }
        ///////////////
    }

    private void Update()
    {
        // DEBUG //
        debugText.text = "Velocity: " + rigidbody.velocity.magnitude + "\nisOnGround: " + isOnGround;
        ///////////
        
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


    private IEnumerator OnGround()
    {
        yield return new WaitForSeconds(0.1f);

        isOnGround = false;
    }
}

