using UnityEngine;
using System.Collections;

public delegate void ClimbDelegate (string s);

public class HeroClimb : MonoBehaviour 
{
	public bool canClimb = true;
	public LayerMask climbLayers = 1 << 9;
	public float climbSpeed = 0.7f;
	public float checkRadius = 0.4f;
	public float checkDistance = 3.0f;
	public float offsetToWall = 0.4f;
	public float heightOffsetToEdge = 2.12f;
	public float smoothDistanceSpeed = 2.0f;
	public float cornerSideOffset = 0.4f;
	
	public enum ClimbState
	{
		WallClimb,
		LedgeJump,
		WallJump,
		Prepare,
		PreShort,
		Climb,
		Edge,
		Overhang,
		Top,
		Corner,
		None
	}
	public ClimbState climbState = ClimbState.None;
	
	bool atHangR = false;
	bool atHangL = false;
	
	public ClimbDelegate doClimbDel = null;
	
	Transform curT = null;
	Transform lastT = null;
	Transform hero;
	Rigidbody rb;
	Quaternion rot;
	bool isPreparing = false;
	bool isPreShort = false;
	bool inClimbRot = false;
	bool reset = false;
	bool isWall = false;
	bool isPullUp = false;
	bool isOverhang = false;
	bool isLedgeEnd = false;
	bool speedInterval = false;
	Vector3 nextPoint;
	Vector3 lastHeroPos;
	Vector3 inputVec;
	float cacheDist = 0.0f;
	public float edgeSpeed = 0.7f;
	HeroMotor hMove;
	HeroSwim hSwim;
	
	public bool isClimbing = false;
	
	
	//=================================================================================================================o
	void Start () 
	{
		hero = transform;
		rb = rigidbody;
		climbState = ClimbState.None;
		Physics.IgnoreLayerCollision(8,9); // ignore player / climb collision
		hMove = GetComponent <HeroMotor> () as HeroMotor;
		hSwim = GetComponent <HeroSwim> () as HeroSwim;
		cacheDist = checkDistance;
	}
	
	//=================================================================================================================o
	void Update () 
	{
		if (canClimb)
		{
			Check ();
			
			if (!isClimbing) // early out
				return;
			
			// Switch climb states
			switch (climbState)
			{
			case ClimbState.None:
				if (!reset)
				{
					ExitClimb ();
				}
				break;
				
			case ClimbState.WallJump:
				WallJump ();
				reset = false;
				break;
				
			case ClimbState.LedgeJump:
				LedgeJump ();
				reset = false;
				break;	
				
			case ClimbState.WallClimb:
				WallClimb ();
				reset = false;
				break;
				
			case ClimbState.Overhang:
				Vector3 relPoint = curT.InverseTransformPoint (hero.position);
				SmoothToSpot (Mathf.Abs(relPoint.z), offsetToWall, Vector3.forward, Vector3.back);
				SmoothToSpot (Mathf.Abs(relPoint.y), heightOffsetToEdge, Vector3.up, Vector3.down);
				break;
				
			case ClimbState.Climb:
				Climb ();
				reset = false;
				break;
				
			case ClimbState.Corner:
				CornerLerp( climbSpeed);
				reset = false;
				break;
				
			case ClimbState.Edge:
				EdgeMovement ();
				reset = false;
				break;
				
			case ClimbState.Top:
				PullUp ();
				reset = false;
				break;
			}
		}
	}
	//=================================================================================================================o
	
	// Check for ledge collider in front
	void CheckLedge ()
	{
		if (hMove.Grounded) 
		{
			RaycastHit hit;
			
			if (!Physics.Raycast (hero.position + hero.up, hero.forward, 1f, climbLayers))
				return;
			
			else if (Physics.Raycast (hero.position + hero.up, hero.forward, out hit, 1f, climbLayers))
			{
				if (hit.collider.name == "climbLedge")
				{
					isClimbing = true;
					rb.isKinematic = true;
					hMove.canMove = false;
					
					isLedgeEnd = false;
					
					curT = hit.transform;
					lastHeroPos = hero.position;
					
					if (doClimbDel != null) { doClimbDel("Ledge"); }
					climbState = ClimbState.LedgeJump;
				}
			}
		}
	}
	//=================================================================================================================o
	
