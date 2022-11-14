using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerController : Controller2D
{
    Animation animation;
    private static PlayerController _Player = null;
    private float _MoveSpeed = 1.0f;
    #region public Variable
    #endregion

    private void Awake()
    {
        if (_Player == null)
        {
            _Player = this;
        }
    }

    void Start()
    {

        ParentStart();

    }



    void FixedUpdate()
    {

        ParentUpdate(Input.GetAxisRaw("Horizontal"), KeyCode.Space);
        

    }
    private void OnTriggerEnter2D(Collider2D coll)
    {

        switch (coll.tag)
        {
            case "Flag":
                useWallSlide = true;
                if (useWallSlide == true)
                {

                    transform.position += new Vector3(_MoveSpeed, 0, 0);
                }

                break;
            case "Enemy":
                Debug.Log("Player Die");
                break;
        }

    }

    public static PlayerController getPlayer
    {
        get { return _Player; }
    }

    public GameObject getObj
    {
        get { return gameObject; }
    }
    public void PlayAnimation() {
        if(Input.GetKey(KeyCode.A))
        {
            animation.Play("CharactorMoveLeft");
        }
        else if(Input.GetKey(KeyCode.D))
        {
            animation.Play("CharactorMoveRight");
        }
        
    }
}