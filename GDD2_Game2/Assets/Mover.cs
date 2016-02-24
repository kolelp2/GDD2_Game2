using UnityEngine;

public class Mover : MonoBehaviour {
    float maxMoveSpeed = 0.0f;
    Vector2 velocity = new Vector2(0, 0);
    void Awake()
    {
        maxMoveSpeed = Random.value * 2.0f + 1.0f;
    }

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        Debug.DrawRay(gameObject.transform.position, velocity, Color.red, Time.deltaTime);
        this.transform.position += (new Vector3(velocity.x, velocity.y)) * Time.deltaTime;
	}

    public void SetVelocity(Vector2 direction, float speed)
    {
        speed = Mathf.Clamp(speed, 0.0f, 1.0f);
        velocity = direction.normalized * maxMoveSpeed * speed;
    }
}