	// Find next spot and climb up
	void Check ()
	{
		// Action button E to start climbing
		if (Input.GetKeyDown(KeyCode.E) && (climbState == ClimbState.None || climbState == ClimbState.Edge)
			&& hMove.isAfterTap)
		{
			Vector3 p1 = hero.position - (hero.up * -heightOffsetToEdge) + hero.forward;
			Vector3 p2 = hero.position - (hero.up * -heightOffsetToEdge) - hero.forward;
			RaycastHit hit;
			
			CheckLedge ();
			
			// Reduce check distance/height while falling 
			if (!hMove.Grounded && climbState == ClimbState.None && !hSwim.isSwimming)
			{
				checkDistance = 0.4f;
			}
			else 
				checkDistance = cacheDist;
			
			// Hit nothing and not at edge -> Out
			if (!Physics.CapsuleCast (p1, p2, checkRadius, hero.up, out hit, checkDistance, climbLayers) 
				&& (climbState != ClimbState.Edge))
				return;
			
			// Get next edge point location
			else if (Physics.CapsuleCast (p1, p2, checkRadius, hero.up, out hit, checkDistance, climbLayers) 
				&& !isPullUp)
			{
				if ((hit.collider.name == "climbObject" || hit.collider.name == "climbObjectTop" 
					|| hit.collider.name == "climbObjectTopOh" || hit.collider.name == "climbObjectOh") 
					&& hit.transform != curT)
				{
					// If not almost facing the edge cancel/Out
					if (Vector3.Angle (hero.right, hit.transform.right) >= 60.0f)
						return;

					isClimbing = true;
					rb.isKinematic = true;
					hMove.canMove = false;
					
					nextPoint = hit.point;
					lastT = curT;
					curT = hit.transform;
					
					// Hero rotation 
					hero.rotation = hit.transform.rotation;
					inClimbRot = true;
					rot = hero.rotation;
					
					// Jump to edge if grounded or start climbing
					if (hMove.Grounded)
					{
						if (doClimbDel != null) { doClimbDel("WallJump"); }
						climbState = ClimbState.WallJump;
						
						if ((hit.collider.name == "climbObjectTopOh") || (hit.collider.name == "climbObjectTop"))
						{
							isPullUp = true;
							if ((hit.collider.name == "climbObjectTopOh"))
								isOverhang = true;
						}
						else if ((hit.collider.name == "climbObjectOh"))
						{
							isOverhang = true;
						}
					}
					else // Not grounded
					{
						// Standard climb
						if (climbState == ClimbState.Edge) // Prepare & climb up
						{
							if (hit.distance > 1) // Distance is bigger than 
							{
								if (!isPreparing) // if coroutine has finished
								{
									StartCoroutine( Prepare());
								}
								if (hit.collider.name == "climbObjectOh")
									isOverhang = true;
							}
							else // Short climb action
							{
								if (!isPreShort) // if coroutine has finished
								{
									StartCoroutine( PreShort( ClimbState.Climb ));
								}
							}
							if ((hit.collider.name == "climbObjectTop") || (hit.collider.name == "climbObjectTopOh"))
							{
								isPullUp = true;
								if (hit.collider.name == "climbObjectTopOh")
									isOverhang = true;
							}
						}
						else if (climbState == ClimbState.None) // Falling
						{
							if (doClimbDel != null) { doClimbDel("Catch"); } // Trigger catch motion
							climbState = ClimbState.Climb; // move to edge we hit
							
							if ((hit.collider.name == "climbObjectTop") || (hit.collider.name == "climbObjectTopOh"))
							{
								isPullUp = true;
								if (hit.collider.name == "climbObjectTopOh")
									isOverhang = true;
							}
						}
					}
				}
				
				// Hit a climb wall collider 
				else if (hit.collider.name == "climbWall" || hit.collider.name == "climbArea"
					&& climbState != ClimbState.WallClimb && climbState != ClimbState.WallJump)
				{
					isClimbing = true;
					rb.isKinematic = true;
		            hMove.canMove = false;

					nextPoint = hit.point;
					lastT = curT;
					curT = hit.transform;
					
					// Hero rotation 
					hero.rotation = hit.transform.rotation;
					inClimbRot = true;
					rot = hero.rotation;
					
					// Wall-climb-jump modus
					if (curT.name == "climbWall") 
					{
						isWall = true;
						if (hMove.Grounded || hSwim.isSwimming)
						{
							if (doClimbDel != null) { doClimbDel("WallJump"); }
							climbState = ClimbState.WallJump;
						}
						else
						{
							if (doClimbDel != null) { doClimbDel("WallClimb"); }
							climbState = ClimbState.WallClimb;
						}
					}
					else // Wall-climb area
					{
						/*if (doClimbDel != null) { doClimbDel("WallClimb"); }
						climbState = ClimbState.WallJump;*/
						if (!isPreShort && !hMove.Grounded) // if coroutine has finished
						{
							StartCoroutine( PreShort( ClimbState.WallJump ));
						}
						else // Grounded
						{
							if (doClimbDel != null) { doClimbDel("WallClimb"); }
							climbState = ClimbState.WallJump;
						}
					}
				}
			}
			
			// End, climb on top
			else if (isPullUp || isWall)
			{
				isClimbing = true;
				lastHeroPos = hero.position;
				
				if (!isPreparing) // if coroutine has finished , PullUp()
				{
					StartCoroutine( PreTop());
				}
			}
		}
	}
	//=================================================================================================================o
	
