using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilemapDontMove : MonoBehaviour
{
    void FixedUpdate()
    {
        transform.position = new Vector2(0f, 0f);
        
    }
}
