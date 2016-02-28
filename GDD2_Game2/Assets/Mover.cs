using UnityEngine;

public class Mover : MonoBehaviour {
    float maxMoveSpeed = 0.0f;
    public float MaxMoveSpeed
    {
        get { return maxMoveSpeed; }
    }
    Vector2 velocity = new Vector2(0, 0);
    Vector2 targetVelocity = new Vector2(0, 0);
    float turnSpeed = 5.0f;
    void Awake()
    {
        maxMoveSpeed = Random.value * 2.0f + 1.0f;
    }

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        //Debug.DrawRay(gameObject.transform.position, velocity, Color.red, Time.deltaTime);
        this.transform.position += (new Vector3(velocity.x, velocity.y)) * Time.deltaTime;
        //work up to the target velocity over time
        velocity = (velocity + targetVelocity * turnSpeed * Time.deltaTime).normalized * targetVelocity.magnitude;
	}

    public void SetVelocity(Vector2 direction, float speed)
    {
        speed = Mathf.Clamp(speed, 0.0f, 1.0f);
        targetVelocity = direction.normalized * maxMoveSpeed * speed;
    }
}