	IEnumerator Overhang ()
	{
		if (doClimbDel != null) { doClimbDel("Overhang"); }
		climbState = ClimbState.Overhang;
		
		yield return new WaitForSeconds (2.2f);

		if (doClimbDel != null) { doClimbDel("Edge"); }
			climbState = ClimbState.Edge;
		
		isOverhang = false;
	}
	//=================================================================================================================o
	
	IEnumerator PreTop ()
	{
		isPreparing = true;
		if (curT.name == "climbObjectTopOh")
		{
			if (doClimbDel != null) { doClimbDel("PreOh"); } // Overhang 
		}
		else
		{
			if (doClimbDel != null) { doClimbDel("Prepare"); } // Normal
		}
		climbState = ClimbState.Prepare;
		
		yield return new WaitForSeconds (0.8f);
		
		if (Input.GetKey(KeyCode.E) && climbState == ClimbState.Prepare) // Climb up
		{
			if (doClimbDel != null) { doClimbDel("Top"); }
				climbState = ClimbState.Top; // Out
		}
		else // Stay at position
		{
			yield return new WaitForSeconds (0.2f); // Fade to hang pose
			if (doClimbDel != null) { doClimbDel("Edge"); }
			climbState = ClimbState.Edge;
		}
		isPreparing = false;
	}
	//=================================================================================================================o
	
	IEnumerator Prepare ()
	{
		isPreparing = true;
		if (doClimbDel != null) { doClimbDel("Prepare"); }
		climbState = ClimbState.Prepare;
		
		yield return new WaitForSeconds (0.9f);
		
		if (Input.GetKey(KeyCode.E) && climbState == ClimbState.Prepare) // Climb up
		{
			if (doClimbDel != null) { doClimbDel("Climb"); }
			climbState = ClimbState.Climb; // Move to edge
		}
		else // Stay at position
		{
			isPullUp = false;
			isOverhang = false;
			yield return new WaitForSeconds (0.2f); // Fade to hang pose
			nextPoint = hero.position - ((hero.up * -heightOffsetToEdge) - (hero.forward * offsetToWall)); //Current position
			curT = lastT;
			climbState = ClimbState.Climb; // To last position
		}
		isPreparing = false;
	}
	//=================================================================================================================o
	
