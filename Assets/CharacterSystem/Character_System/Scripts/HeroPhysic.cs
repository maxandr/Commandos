using UnityEngine;
using System.Collections;

public delegate void PhysDelegate(string s);

public class HeroPhysic : MonoBehaviour
{
	public bool useRagdoll = true;
	
	public float startRagdollForce = 10.0f;
	public float endRagdollForce = 0.1f;
	
	public float velocityMultip = 1.2f;
	
	public bool start = false;
	public bool end = false;
	public bool isRagdoll = false;
	
	public float hardSoftLandFactor = 2.0f; // Trigger for normal or roll land animation
	
	public float standUpMotionLength = 0.7f;
	
	public PhysDelegate doPhysDel = null;
	
	Rigidbody rb;
	Transform[] bones;
	Transform hero = null;
	Transform rootBone = null;
	
	HeroAnim hMotion;
	HeroMotor hMove;
	HeroClimb hClimb;
	
	//=================================================================================================================o
	void Start ()
	{
		hero = transform;
		rb = rigidbody;
		hMotion = GetComponent <HeroAnim> () as HeroAnim;
		hMove = GetComponent <HeroMotor> () as HeroMotor;
		hClimb = GetComponent <HeroClimb> () as HeroClimb;
		bones = GetComponentsInChildren <Transform> () as Transform[];

		foreach (Transform t in bones)
		{
			if (t.name != hero.name && t.collider && t.rigidbody)
			{
				t.collider.isTrigger = true;
				t.rigidbody.isKinematic = true;
				
				if (t.name == "root") 
					rootBone = t;
			}
		}
	}
	//=================================================================================================================o	
	
	void Update ()
	{
		if (useRagdoll) 
		{
			float velM = rootBone.rigidbody.velocity.magnitude;
			
			if (isRagdoll)
			{
				RecenterHero ();
				if (velM < endRagdollForce)
				{
					StartCoroutine( StandUp( standUpMotionLength ));
				}
				if (end || Input.GetKeyDown(KeyCode.E)) // For ending manually with "E" or end=true 
				{
					EndRagdoll();
				}
			}
			else if (start) // For starting manually
			{
				StartRagdoll();
			}
		}
	}
	//=================================================================================================================o
	
	// Start ragdoll modus
	void StartRagdoll ()
	{
		start = true;
		Vector3 v =  rb.velocity * velocityMultip;
		v.y -= 4; // Extra down force
		hMotion.useProceduralMotions = false;
		
		animation.enabled = false;
		hClimb.ExitClimb();
		hClimb.canClimb = false;
		hMove.canMove = false;
		isRagdoll = true;
		
		if (doPhysDel != null) { doPhysDel("Start"); }
	
		foreach (Transform t in bones)
		{
			if (t.rigidbody && t != hero)
			{
				t.rigidbody.isKinematic = false;
				t.collider.isTrigger = false;
				t.rigidbody.drag = 0.0f;
				t.rigidbody.velocity = v;
			}
		}
		start = false;
	}
	//=================================================================================================================o
	
	// End ragdoll modus
	void EndRagdoll ()
	{
		end = true;
		hMotion.useProceduralMotions = true;
		
		hClimb.canClimb = true;
		hMove.canMove = true;
		isRagdoll = false;
		animation.enabled = true;
		if (doPhysDel != null) { doPhysDel("End"); }
		
		// FX
		StartCoroutine(LateFX("FX_Dust", 0.3f));
		
		foreach (Transform t in bones)
		{
			if (t != hero && t.rigidbody)
			{
				t.rigidbody.isKinematic = true;
				t.collider.isTrigger = true;
			}
		}
		end = false;
	}
	//=================================================================================================================o
	
	// Parent transform stays at ragdoll position
	void RecenterHero ()
	{
		rb.velocity = Vector3.zero;
		Vector3 pos = rootBone.position;
		hero.position = Vector3.Lerp (hero.position, pos, Time.deltaTime * 100.0f);
		rootBone.position = pos;
	}
	//=================================================================================================================o
	
	// Is Ragdoll
	IEnumerator StandUp (float sec)
	{
		if (hMove.Grounded)
		{
			//_end = true;
			Vector3 targetDir = rootBone.forward - hero.up;
			float targetAngle = Vector3.Angle (targetDir, rootBone.forward);
			
			// Negative rotation if shortest route is counter-clockwise
			if (Vector3.Angle (targetDir, -rootBone.up) > Vector3.Angle (targetDir, -rootBone.up * -1))
			{
				targetAngle *= -1.0f;
			}
			
			// Pick animation based on angle
			if (Mathf.Abs (targetAngle) >= 40.0f)
			{
				MainRotation (2);
				isRagdoll = false;
				animation.enabled = true;
				animation.Stop();
				if (doPhysDel != null) { doPhysDel ("Back"); }
				yield return new WaitForSeconds (sec);
				EndRagdoll ();
			}
			else if (Mathf.Abs (targetAngle) <= 40.0f)
			{
				MainRotation (-2);
				isRagdoll = false;
				animation.enabled = true;
				animation.Stop();
				if (doPhysDel != null) { doPhysDel ("Front"); }
				yield return new WaitForSeconds (sec);
				EndRagdoll ();
			}
		}
	}
	//=================================================================================================================o
	
	// Recover rotation before STandUp()
	void MainRotation (float f)
	{
		Vector3 v = rootBone.position + (rootBone.right * f);
		v.y = hero.position.y;
		hero.LookAt(v);
	}
	//=================================================================================================================o
	
	
    void OnCollisionEnter (Collision c)
    {
		// Impact force
    	float f = c.impactForceSum.magnitude;
		
		// If in ragdoll mode
		if (!isRagdoll) 
		{
			// 
			if ( f > (startRagdollForce - hardSoftLandFactor))
			{
				if (doPhysDel != null) { doPhysDel("Roll_Impact"); }
			}
			else
			{
				if (doPhysDel != null) { doPhysDel("Land"); }
			}
			
			// Out if force is to low
			if ( f < startRagdollForce) return;
			
			// Condition to start the ragdoll modus
		  	else if ( f >= startRagdollForce && useRagdoll) 
			{
				StartRagdoll ();
				
				// FX
				SpawnFX ("FX_Dust", c);
				StartCoroutine(LateFX("FX_Dust", 1.3f));
			}
		}
		
		// Collison contact information
    	/*foreach (ContactPoint contact in c.contacts) 
		{
			//print ("normal" + "   " + f);
    		Debug.DrawRay (contact.point, contact.normal, Color.white);
    	}*/
	}
	
	
	
	
	
	// Particle FX instatiation
	void SpawnFX (string nameFX, Collision c)
	{
		GameObject gO = (GameObject)Resources.Load( nameFX );
		ContactPoint cP = c.contacts [0];
		Quaternion rot = Quaternion.FromToRotation( Vector3.up, cP.normal );
		Vector3 pos = cP.point;
		Instantiate( gO, pos, rot );
	}
	
	IEnumerator LateFX (string nameFX, float time)
	{
		yield return new WaitForSeconds(time);
		GameObject gO = (GameObject)Resources.Load( nameFX );
		Quaternion rot = Quaternion.identity;
		Vector3 pos = rootBone.position;
		Instantiate( gO, pos, rot );
	}
}
