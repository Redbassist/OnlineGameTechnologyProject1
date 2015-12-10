using UnityEngine;
using System.Collections;
using System.Diagnostics;

public class Player : MonoBehaviour
{

    public bool player1;
    public float moveSpeed = 4f;
    public float maxSpeed = 5f;
    float angle;

    private Rigidbody2D rb2d;

    Vector2 mousePosition;

    public int boostTimer = 60;
    public int boostFire = 60;
    public float boostPower = 100;

    bool isOn = false;
    bool gameOn = false;

    bool interpolatingMove = false;
    float interpolationTime;
    Stopwatch interpolateWatch;
    Vector2 targetPosition;
    Vector2 positionAtUpdate;
    Vector2 distance;

    Vector2 ghostPos;
    Vector2 ghostVel;

    //starts on
    bool extrapolation = true;

    bool deadReckoning = true;

    float pushTimer = 120;

    // Use this for initialization
    void Start()
    {
        rb2d = gameObject.GetComponent<Rigidbody2D>();
        interpolateWatch = new Stopwatch();
        ghostPos = new Vector2(transform.position.x, transform.position.y);
        ghostVel = rb2d.velocity;

    }

    // Update is called once per frame
    void Update()
    {
        if (gameOn)
        {
            if (player1)
            {
                mousePosition = Input.mousePosition;
                Vector2 objectPos = Camera.main.WorldToScreenPoint(transform.position);
                mousePosition.x = mousePosition.x - objectPos.x;
                mousePosition.y = mousePosition.y - objectPos.y;
                angle = Mathf.Atan2(mousePosition.y, mousePosition.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
                rb2d.MoveRotation(angle);

                boostTimer++;
                pushTimer++;

                float dist = Vector2.Distance(transform.position, ghostPos);

                //whern the distance between the two is larger than specified amount, then send update!
                if (dist > 1 && deadReckoning)
                {
                    //updating ghost position and velocity
                    ghostPos = transform.position;
                    ghostVel = rb2d.velocity;
                    UnityEngine.Debug.Log("Distance exceeded");

                    GameObject net;
                    net = GameObject.Find("Net");
                    net.GetComponent<Network>().SendPlayerUpdate();
                }

                ghostPos.x += ghostVel.x;
                ghostPos.y += ghostVel.y;

                if (boostTimer > 120)
                {
                    maxSpeed = 5f;
                }

                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
                {

                    if (Input.GetKey(KeyCode.W))
                    {
                        rb2d.AddForce(transform.right * moveSpeed);
                    }
                    if (Input.GetKey(KeyCode.S))
                    {
                        rb2d.AddForce(-transform.right * moveSpeed);
                    }
                }
                else
                {
                    rb2d.velocity = new Vector2(rb2d.velocity.x, rb2d.velocity.y) * 0.9f;
                }

                if (rb2d.velocity.magnitude > maxSpeed)
                {
                    rb2d.velocity = rb2d.velocity.normalized * maxSpeed;
                }

                if (Input.GetKey(KeyCode.Space) && boostTimer > 120)
                {
                    boostTimer = 0;
                    maxSpeed = 10f;
                    rb2d.AddForce(transform.right * boostPower);
                }

                if (Input.GetKey(KeyCode.E) && pushTimer > 120)
                {
                    pushTimer = 0;
                    GameObject net;
                    net = GameObject.Find("Net");
                    net.GetComponent<Network>().SendPush(new Vector2(transform.position.x, transform.position.y));
                    gameObject.GetComponent<ParticleSystem>().Play();
                }
            } 
            else
            {
                InterpolateToPosition();
            }
        }
    }

    public void SetPlayer1(bool isPlayer1)
    {
        player1 = isPlayer1;
        gameOn = true;
    }

    public void SetPlayer2(bool isPlayer1)
    {
        gameOn = true;
    }

    public float GetAngle()
    {
        return angle;
    }

    public void IsOn(bool yes)
    {
        isOn = yes;
    }

    public bool IsPlayerOn()
    {
        return isOn;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.name == "Player1" || col.gameObject.name == "SecondPlayer")
        {
            gameOn = false;
            GameObject net;
            net = GameObject.Find("Net");
            net.GetComponent<Network>().SendEndMessage();
        }
    }

    public void StartInterpolation(float time, Vector2 pos)
    {
        positionAtUpdate = transform.position;

        interpolatingMove = true;
        interpolationTime = time;
        //starting watch for interpolating - time since update
        interpolateWatch.Reset();
        interpolateWatch.Start();

        //position trying to get to
        distance = pos - positionAtUpdate;
    }

    public void InterpolateToPosition()
    {
        //Stops the NaN error!
        if (interpolatingMove == true)
        {
            //interpolation to next position
            if (interpolationTime > 0)
            {
                //using distance and the time taken since last update, interpolate towards the position that was sent last.
                Vector2 tempToAddToPos = new Vector2(distance.x * (interpolateWatch.ElapsedMilliseconds / interpolationTime), distance.y * (interpolateWatch.ElapsedMilliseconds / interpolationTime));
                transform.position = new Vector2(positionAtUpdate.x + tempToAddToPos.x, positionAtUpdate.y + tempToAddToPos.y);

                //dead reckoning. The player will continue in the direction and at the speed from the last update till a new update is received
                if (interpolateWatch.ElapsedMilliseconds > interpolationTime && extrapolation)
                {
                    interpolateWatch.Reset();
                    interpolateWatch.Start();
                    positionAtUpdate = transform.position;
                }
            }
        }
    }

    public void HandlePush(Vector2 pos)
    {
        //getting distane between two players
        Vector2 tempPlayerPos = new Vector2(transform.position.x, transform.position.y);
        float distanceBetween = Vector2.Distance(tempPlayerPos, pos);

        //how far away the push will work from
        if (distanceBetween < 2)
        {
            Vector2 direction = (tempPlayerPos - pos).normalized;
            float force = 1000;
            rb2d.AddForce(direction * force);
        }
    }

    public void DeadReckoningUpdate(Vector2 pos, Vector2 vel)
    {
        //updating the player2 position and velocity when there is a difference of a certain amount
        transform.position = pos;
        rb2d.velocity = vel;
    }
}