	IEnumerator PreShort (ClimbState cS)
	{
		isPreShort = true;
		
		// Switch fast to next step if falling
		float seconds = climbState != ClimbState.None ? 0.75f : 0.0f;
		
		// Delegate to trigger animation state
		if (doClimbDel != null) { doClimbDel("PreShort"); } 
			climbState = ClimbState.PreShort; // prepare for short distance climb (Animation)
		
		yield return new WaitForSeconds (seconds);
		
		if (Input.GetKey(KeyCode.E) && climbState == ClimbState.PreShort) // Climb up
		{
			if (doClimbDel != null) { doClimbDel("Short"); }
			climbState = cS; //ClimbState.Climb; // Move to edge
		}
		else // Stay at position
		{
			isPullUp = false;
			isOverhang = false;
			yield return new WaitForSeconds (0.3f); // Fade to hang pose
			
			if ( curT != null && curT.name != "climbArea")
			{
				nextPoint = hero.position - ((hero.up * -heightOffsetToEdge) - (hero.forward * offsetToWall)); //lastPoint;
				curT = lastT;
				climbState = ClimbState.Climb; // To last
			}
			else // We hit a Wall collider start climbing
			{
				if (doClimbDel != null) { doClimbDel("WallClimb"); }
				climbState = ClimbState.WallClimb;
			}	
		}
		isPreShort = false;
	}
	//=================================================================================================================o
	
	
	void Climb ()
	{
		Vector3 edgePos = nextPoint + ((hero.up * -heightOffsetToEdge) - (hero.forward * offsetToWall));
		
		// Apply lift up
		hero.position = Vector3.Lerp (hero.position, edgePos, Time.deltaTime * climbSpeed * 8 );

		// At edge
		if (Vector3.Distance ( hero.position, edgePos ) <=  0.1f)
		{
			if (isOverhang)
			{
				StartCoroutine( Overhang());
			}
			else
			{
				if (doClimbDel != null) { doClimbDel("Edge"); }
				climbState = ClimbState.Edge;
			}
		}
	}
	//=================================================================================================================o
	
	// Moving sideways on edge
	void EdgeMovement ()
	{
		Vector3 relPoint = curT.InverseTransformPoint (hero.position);
		
		SmoothToSpot (Mathf.Abs(relPoint.z), offsetToWall, Vector3.forward, Vector3.back);
		SmoothToSpot (Mathf.Abs(relPoint.y), heightOffsetToEdge, Vector3.up, Vector3.down);
		
		// Drop down
		if (Input.GetAxis("Vertical") < 0.0f/* || relPoint.z > 5f*/)
		{
			climbState = ClimbState.None;
		}
		// One handed on edge, animation trigger
		else if (Mathf.Abs(relPoint.x) >= 0.44f) 
		{
			// Right side
			if (relPoint.x >= 0.44f) 
			{
				
				if (doClimbDel != null) { doClimbDel("HangR"); }
				atHangR = true; // Hang at right end
				
				if (relPoint.x >= 0.5f)
				{
					climbState = ClimbState.None; // Out
				}
			}
			// Left side
			else if (relPoint.x <= -0.44f) 
			{
				if (doClimbDel != null) { doClimbDel("HangL"); }
				atHangL = true; // Hang at left end
				
				if (relPoint.x <= -0.5f)
				{
					climbState = ClimbState.None; // Out
				}
			}
		}
		
		// Early out
		if (Input.GetAxis ("Horizontal") == 0.0f)
			return;
		
		// Left / Right check for new spots
		else
		{
			Vector3 origin = hero.position - (hero.forward * -offsetToWall) - (hero.up * -heightOffsetToEdge);
			float xInpt = Input.GetAxis ("Horizontal") > 0.0f ? 1.0f : -1.0f;
			Vector3 dir = Input.GetAxis ("Horizontal") > 0.0f ? hero.right : -hero.right;
			cornerSideOffset = Input.GetAxis ("Horizontal") > 0.0f ? -0.4f : 0.4f;
			RaycastHit hit;
			
			if (Physics.SphereCast (origin, 0.2f, dir, out hit, 0.3f, climbLayers))
			{
				nextPoint = hit.point;
				lastT = curT;
				curT = hit.transform;
				
				inClimbRot = true;
				rot = hero.rotation;
				
				if (doClimbDel != null) { doClimbDel("Corner"); }
				climbState = ClimbState.Corner; // Out
			}

			// Speed interval
			if (!speedInterval)
			{
				StartCoroutine( SpeedInterval( 0.2f, climbSpeed)); // 0.2f, climbspeed
			}
			
			// Apply
			hero.Translate( xInpt * Time.deltaTime * edgeSpeed, 0, 0, curT );
			
			// If not close to end
			if ((Mathf.Abs(relPoint.x) < 0.44f) && (atHangR || atHangL)) 
			{
				if (doClimbDel != null) { doClimbDel("Edge"); }
				atHangR = false;
			    atHangL = false;
			}
		}
	}
	//=================================================================================================================o
	
