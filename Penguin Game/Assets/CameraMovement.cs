using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private GameObject penguin;
    private Transform penguinTransform;
    private Rigidbody2D penguinRigidbody;
    private float penguinSpeed;
    
    private Camera camera;
    
    // Start is called before the first frame update
    void Start()
    {
        penguin = GameObject.Find("Penguin");
        penguinTransform = penguin.transform;
        penguinRigidbody = penguin.GetComponent<Rigidbody2D>();
        
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        penguinSpeed = penguinRigidbody.velocity.magnitude;
        if (penguin.GetComponent<Movement>().isSliding)
        {
            camera.orthographicSize = Mathf.Lerp(5.0f, 20.0f, penguinSpeed / 50.0f);
        }
        else
        {
            camera.orthographicSize = 5.0f;
        }

        transform.position = penguin.transform.position + new Vector3(0.0f, 2.0f, -1.0f);
    }
}
