using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReadSign : MonoBehaviour
{
    private Movement penguinMovement;
    private bool isSignRead = false;
    private Text information;
    private bool isInFrontOfSign;
    private Sign sign;
    [SerializeField] GameObject dialogueBox;
    [SerializeField] Text dialogueText;

    private void Start()
    {
        penguinMovement = GameObject.Find("Penguin").GetComponent<Movement>();
        information = GameObject.Find("Information Text").GetComponent<Text>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<Sign>() != null)
        {
            sign = other.gameObject.GetComponent<Sign>();
            isInFrontOfSign = true;
            information.text = "Press E to read sign";
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        sign = null;
        isInFrontOfSign = false;
        information.text = "";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isSignRead)
            {
                if (isInFrontOfSign)
                {
                    isSignRead = true;
                    if (penguinMovement.isSliding)
                    {
                        penguinMovement.isSliding = false;
                        GetComponent<BoxCollider2D>().size *= new Vector2(1.0f, 10.0f);
                    }
                    penguinMovement.isMovementDisabled = true;
                    dialogueText.text = sign.signMessage;
                    dialogueBox.SetActive(true);
                }
            }
            else
            {
                isSignRead = false;
                penguinMovement.isMovementDisabled = false;
                dialogueBox.SetActive(false);
            }
        }
    }
}