	// Edge climb speed interval
	IEnumerator SpeedInterval (float min, float max)
	{
		speedInterval = true;
		edgeSpeed = min;
		yield return new WaitForSeconds (0.35f);
		edgeSpeed = max;
		yield return new WaitForSeconds (1.4f);
		speedInterval = false;
	}
	//=================================================================================================================o
	
	// Reached the top, PullUp
	void PullUp ()
	{
		Vector3 topVec = lastHeroPos + hero.up * 2.3f;
		Vector3 fwdVec = topVec + hero.forward * 1.5f;
		
		// Step uo
		if (Vector3.Distance (hero.position, lastHeroPos) <= 2.1f)
		{
			hero.position = Vector3.Lerp (hero.position, topVec, Time.deltaTime * climbSpeed *4.5f);
		}
		else // Step forward
		{
			hero.position = Vector3.Lerp (hero.position, fwdVec, Time.deltaTime * climbSpeed *4);
			
			// Exit
			if (Vector3.Distance (topVec, hero.position) >= 0.7f)
			{
				climbState = ClimbState.None;
			}
		}
		isPullUp = false;
	}
	//=================================================================================================================o
	
	// Jump over ledge
	void LedgeJump ()
	{
		Vector3 topVec = new Vector3 (lastHeroPos.x, curT.position.y + 0.3f, lastHeroPos.z);
		Vector3 fwdVec = topVec + hero.forward * 2.0f;
		Vector3 dwnVec = hero.position - (hero.up * 3f);
		
		// Up
		if (((hero.position.y - curT.position.y) <=  0.0f) && !isLedgeEnd)
		{
			hero.position = Vector3.Lerp (hero.position, topVec, Time.deltaTime * climbSpeed * 5f);
		}
		else
		{
			// Forward
			if (!isLedgeEnd)
			{
				hero.position = Vector3.Lerp (hero.position, fwdVec, Time.deltaTime * climbSpeed * 9f);
			}

			// Down & Exit
			if (Vector3.Distance(topVec, hero.position) >= 1.8f) // Forward distance to go over the ledge
			{
				isLedgeEnd = true;
				hero.position = Vector3.Lerp (hero.position, dwnVec, Time.deltaTime * climbSpeed * 1.0f);
				if (hMove.Grounded)
				{
					climbState = ClimbState.None;
				}
			}
		}
	}
	//=================================================================================================================o
	
	// At corner change climb handle
	void CornerLerp (float speed)
	{
		Vector3 v = nextPoint - 
			(hero.right * cornerSideOffset) - (hero.forward * offsetToWall) - (hero.up * heightOffsetToEdge);
		
		float angleTo = Vector3.Angle (hero.right, curT.transform.right);
		
		// Faster if angle is rel small
		speed = angleTo <= 15.0f ?  speed *= 15 : speed *= 4;
		
		// Apply
		hero.position = Vector3.Lerp (hero.position, v, Time.deltaTime * speed);
		hero.rotation = Quaternion.Lerp (hero.rotation, curT.rotation, Time.deltaTime * speed);
		
		// Shift inward a bit 
		if (angleTo <= 0.01f)
		{
			Vector3 relPoint = curT.InverseTransformPoint (hero.position);
			SmoothToSpot (Mathf.Abs(relPoint.z), offsetToWall, Vector3.forward, Vector3.back);
			
			if (doClimbDel != null) { doClimbDel("Edge"); }
			climbState = ClimbState.Edge; // Out
		
		} // Reset hang
		atHangR = false; 
	    atHangL = false;
	}
	//=================================================================================================================o
	
