using UnityEngine;
using System;

public class Mover : MonoBehaviour {
    float maxMoveSpeed = 0.0f;
    static int velocityUpdateInterval = 1;
    int velocityUpdateSeed;
    static int positionUpdateInterval = 1;
    int positionUpdateSeed;
    static float minSpeed = 1.0f;
    static float speedVariance = 2.0f;
    static int altitudeModUpdateInterval = 4;
    int altitudeModUpdateSeed;
    float altitudeModifier = 1.0f;
    MapInfo mi;
    SpriteRenderer sr;
    public float MaxMoveSpeed
    {
        get { return maxMoveSpeed; }
    }
    Vector2 velocity = new Vector2(0, 0);
    public Vector2 Velocity
    {
        get
        {
            return new Vector2(velocity.x, velocity.y);
        }
    }
    Vector2 targetVelocity = new Vector2(0, 0);
    float turnSpeed = 4.0f; //higher means sharper turns
    void Awake()
    {
        maxMoveSpeed = UnityEngine.Random.value * speedVariance + minSpeed;
    }

	// Use this for initialization
	void Start () {
        mi = (MapInfo)GameObject.Find("Map").GetComponent(typeof(MapInfo));
        sr = (SpriteRenderer)gameObject.GetComponent(typeof(SpriteRenderer));
        velocityUpdateSeed = (int)Math.Round(UnityEngine.Random.value * (velocityUpdateInterval - 1));
        altitudeModUpdateSeed = (int)Math.Round(UnityEngine.Random.value * (altitudeModUpdateInterval - 1));
    }
	
	// Update is called once per frame
	void Update () {
        //Debug.DrawRay(gameObject.transform.position, velocity*altitudeModifier, Color.red, Time.deltaTime);
        this.transform.position += (new Vector3(velocity.x, velocity.y)) * Time.deltaTime * altitudeModifier;
        //work up to the target velocity over time
        if (Time.frameCount % velocityUpdateInterval == velocityUpdateSeed)
            velocity = (velocity + targetVelocity * turnSpeed * Time.deltaTime * velocityUpdateInterval).normalized * targetVelocity.magnitude;
        //check altitude modifier every few frames
        if (Time.frameCount % altitudeModUpdateInterval == altitudeModUpdateSeed)
            altitudeModifier = mi.GetSpeedModifierFromAltitudeAtPos(transform.position);
        if (velocity != Vector2.zero)
        {
            var angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle-90, Vector3.forward);
        }
    }

    public void SetVelocity(Vector2 direction, float speed)
    {
        speed = Mathf.Clamp(speed, 0.0f, 1.0f);
        targetVelocity = direction.normalized * maxMoveSpeed * speed;
    }
}
