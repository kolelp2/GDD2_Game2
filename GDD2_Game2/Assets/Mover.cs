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
    float turnSpeed = 2.0f;
    void Awake()
    {
        maxMoveSpeed = UnityEngine.Random.value * speedVariance + minSpeed;
    }

	// Use this for initialization
	void Start () {
        velocityUpdateSeed = (int)Math.Round(UnityEngine.Random.value * (velocityUpdateInterval - 1));
    }
	
	// Update is called once per frame
	void Update () {
        //Debug.DrawRay(gameObject.transform.position, velocity, Color.red, Time.deltaTime);
        this.transform.position += (new Vector3(velocity.x, velocity.y)) * Time.deltaTime;
        //work up to the target velocity over time
        if (Time.frameCount % velocityUpdateInterval == velocityUpdateSeed)
            velocity = (velocity + targetVelocity * turnSpeed * Time.deltaTime * velocityUpdateInterval).normalized * targetVelocity.magnitude;
	}

    public void SetVelocity(Vector2 direction, float speed)
    {
        speed = Mathf.Clamp(speed, 0.0f, 1.0f);
        targetVelocity = direction.normalized * maxMoveSpeed * speed;
    }
}
