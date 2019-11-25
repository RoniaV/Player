using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovController : MonoBehaviour
{
    [SerializeField]
    private float originalVel = 0.1f;
    [SerializeField]
    private float runMultiplicator = 2.5f;
    [SerializeField]
    private float jumpForce = 0.4f, wallRunningForce = 0.15f, wallRunningTime = 1.5f;
    [SerializeField] [Range (0, 1)]
    private float jumpGravity = 0.4f;
    [SerializeField]
    private float gravity = 1.5f;
    [SerializeField]
    private int trickCount = 1;
    [SerializeField]
    private float maxCameraRot = 80;
    [SerializeField]
    private float camSensitivity = 2;

    private float vel, grav;
    private Vector3 movDir, lastMovDir;
    private float yVel;
    private bool isGrounded, isJumping, isDoingATrick, isDead, isPaused;
    private bool jumpInput, collide;
    private int tricksRemaining;
    private Vector3 wallRunDir;
    private Vector3 groundCheckDir = new Vector3(0, -0.9163612f, -0.4003527f);
    
    private Vector3 camRot, pjRot;
    private float h, v;

    private Camera myCam;
    private CharacterController myCC;

    void Awake()
    {
        myCC = GetComponent<CharacterController>();
        myCam = Camera.main;
    }

    void Start()
    {
        vel = originalVel;
        grav = gravity;
        tricksRemaining = trickCount;

        GameManager.GM.GameOver += Dead;
        GameManager.GM.Pause += Paused;
        GameManager.GM.Resume += Reanude;
    }

    void Update()
    {

        if (!isDead && !isPaused)
        {
            #region Moviminto Logica
            if (isGrounded)
            {
                if (Input.GetAxis("Run") > 0.5f && Input.GetAxis("Vertical") > 0)
                {
                    vel = originalVel * runMultiplicator * Input.GetAxis("Run");
                }
                else { vel = originalVel; }
                
                if (!isDoingATrick)
                {
                    myCC.height = 1.8f;
                    tricksRemaining = trickCount;

                    if (Input.GetButtonDown("Jump"))
                    {
                        jumpInput = true;
                    }
                }
            }

            if (!isDoingATrick)
            {
                collide = false;

                movDir.x = Input.GetAxis("Horizontal") * originalVel;
                movDir.z = Input.GetAxis("Vertical") * vel;
            }
            #endregion

            #region Rotacion Logica
            h = camSensitivity * Input.GetAxis("Mouse X");
            v = camSensitivity * Input.GetAxis("Mouse Y");
            #endregion

            #region Trucos Logica
            #region Jump Back
            if (isJumping && Input.GetButtonDown("Jump"))
            {
                RaycastHit wallFrontHit;
                Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward) - new Vector3(0, 0.7f, 0), out wallFrontHit, 1f);

                if (wallFrontHit.collider != null && wallFrontHit.collider.gameObject.tag == "Wall")
                {
                    Debug.Log("wall touched");
                    InitTrick();
                    movDir.x = 0;
                    movDir.z = 0;
                    StartCoroutine(JumpBack(new Vector3(0, -180, 0), 0.5f));
                }
            }
            #endregion

            if (tricksRemaining > 0 && !isDoingATrick && Input.GetButtonDown("Trick"))
            {
                #region Wall Climbing
                RaycastHit wallFrontHit;
                Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward) - new Vector3(0, 0.7f, 0), out wallFrontHit, 1f);

                if (wallFrontHit.collider != null && wallFrontHit.collider.gameObject.tag == "Wall")
                {
                    Debug.Log("wall touched");
                    InitTrick();
                    movDir.x = 0;
                    movDir.z = 0;
                    StartCoroutine(WallClimb(0.5f));
                }
                #endregion
                #region Wall Running
                else
                {
                    RaycastHit wallRightHit;
                    RaycastHit wallLeftHit;
                    Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out wallRightHit, 2f);
                    Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), out wallLeftHit, 2f);
                    
                    if (wallRightHit.collider != null && wallRightHit.collider.gameObject.tag == "Wall" && transform.InverseTransformDirection(myCC.velocity).z > 0)
                    {
                        InitTrick();
                        StartCoroutine(WallRunning(Quaternion.FromToRotation(Vector3.left, wallRightHit.normal), movDir.z, wallRunningTime, transform.TransformDirection(Vector3.right), 7));
                    }
                    else if (wallLeftHit.collider != null && wallLeftHit.collider.gameObject.tag == "Wall" && transform.InverseTransformDirection(myCC.velocity).z > 0)
                    {
                        InitTrick();
                        StartCoroutine(WallRunning(Quaternion.FromToRotation(Vector3.right, wallLeftHit.normal), movDir.z, wallRunningTime, transform.TransformDirection(Vector3.left), -7));
                    }
                }
                #endregion
            }
            #endregion
        }
    }

    void FixedUpdate()
    {
        if (!isPaused)
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, (myCC.height / 2) + myCC.skinWidth + 0.05f);
            if (!isGrounded) isGrounded = Physics.Raycast(transform.position, groundCheckDir, 1.1240f); 
            //Doble checkeo, un raycast que apunta ligeramente hacia atras, para las cuestas abajo 

            #region Movimiento

            if (yVel < 0 && !isGrounded) { grav = gravity * jumpGravity; }
            //En la caida la gravedad es mas baja para que sea mas controlable y da una mejor sensacion de control
            else { grav = gravity; }

            if (!isDoingATrick)
            {
                if (isGrounded)
                {
                    isJumping = false;
                    yVel = 0;
                    if (jumpInput)
                    {
                        isJumping = true;
                        yVel = jumpForce;
                        jumpInput = false;
                    }
                }
                yVel -= grav * Time.fixedDeltaTime;
            }
            movDir.y = yVel;

            if (!isDoingATrick) myCC.Move(transform.TransformDirection(movDir));
            else myCC.Move(movDir);
            #endregion

            #region Rotacion

            gameObject.transform.Rotate(0, h, 0);

            camRot = myCam.transform.eulerAngles + new Vector3(-v, 0, 0);
            camRot.x = ClampAngle(camRot.x, -maxCameraRot, maxCameraRot);
            myCam.transform.eulerAngles = camRot;
            #endregion
            
        }
    }

    private IEnumerator WallClimb(float t)
    {
        float actualT = Time.time;
        RaycastHit rHit;
        yVel = wallRunningForce;
        while (Time.time < actualT + t && !collide)
        {
            Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward) - new Vector3(0, 0.7f, 0), out rHit, 1f);
            if(rHit.collider == null || rHit.collider.gameObject.tag != "Wall")
            {
                break;
            }
            else if(Input.GetButtonDown("Jump"))
            {
                StartCoroutine(JumpBack(new Vector3(0, -180, 0), 0.5f));
                yield break;
            }
            yield return new WaitForFixedUpdate();
        }
        isDoingATrick = false;
    }

    private IEnumerator WallRunning(Quaternion rot, float actualVel, float t, Vector3 dir, float camRot)
    {
        float actualT = Time.time;
        RaycastHit rHit;
        transform.rotation = rot;
        myCam.transform.eulerAngles = new Vector3(myCam.transform.eulerAngles.x, myCam.transform.eulerAngles.y, camRot);
        //Pequeña rotacion de la camara para que tenga un impacto visual
        movDir.z = actualVel;
        movDir.x = 0;
        yVel = wallRunningForce / 2;
        movDir = transform.TransformDirection(movDir);
        yield return new WaitForSeconds(0.3f);
        yVel = 0;
        while(Time.time < actualT + t && !collide && Input.GetButton("Trick"))
        {
            Physics.Raycast(transform.position, dir, out rHit, 2f);
            if(rHit.collider == null || rHit.collider.gameObject.tag != "Wall")  break; 
            yield return new WaitForFixedUpdate();
        }
        myCam.transform.eulerAngles = new Vector3(myCam.transform.eulerAngles.x, myCam.transform.eulerAngles.y, 0);
        isDoingATrick = false;
    }

    private IEnumerator JumpBack(Vector3 rotacion, float time)
    {
        isJumping = true;
        yVel = jumpForce / 3.5f;
        movDir = transform.TransformDirection(Quaternion.Euler(rotacion) * Vector3.forward) / 4;
        for (float lenght = time, tim = Time.time; Time.time < tim + lenght;)
        {
            transform.eulerAngles += new Vector3(0, rotacion.y, 0) * (Time.fixedDeltaTime / lenght);
            yield return new WaitForFixedUpdate();
        }
        isDoingATrick = false;
    }

    private void InitTrick()
    {
        isDoingATrick = true;
        myCC.height = 1.2f;
        tricksRemaining--;
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle < 0f) { angle = 360 + angle; }
        if (angle > 180f) { return Mathf.Max(angle, 360 + min); }
        return Mathf.Min(angle, max);
    }

    private void Paused()
    {
        isPaused = true;
        lastMovDir = movDir;
        myCC.Move(Vector3.zero);
    }

    private void Reanude()
    {
        isPaused = false;
        myCC.Move(lastMovDir);
    }

    private void Dead()
    {
        isDead = true;

        movDir.x = 0;
        movDir.z = 0;
        h = 0;
        v = 0;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(isDoingATrick)
        {
            collide = true;
        }
    }
}
