using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalPhysics : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Physics2D.gravity = new Vector2(0.0f, -9.8f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
