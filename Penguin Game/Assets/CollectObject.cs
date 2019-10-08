using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interfaces;

public class CollectObject : MonoBehaviour
{
    private int collectedIceCores = 0;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // DEBUG //
        Debug.Log("Collided with " + other );
        ///////////
        
        if (other.gameObject.GetComponent<Collectable>() != null)
        {
            GameObject.Destroy(other.gameObject);
            collectedIceCores++;
            
            // DEBUG //
            Debug.Log("Collected " + collectedIceCores + " ice cores");
            ///////////
        }
    }
}
