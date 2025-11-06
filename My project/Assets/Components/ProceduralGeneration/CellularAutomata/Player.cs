using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private Vector3 _moveDirection;

    private void Update()
    {
        _moveDirection = Vector3.zero;
        if (Keyboard.current.wKey.isPressed)
            _moveDirection.z = 1;
        else if (Keyboard.current.sKey.isPressed)
            _moveDirection.z = -1;
        if (Keyboard.current.aKey.isPressed)
            _moveDirection.x = -1;
        else if (Keyboard.current.dKey.isPressed)
            _moveDirection.x = 1;
        if (_moveDirection != Vector3.zero)
        {
            _moveDirection.Normalize();
            transform.position += _moveDirection * Time.deltaTime * 20f;
        }
    }

    public void DeletePlayer() { Destroy(gameObject); }
}