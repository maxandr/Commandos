using UnityEngine;
using System.Collections;

public delegate void SwimDelegate ();

public class HeroSwim : MonoBehaviour 
{
	public bool canSwim = true;
	
	public float swimSpeed = 2.5f;
	public float rotationSpeed = 1.0f;
	public float waterDrag = 1.4f;
	public float offsetToSurf = 1.6f;
	
	public bool isSwimming = false;
	
	public Vector3 liftVector;
	
	public SwimDelegate doSwimDel = null;
	
	Transform hero;
	Rigidbody rb;
	Transform cam;
	HeroMotor hMove;
	HeroPhysic hPhys;
	HeroCamera hCam;
	HeroClimb hClimb;
	float distY = 0.0f;
	Vector3 diveVec;
	float curAngle = 0.0f;
	
	Transform[] bones;
	Transform rootB = null;
	
	GameObject surfSplash;
	
	void Start ()
	{
		hero = transform;
		rb = rigidbody;
		liftVector = new Vector3 (0, 2.0f, 0);
		hMove = GetComponent <HeroMotor> () as HeroMotor;
		hPhys = GetComponent <HeroPhysic> () as HeroPhysic;
		hCam = GetComponent <HeroCamera> () as HeroCamera;
		hClimb = GetComponent <HeroClimb> () as HeroClimb;
		if (hCam.cam) 
			cam = hCam.cam;
		bones = GetComponentsInChildren <Transform> () as Transform[];
		foreach (Transform t in bones)
		{
			if (t.name == "root")
				rootB = t;
		}
		// FX
		GameObject gO = (GameObject)Resources.Load( "FX_WaterSurf" );
		surfSplash = (GameObject)Instantiate (gO, hero.position, Quaternion.identity);
		surfSplash.active = false;
	}
	
	//=================================================================================================================o
	void FixedUpdate ()
	{
		// Swimming with physix
		if (canSwim && isSwimming && !hClimb.isClimbing)
		{
			rb.drag = waterDrag;
			Floating ();
			MoveDive ();
			
			// Delegate for HeroMotion - SwimState
			if (doSwimDel != null)
			{
			    doSwimDel ();
			}
		}
	}
	//=================================================================================================================o
	void LateUpdate ()
	{
		// Procedural animation 
		if (canSwim && isSwimming && !hMove.Grounded && !hPhys.isRagdoll && !hClimb.isClimbing)
		{
			DiveRotation ();
		}
	}
	//=================================================================================================================o
	
	// Water behaviour
	void Floating ()
	{
		float dTS = Mathf.Abs (distY - hero.position.y);
		
		// Not on the surface
		if (dTS.ToString("f2") != offsetToSurf.ToString("f2")) 
		{
			if (dTS > offsetToSurf) // Under water
			{
				rb.AddForce (liftVector, ForceMode.Acceleration); // normal uplift
			}
			else if (dTS < offsetToSurf && !hMove.Grounded) // On surface
			{
				rb.AddForce (-liftVector *1.5f, ForceMode.Acceleration); // low downforce
			}
			
			// If in ragdoll modus
			if (hPhys.isRagdoll)
			{
				WaterRagdoll ();
			}
			
			// Damp velocity "water effect"
			if (rb.velocity.sqrMagnitude > 6f)
			{
				rb.velocity = Vector3.Lerp(rb.velocity, rb.velocity.normalized, Time.deltaTime * 4);
			}
		}
		// FX
		FXWaterSurf ();
	}
	//=================================================================================================================o
	
	// Movement & Rotation
	void MoveDive ()
	{
		
		// Rotation, if mouse movement
		if (Input.GetAxis ("Mouse X") != 0.0f)
		{
			hero.RotateAround (hero.up, Input.GetAxis ("Mouse X") * Time.fixedDeltaTime * rotationSpeed);
		}
		
		// If input
		if (Input.GetAxis ("Horizontal") != 0.0f || Input.GetAxis ("Vertical") != 0.0f) 
		{
			float diveAngle = Mathf.Round( Vector3.Angle (cam.forward, hero.up));
			// Faster for diving to conquer the up force
			float speed = diveAngle > 150 ? swimSpeed * 3 : swimSpeed; 
			// Dive if camera to hero angle is bigger as 
			if (diveAngle > 140)
			{
				diveVec = -liftVector * waterDrag;
			}
			else
			{
				diveVec = Vector3.zero;
			}
			
			// Movement Vector
			Vector3 swimInput = hero.right * Input.GetAxis ("Horizontal") 
				+ hero.forward * Input.GetAxis ("Vertical") + diveVec;
			
			rb.AddForce (swimInput.normalized * speed, ForceMode.Acceleration);
		}
	}
	//=================================================================================================================o
	
	// Procedural animation for LateUpdate
	void DiveRotation ()
	{
		float targetAngle = Mathf.Abs (Vector3.Angle (cam.forward, hero.up));
		// stay upwards till targetAngle is in scope
		if (targetAngle < 150.0f)
		{
			targetAngle -= 90;
		}
		else if (targetAngle >= 150.0f)
		{
			targetAngle = 140.0f;
		}
		
		curAngle = Mathf.Lerp (curAngle, targetAngle, Time.deltaTime * rotationSpeed);
		// Update our current rotation
		if (curAngle > 5.0f) 
		{
			// Apply rotation
			rootB.RotateAround (rootB.position, hero.right, curAngle);
		}
	}
	
	//=================================================================================================================o
	void WaterRagdoll ()
	{
		// Every rigidbody of the ragdoll
		foreach (Transform t in bones)
		{
			if (t.rigidbody)
			{
				t.rigidbody.drag = waterDrag;
				t.rigidbody.AddForce( liftVector * 3f, ForceMode.Acceleration );
				t.rigidbody.AddTorque( Random.insideUnitSphere * 30f, ForceMode.Acceleration );
			}
		}
		
		// Recover from ragdoll ("Remove/Disable if the character has died")
		if (rootB.rigidbody.velocity.sqrMagnitude < 0.001f)
		{
			hPhys.end = true;
		}
	}
	
	//=================================================================================================================o
	
	// Trigger of the water system
	void OnTriggerEnter (Collider c)
	{
		if ( c.name == "WaterTrigger" )
		{
			isSwimming = true;
			distY = c.bounds.max.y;
			hMove.canMove = false;
			rb.useGravity = false;
			StartCoroutine(LateFX("FX_Splash", 0f));
			surfSplash.active = true;
		}
	}
	void OnTriggerExit (Collider c)
	{
		if ( c.name == "WaterTrigger" )
		{
			isSwimming = false;
			distY = 0.0f;
			
			if (!hPhys.isRagdoll) 
			{
				if (!hClimb.isClimbing) 
				{
					hMove.canMove = true;
				}
				rb.useGravity = true;
			}
			surfSplash.active = false;
		}
	}
	//=================================================================================================================o
	
	// Particle FX instatiation
	IEnumerator LateFX (string nameFX, float time)
	{
		yield return new WaitForSeconds(time);
		GameObject gO = (GameObject)Resources.Load( nameFX );
		Quaternion rot = Quaternion.identity;
		Vector3 pos = hero.position;
		Instantiate( gO, pos, rot );
	}
	void FXWaterSurf ()
	{
		Vector3 sVec = new Vector3(hero.position.x, distY, hero.position.z);
		surfSplash.transform.position = sVec;
	}
}
