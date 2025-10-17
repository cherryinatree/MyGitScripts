using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    public float Speed = 5f;
    private float origionalSpeed;
    public float wallHopOff = 3;
    public float moving;
    public float JumpHeight = 4f;
    public float GroundDistance = 0.5f;
    public float DashDistance = 5f;
    public float degradeSpeed = 0.1f;
    public LayerMask Ground;

    public Rigidbody _body;
    [HideInInspector]
    public Vector3 _inputs = Vector3.zero;
    public bool _isGrounded = true;
    private Transform _groundChecker;

    public GeneralTrigger groundDececter;
    public GeneralTrigger bodyWallDececter;
    public GeneralTrigger wallDececter;

    public bool PcControls = false;

    [Header("Animation Smoothing")]
    [Range(0, 1f)]
    public float HorizontalAnimSmoothTime = 0.2f;
    [Range(0, 1f)]
    public float VerticalAnimTime = 0.2f;
    [Range(0, 1f)]
    public float StartAnimTime = 0.3f;
    [Range(0, 1f)]
    public float StopAnimTime = 0.15f;

    public Animator anim;


    public float allowPlayerRotation = 0.4f;

    public float FallMultiplier = 2.5f;

    public bool winPause = false;

    void Start()
    {
        anim = this.GetComponent<Animator>();
        _body = GetComponent<Rigidbody>();
       // origionalSpeed = Speed;

    }

    void Update()
    {
        if (!winPause)
        {
            //SpeedCheck();
            if (PcControls)
            {
                _inputs = Vector3.zero;
                _inputs.x = Input.GetAxis("Horizontal");
                _inputs.z = Input.GetAxis("Vertical");
                if (_inputs != Vector3.zero)
                    transform.forward = _inputs;
            }
            if (Input.GetButtonDown("Jump"))
            {
                ButtonJump();
            }

            JumpSmooth();
            Movement();
        }
        else
        {
            _body.constraints = RigidbodyConstraints.FreezePosition;
        }
    }

    private void SpeedCheck()
    {
        if (Speed > origionalSpeed)
        {
           /* isGrounded();

            if (_isGrounded)
            {*/
                Speed -= Time.deltaTime;
                if (Speed < origionalSpeed + 1)
                {
                    Speed = origionalSpeed;
                }
          /*  }
            else
            {

                Speed -= Time.deltaTime;
                if (Speed < origionalSpeed + 1)
                {
                    Speed = origionalSpeed;
                }
            }*/
        }
        if (Speed < origionalSpeed)
        {
            isGrounded();

            if (_isGrounded)
            {
                Speed += Time.deltaTime * 5;
                if (Speed > origionalSpeed - 1)
                {
                    Speed = origionalSpeed;
                }
            }
            else
            {

                Speed -= Time.deltaTime;
                if (Speed < origionalSpeed + 1)
                {
                    Speed = origionalSpeed;
                }
            }
        }


    }

    private void JumpSmooth()
    {
        if(_body.velocity.y < 0)
        {
            _body.velocity += Vector3.up * Physics.gravity.y * (FallMultiplier-1) * Time.deltaTime;
        }
    }

    public void ButtonLeft(bool active)
    {
        if (!winPause)
        {
            if (active)
            {
                _inputs = Vector3.zero;
                _inputs.x = -1f;
                _inputs.z = Input.GetAxis("Vertical");
                if (_inputs != Vector3.zero)
                    transform.forward = _inputs;

                //_body.MovePosition(_inputs);
                Movement();
            }
            else
            {

                _inputs = Vector3.zero;
                _inputs.x = 0;
                _inputs.z = Input.GetAxis("Vertical");
                if (_inputs != Vector3.zero)
                    transform.forward = _inputs;

                Movement();
            }
        }
    }

    public void ButtonRight(bool active)
    {
        if (!winPause)
        {
            if (active)
            {
                _inputs = Vector3.zero;
                _inputs.x = 1f;
                _inputs.z = Input.GetAxis("Vertical");
                if (_inputs != Vector3.zero)
                    transform.forward = _inputs;

                Movement();
            }
            else
            {

                _inputs = Vector3.zero;
                _inputs.x = 0;
                _inputs.z = Input.GetAxis("Vertical");
                if (_inputs != Vector3.zero)
                    transform.forward = _inputs;

                Movement();
            }
        }
    }

    public void ButtonJump()
    {
        if (!winPause) 
        {


            isGrounded();

            if (_isGrounded)
            {
                if (groundDececter.TriggerActivated)
                {
                    _body.velocity = new Vector3(_body.velocity.x, JumpHeight, _body.velocity.z);
                }
                else
                {
                    float sign = 1;
                    if(_body.velocity.x < 0)
                    {
                        sign = -1;
                    }
                    else
                    {
                        sign = 1;
                    }
                    _body.velocity = new Vector3(sign * wallHopOff, JumpHeight, _body.velocity.z);
                }
            }

            if(wallDececter != null)
            {
                if (wallDececter.TriggerActivated)
                {
                    float sign = 1;
                    if (_inputs.x < 0)
                    {
                        sign = 1;
                    }
                    else
                    {
                        sign = -1;
                    }

                    _body.velocity = new Vector3(sign * (wallHopOff* 1.5f), (JumpHeight*1.5f), _body.velocity.z);
                }
            }
        }
    }

    private void Movement()
    {
        if (!winPause)
        {
            moving = new Vector2(_inputs.x, _inputs.z).sqrMagnitude;

            if (moving > allowPlayerRotation)
            {
                anim.SetFloat("Blend", moving * 2, StartAnimTime, Time.deltaTime);
            }
            else if (moving < allowPlayerRotation)
            {
                anim.SetFloat("Blend", moving * 2, StopAnimTime, Time.deltaTime);
            }
        }
    }

    public void BounceFace()
    {
        float direction = 1;
        if (_body.velocity.x>0)
        {
            direction = 1;
        }
        else
        {

            direction = -1;
        }

            Vector3 facing = new Vector3(direction, _inputs.z, 0).normalized;
        Debug.Log(facing);

        transform.forward = facing;
    }


    void FixedUpdate()
    {
        if(_inputs.x != 0)
        {
            if(_body.velocity.x > 0 && _inputs.x < 0)
            {
                _body.velocity -= new Vector3(degradeSpeed, 0,0);
            }
            if(_body.velocity.x < 0 && _inputs.x > 0)
            {

                _body.velocity += new Vector3(degradeSpeed, 0, 0);
            }
        }
        _body.MovePosition(_body.position + _inputs * Speed * Time.fixedDeltaTime);
    }


    private void isGrounded()
    {

        if (groundDececter.TriggerActivated || bodyWallDececter.TriggerActivated)
        {
            _isGrounded = true;
        }
        else
        {
            _isGrounded = false;
        }
    }


    private bool RayDown()
    {
         RaycastHit hit;
         if (Physics.Raycast(transform.position, -Vector3.up, out hit))
         {
             //Debug.Log(hit.distance);
             if (hit.distance < GroundDistance)
             {
                 return true;
             }
             else
             {
                 return false;
             }
         }
         return false;

    }
}