	// Reset if leaving Climb modus
	public void ExitClimb ()
	{
		rb.isKinematic = false;
		hMove.canMove = true;
		atHangR = false;
	    atHangL = false;
		isWall = false;
		isPullUp = false;
		curT = null;
		lastT = null;

		rb.AddForce( Vector3.down * 2, ForceMode.VelocityChange);
		
		// Restore Rotation
		if (inClimbRot)
		{
			hero.rotation = Quaternion.Euler (0, rot.eulerAngles.y, 0);
			inClimbRot = false;
		}
		
		isClimbing = false;
		
		if (doClimbDel != null) { doClimbDel("None"); }
		climbState = ClimbState.None;
		
		// Out
		reset = true;
	}
	//=================================================================================================================o
	
	// Keep correct distance 
	void SmoothToSpot (float dist, float desiredDist, Vector3 positive, Vector3 negative)
	{
		if (dist.ToString("f2") != desiredDist.ToString("f2")) 
		{
			float s = climbSpeed * Time.deltaTime;
			
			if (dist > desiredDist) // forward
			{
				if (dist > (desiredDist + 0.1f)) 
					s *= smoothDistanceSpeed * 2; // Far, move faster
				else
					s /= smoothDistanceSpeed; // / 1.5f Near, move slower
				hero.Translate( positive * s );
			}
			else if (dist < desiredDist) // backward
			{
				if (dist < (desiredDist + 0.1f))
					s *= smoothDistanceSpeed; // Far, move faster
				else
					s /= smoothDistanceSpeed; // Near, move slower
				hero.Translate( negative * s );
			}
		}
	}
	//=================================================================================================================o
	
	// Wall climb handling
	void WallClimb ()
	{
		Vector3 relV = curT.InverseTransformPoint(hero.position);
		
		float distY = Mathf.Abs(relV.y);
		float distZ = Mathf.Abs(relV.z);
		
		// Limit is almost the edge
		if (distY >= heightOffsetToEdge * 1.1f)
		{
			SmoothToSpot (distZ, offsetToWall, Vector3.forward, Vector3.back);
			
			// Input
			if (Input.GetAxis ("Vertical") != 0.0f)
			{
				if (Input.GetAxis ("Vertical") > 0.0f)
				{
					inputVec = hero.TransformPoint( Vector3.up );
				} 
				else 
				{
					inputVec = hero.TransformPoint( Vector3.down );
				
					if (!Physics.Raycast(hero.position, hero.forward, offsetToWall, climbLayers) || hMove.Grounded)
					{
						climbState = ClimbState.None;
					}
				}
				
				// Apply movement
				hero.position = Vector3.Lerp( hero.position, inputVec, Time.deltaTime * climbSpeed * 1.3f);
			}
			else // Stop if there's no input
			{
				inputVec = hero.TransformPoint( Vector3.zero );
			}
		}
		else // Reached the edge
		{
			if (doClimbDel != null) { doClimbDel("Edge"); }
			climbState = ClimbState.Edge; // Out
		}
		
		// Drop
		// Action key down to Exit
		if (Input.GetKeyDown (KeyCode.E) && !hMove.Grounded)
		{
			climbState = ClimbState.None; // Out
		}
	}
	//=================================================================================================================o
	
	// Jump to next spot
	void WallJump ()
	{
		if (hMove.Grounded) // Jump to wall
		{
			// if Grounded and edge is very close, just snap!
			if (Vector3.Distance ( hero.position, nextPoint ) <= 2.5f)
				Climb();
			else // do a walljump 
				hero.Translate( hero.up * Time.deltaTime * climbSpeed * 8 );
		}
		else // Air/Edge
		{
			if (curT.name == "climbArea")
			{
				// Jump to position
				Vector3 edgePos = nextPoint + ((hero.up * -heightOffsetToEdge) - (hero.forward * offsetToWall));
				
				// Apply lift up
				hero.position = Vector3.Lerp (hero.position, edgePos, Time.deltaTime * climbSpeed * 8 ); // 10
				
				// At position
				if (Vector3.Distance ( hero.position, edgePos ) <=  0.1f)
				{
					if (doClimbDel != null) { doClimbDel("WallClimb"); }
					climbState = ClimbState.WallClimb; // Out
				}
			}
			else if (curT.name == "climbWall")
			{
				if (doClimbDel != null) { doClimbDel("WallClimb"); }
				climbState = ClimbState.WallClimb; // Out
			}
			else
				Climb(); // Climb to --> Edge and Out
		}
	}
}
