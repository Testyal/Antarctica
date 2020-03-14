using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private GameObject follow;

    private void Update()
    {
        this.transform.position = this.follow.transform.position + Vector3.back;
    }
}
