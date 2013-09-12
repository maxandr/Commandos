using UnityEngine;
using System.Collections;

public delegate void MotionSendSpeedDelegate(float f);
public delegate void EvadeSwitchDelegate (bool b);
public delegate void MeleeDelegate (string s);

public class HeroAnim : MonoBehaviour 
{
	public bool useMotions = true;
	
	public enum MainState
	{
		Normal,
		Jumping,
		Falling,
		Landing,
		Climbing,
		Swimming,
		Balance,
		Physic,
		Sneak,
		Evade,
		Combat
	}
	
	public enum WeaponState
	{
		Unarmed,
		Sword,
		Bow,
		Rifle,
		Pistol,
		None
	}
	public enum ActionState
	{
		Attack_L1,
		Attack_L2,
		Attack_L3,
		Attack_R1,
		Attack_R2,
		Attack_R3,
		Unarmed_L1,
		Unarmed_L2,
		Unarmed_R1,
		Unarmed_R2,
		DrawSword,
		ThrowShuriken,
		Dance1,
		Dance2,
		Dance3,
		PullLever,
		PushButton,
		ShootBow,
		DrawBow,
		ShootRifle,
		DrawRifle,
		ReloadRifle,
		ShootPistol,
		DrawPistol,
		ReloadPistol,
		None
	}
	public enum EvadeState
	{
		Forward,
		Backward,
		Left,
		Right,
		None
	}
	public enum ClimbState
	{
		WallClimb,
		WallJump,
		Overhang,
		Prepare,
		PreShort,
		PreOh,
		Ledge,
		Catch,
		Climb,
		Short,
		Edge,
		Corner,
		HangL,
		HangR,
		Top,
		None
	}
	
	public float walkSpeed = 0.5f;
	public float runSpeed = 2.7f;
	public float sprintSpeed = 6.0f;
	public float rotateSpeed = 3.0f;
	public float shuffleSpeed = 7.0f;
	public float stopMotionModifier = 1.56f; // magnitude stop-anim trigger
	
	public MainState mainState = MainState.Falling;
	
	public WeaponState weaponState = WeaponState.None;
	public ActionState actionState = ActionState.None;
	public EvadeState evadeState = EvadeState.None;
	public ClimbState climbState = ClimbState.None;
	
	public GameObject sword_Hand = null;
	public GameObject sword_Holster = null;
	public GameObject bow_Hand = null;
	public GameObject bow_Holster = null;
	public GameObject bow_Quiver = null;
	public GameObject rifle_Hand = null;
	public GameObject rifle_Holster = null;
	public GameObject pistol_Hand = null;
	public GameObject pistol_Holster = null;
	
	
	public bool useProceduralMotions = true;
	
	public Transform rootBone;
	public Transform spine;
	public Transform spine2;
	public Transform head;
	
	public MotionSendSpeedDelegate doSpeedDel = null;
	public EvadeSwitchDelegate doSwitchDel = null;
	public MeleeDelegate doMeleeDel = null;
	
	public GameObject lastWeapon = null;
	
	Transform hero;
	Animation a;
	
	float currRot;
	float leanRot;
	Vector3 lastRootForward;
	HeroMotor hMotor;
	HeroClimb hClimb;
	HeroPhysic hPhys;
	HeroSwim hSwim;
	
	bool canLand = true;
	bool inSprintJump = false; // Sprint-jump animation
	bool stopMotion = false; // Abrupt movement stop 
	bool stopMotionSword = false; // Abrupt sword-stance-movement stop 
	bool impactRoll = false; // Do a roll on heavier landings
	AnimationClip impactM; // Land-impact motion
	AnimationClip aC; // Sword,unarmed, evade motion cache
	
	bool climbMotion = false;
	bool prepareMotion = false;
	bool preShortMotion = false;
	bool shortMotion = false;
	bool overhangMotion = false;
	string standUpM = " ";
	
	
	bool isShort = false;
	public bool isWeaponDraw = false;
	int lastMainState = 0;
	
	Vector3 XZVelo 
	{
		get { return new Vector3 (rigidbody.velocity.x, 0.0f, rigidbody.velocity.z); }
	}
	
	void Awake ()
	{
		a = GetComponent <Animation>() as Animation;
		MotionSettings ();
	}
	
	//=================================================================================================================o
	void Start ()
	{
		hero = transform;
		//a = GetComponent <Animation>() as Animation;
		if (a.clip) {
			aC = a.clip;
		} else aC = a["idle"].clip;
		impactM = a["land"].clip;
		
		hMotor = GetComponent <HeroMotor>() as HeroMotor;
		hClimb = GetComponent <HeroClimb>() as HeroClimb;
		hPhys = GetComponent <HeroPhysic>() as HeroPhysic;
		hSwim = GetComponent <HeroSwim>() as HeroSwim;
		
		// Pass Delegates
		hMotor.doJumpDel += DoJump;
		hMotor.doBalanceDel += DoBalance;
		hMotor.doCombatDel += DoCombat;
		hMotor.doSwitchWeapDel += DoSwitchWeapons;
		hMotor.doEvadeDel += DoEvade;
		hMotor.doSneakDel += DoSneak;
		hClimb.doClimbDel += DoClimb;
		hSwim.doSwimDel += DoSwim; 
		hPhys.doPhysDel += DoPhysx;
		
		currRot = 0.0f;
		leanRot = 0.0f;
		//sprintRot = 0.0f;
		lastRootForward = hero.forward;
		
		// Start is in Weaponstate.None, weaponless
		
		// Sword
		if (sword_Hand == null) // If no weapon is assigned
		{
			sword_Hand = new GameObject("emptyObj");
			sword_Hand.active = false;
		}
		else
			sword_Hand.active = false;
		if (sword_Holster == null) // If no weapon is assigned
		{
			sword_Holster = new GameObject("emptyObj");
			sword_Holster.active = false;
		}
		else
			sword_Holster.active = false;
		
		// Bow
		if (bow_Hand == null) // If no weapon is assigned
		{
			bow_Hand = new GameObject("emptyObj");
			bow_Hand.active = false;
		}
		else 
			bow_Hand.active = false;
		if (bow_Holster == null) // If no weapon is assigned
		{
			bow_Holster = new GameObject("emptyObj");
			bow_Holster.active = false;
		}
		else
			bow_Holster.active = false;
		
		// Quiver
		if (bow_Quiver == null) // If no weapon is assigned
		{
			bow_Quiver = new GameObject("emptyObj");
			bow_Quiver.active = false;
		}
		else
			bow_Quiver.active = false;
		
		// Rifle
		if (rifle_Hand == null) // If no weapon is assigned
		{
			rifle_Hand = new GameObject("emptyObj");
			rifle_Hand.active = false;
		}
		else
			rifle_Hand.active = false;
		if (rifle_Holster == null)// If no weapon is assigned
		{
			rifle_Holster = new GameObject("emptyObj");
			rifle_Holster.active = false;
		}
		else
			rifle_Holster.active = false;
		
		// Pistol
		if (pistol_Hand == null)// If no weapon is assigned
		{
			pistol_Hand = new GameObject("emptyObj");
			pistol_Hand.active = false;
		}
		else
			pistol_Hand.active = false;
		if (pistol_Holster == null)// If no weapon is assigned
		{
			pistol_Holster = new GameObject("emptyObj");
			pistol_Holster.active = false;
		}
		else
			pistol_Holster.active = false;
		
		// Look for setup
		DoSwitchWeapons ();
		
		actionState = ActionState.None;
		evadeState = EvadeState.None;
		climbState = ClimbState.None;
	}
	//=================================================================================================================o
	void DoJump ()
	{
		canLand = false;
		mainState = MainState.Jumping;
		Invoke ("Fall", a["jump"].length);
	}
	//=================================================================================================================o
	void DoBalance (bool b)
	{
		if (b)
		{
			mainState = MainState.Balance;
		}
		else
		{
			bool inCombat = sword_Hand.active || bow_Hand.active || rifle_Hand.active || pistol_Hand.active;
			mainState = inCombat ? MainState.Combat : MainState.Normal;
		}
	}
	//=================================================================================================================o
	void DoClimb (string s)
	{
		if (s == "None") { climbState = ClimbState.None; isShort = false; } // Shortclimb reset
		else if (s == "WallClimb") { climbState = ClimbState.WallClimb; }
		else if (s == "WallJump") { climbState = ClimbState.WallJump; }
		else if (s == "PreOh") { climbState = ClimbState.PreOh; }
		else if (s == "Overhang") { climbState = ClimbState.Overhang; isShort = false; }
		else if (s == "Prepare") { climbState = ClimbState.Prepare; }
		else if (s == "PreShort") { climbState = ClimbState.PreShort; }
		else if (s == "Ledge") { climbState = ClimbState.Ledge; }
		else if (s == "Catch") { climbState = ClimbState.Catch; }
		else if (s == "Corner") { climbState = ClimbState.Corner; }
		else if (s == "HangL") { climbState = ClimbState.HangL; }
		else if (s == "HangR") { climbState = ClimbState.HangR; }
		else if (s == "Climb") { climbState = ClimbState.Climb; isShort = false; }
		else if (s == "Short") { climbState = ClimbState.Short; isShort = true; }
		else if (s == "Edge") { climbState = ClimbState.Edge; climbMotion = false; } // XFadeRandom() is ready now
		else if (s == "Top") { climbState = ClimbState.Top; }
		
		mainState = MainState.Climbing;
	}
	//=================================================================================================================o
	void DoSwim ()
	{
		if (hMotor.Grounded) 
		{
			return;
		}
		mainState = MainState.Swimming;
	}
	//=================================================================================================================o
	void DoLand ()
	{
		canLand = false;
		mainState = MainState.Landing;
	}
	//=================================================================================================================o
	void DoCombat ()
	{
		if (!hMotor.Grounded || climbState != ClimbState.None || weaponState == WeaponState.None || isWeaponDraw)
		{
			return;
		}
		// Enable / Disable swords in hand and holster
		
		// Sword
		if (weaponState == WeaponState.Sword)
		{
			if (mainState != MainState.Combat) // Draw
			{
				actionState = ActionState.DrawSword;
				StartCoroutine( DrawHolster(sword_Hand, sword_Holster, 0.4f)); 
				mainState = MainState.Combat;
			}
			else // Holster
			{
				actionState = ActionState.DrawSword;
				StartCoroutine( DrawHolster(sword_Holster, sword_Hand, 0.4f));
				mainState = MainState.Normal;
			}
		}
		// Bow
		else if (weaponState == WeaponState.Bow)
		{
			if (mainState != MainState.Combat) // Draw
			{
				actionState = ActionState.DrawBow;
				StartCoroutine( DrawHolster(bow_Hand, bow_Holster, 0.3f));
				mainState = MainState.Combat;
			}
			else // Holster
			{
				actionState = ActionState.DrawBow;
				StartCoroutine( DrawHolster(bow_Holster, bow_Hand, 0.5f));
				mainState = MainState.Normal;
			}
		}
		// Rifle
		else if (weaponState == WeaponState.Rifle)
		{
			if (mainState != MainState.Combat) // Draw
			{
				actionState = ActionState.DrawRifle;
				StartCoroutine( DrawHolster(rifle_Hand, rifle_Holster, 0.3f));
				mainState = MainState.Combat;
			}
			else // Holster
			{
				actionState = ActionState.DrawRifle;
				StartCoroutine( DrawHolster(rifle_Holster, rifle_Hand, 1.2f));
				mainState = MainState.Normal;
			}
		}
		// Pistol
		else if (weaponState == WeaponState.Pistol)
		{
			if (mainState != MainState.Combat) // Draw
			{
				actionState = ActionState.DrawPistol;
				StartCoroutine( DrawHolster(pistol_Hand, pistol_Holster, 0.5f));
				mainState = MainState.Combat;
			}
			else // Holster
			{
				actionState = ActionState.DrawPistol;
				StartCoroutine( DrawHolster(pistol_Holster, pistol_Hand, 0.6f));
				mainState = MainState.Normal;
			}
		}
	}
	//=================================================================================================================o
	void DoSwitchWeapons ()
	{
		// Not while Drawing/holstering, not in combat modus
		if (mainState == MainState.Combat || isWeaponDraw)
			return;
		
		// Epuip weapon set
		switch (weaponState)
		{
		case WeaponState.Unarmed:
			break;
		case WeaponState.Sword:
			if (sword_Holster.name == "emptyObj") // Skip if no weapon is assigned
			{
				weaponState = WeaponState.Bow;
				if (lastWeapon != null)
				{
					lastWeapon.active = false;
					lastWeapon = null;
				}
				goto case WeaponState.Bow;
			}
			// Switch set
			else if (lastWeapon != null)
			{
				StartCoroutine( OnOffSwitch(sword_Holster, lastWeapon));
			}
			else
				sword_Holster.active = true; // In first cycle, lastweapon is null
			lastWeapon = sword_Holster;
			break;
		case WeaponState.Bow:
			if (bow_Holster.name == "emptyObj") // Skip if no weapon is assigned
			{
				weaponState = WeaponState.Rifle;
				if (lastWeapon != null)
				{
					lastWeapon.active = false;
					lastWeapon = null;
				}
				goto case WeaponState.Rifle;
			}
			// Switch set
			else if (lastWeapon != null)
			{
				StartCoroutine( OnOffSwitch(bow_Holster, lastWeapon));
			}
			lastWeapon = bow_Holster;
			break;
		case WeaponState.Rifle:
			if (rifle_Holster.name == "emptyObj") // Skip if no weapon is assigned
			{
				weaponState = WeaponState.Pistol;
				if (lastWeapon != null)
				{
					lastWeapon.active = false;
					lastWeapon = null;
				}
				goto case WeaponState.Pistol;
			}
			// Switch set
			else if (lastWeapon != null)
			{
				StartCoroutine( OnOffSwitch(rifle_Holster, lastWeapon));
			}
			lastWeapon = rifle_Holster;
			break;
		case WeaponState.Pistol:
			if (pistol_Holster.name == "emptyObj") // Skip if no weapon is assigned
			{
				weaponState = WeaponState.None;
				if (lastWeapon != null)
				{
					lastWeapon.active = false;
					lastWeapon = null;
				}
				goto case WeaponState.None;
			}
			// Switch set
			else if (lastWeapon != null)
			{
				StartCoroutine( OnOffSwitch(pistol_Holster, lastWeapon));
			}
			lastWeapon = pistol_Holster;
			break;
		case WeaponState.None:
			if (lastWeapon != null)
				lastWeapon.active = false; // Weaponless
			break;
		}
	}
	//=================================================================================================================o
	IEnumerator OnOffSwitch (GameObject go_On, GameObject go_Off)
	{
		yield return null;
		go_On.active = true;
		if (weaponState == WeaponState.Bow) // Bow Quiver on
			bow_Quiver.active = true;
		
		go_Off.active = false;
		if (weaponState != WeaponState.Bow) // Bow Quiver off
			bow_Quiver.active = false;
	}
	//=================================================================================================================o
	void DoEvade (string s)
	{
		if (s == "W") evadeState = EvadeState.Forward;
		else if (s == "S") evadeState = EvadeState.Backward;
		else if (s == "A") evadeState = EvadeState.Left;
		else if (s == "D") evadeState = EvadeState.Right;
	
		if (evadeState != EvadeState.None)
		{
			lastMainState = (int)mainState;
			mainState = MainState.Evade;
		}
	}
	//=================================================================================================================o
	void DoSneak (bool b)
	{
		if (b)
		{
			// Only remember Normal or Combat state
			if (mainState == MainState.Combat || mainState == MainState.Normal) 
			{
				lastMainState = (int)mainState;
			}
			mainState = MainState.Sneak;
		}
		else
		{
			if (lastMainState == (int)MainState.Combat || lastMainState == (int)MainState.Normal) 
			{
				mainState = (MainState)lastMainState;
			}
		}
	}
	//=================================================================================================================o
	void AtKStateSpeedSender ()
	{
		// Sends the desired movement speed to the HeroMotor class
		if (doSpeedDel != null)
		{
			// Attacking
			if (actionState == ActionState.Attack_L1) {  doSpeedDel(0.2f); }
			else if (actionState == ActionState.Attack_L2) {  doSpeedDel(0.3f); }
			else if (actionState == ActionState.Attack_L3) {  doSpeedDel(0.2f); }
			else if (actionState == ActionState.Attack_R1) {  doSpeedDel(0.1f); }
			else if (actionState == ActionState.Attack_R2) {  doSpeedDel(0.5f); }
			else if (actionState == ActionState.Attack_R3) {  doSpeedDel(0.1f); }
			else if (actionState == ActionState.Unarmed_L1) {  doSpeedDel(0.3f); }
			else if (actionState == ActionState.Unarmed_L2) {  doSpeedDel(0.3f); }
			else if (actionState == ActionState.Unarmed_R1) {  doSpeedDel(0.1f); }
			else if (actionState == ActionState.Unarmed_R2) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.DrawSword) {  doSpeedDel(0.1f); }
			else if (actionState == ActionState.ThrowShuriken) {  doSpeedDel(0.1f); }
			else if (actionState == ActionState.Dance1) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.Dance2) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.Dance3) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.PullLever) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.PushButton) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.ShootBow) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.DrawBow) {  doSpeedDel(0.0f); }
			//else if (actionState == ActionState.ShootRifle) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.DrawRifle) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.ReloadRifle) {  doSpeedDel(0.0f); }
			//else if (actionState == ActionState.ShootPistol) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.DrawPistol) {  doSpeedDel(0.0f); }
			else if (actionState == ActionState.ReloadPistol) {  doSpeedDel(0.0f); }
			
			// Landing
			else if (mainState == MainState.Landing) {  doSpeedDel(0.0f); }
		}
	}
	//=================================================================================================================o
	void DoPhysx (string s)
	{
		switch (s)
		{
		case "Start":
			EvadeReset();
			mainState = MainState.Physic;
			break;
		case "End":
			bool inCombat = sword_Hand.active || bow_Hand.active || rifle_Hand.active || pistol_Hand.active;
			mainState = inCombat ? MainState.Combat : MainState.Normal;
			break;
		case "Front":
			standUpM = s;
			break;
		case "Back":
			standUpM = s;
			break;
		case "Roll_Impact":
			impactM = a["roll_impact"].clip;
			break;
		case "Land":
			impactM = a["land"].clip;
			break;
		}
	}
	//=================================================================================================================o
	void EvadeMotions ()
	{
		switch (evadeState)
		{
		case EvadeState.Forward:
			if (hMotor.Grounded) 
			{
				a.CrossFade("roll", 0.2f);
				aC = a["roll"].clip;
			}
			else if (!a.IsPlaying("roll"))
				a.CrossFade ("cliff_jump", 0.4f);
			break;
			
		case EvadeState.Backward:
			a.CrossFade("roll_back", 0.1f);
			aC = a["roll_back"].clip;
			break;
			
		case EvadeState.Left:
			a.CrossFade("roll_side_L", 0.2f);
			aC = a["roll_side_L"].clip;
			break;
			
		case EvadeState.Right:
			a.CrossFade("roll_side_R", 0.1f);
			aC = a["roll_side_R"].clip;
			break;
			
		case EvadeState.None:
			break;
		}
		
		// Switch to default if an animation is almost over
		if (a[aC.name].time >= (a[aC.name].length - 0.1f))
		{
			EvadeReset ();
		}
	}
	//=================================================================================================================o
	void EvadeReset ()
	{
		if (doSwitchDel != null) { doSwitchDel (false); } // Double tab is false in HeroMotor
		// Switch to Normal or Combat if last states are..
		if ((MainState)lastMainState == MainState.Balance || 
			(MainState)lastMainState == MainState.Falling || 
			(MainState)lastMainState == MainState.Jumping)
		{
			bool inCombat = sword_Hand.active || bow_Hand.active || rifle_Hand.active || pistol_Hand.active;
			mainState = inCombat ? MainState.Combat : MainState.Normal;
		}
		else
			mainState = (MainState)lastMainState; // Last state before evading
		
		// Reset procedural motion rotations
		currRot = Mathf.Lerp(currRot, 0f, Time.deltaTime);
		leanRot = 0f;
		
		evadeState = EvadeState.None; // Out
	}
	//=================================================================================================================o
	void Fall () 
	{
		if (hMotor.Grounded || climbState != ClimbState.None ||
			hSwim.isSwimming || mainState == MainState.Physic || mainState == MainState.Evade) 
		{
			return;
		}
		mainState = MainState.Falling;
	}
	//=================================================================================================================o
	void Movement ()
	{
		Vector3 movement = XZVelo;
		
		// If not in combat nor evading and sneaking
		if (actionState == ActionState.None && evadeState == EvadeState.None)
		{
			// Stop animation trigger
			if (Input.GetKeyUp (KeyCode.W))
			{
				stopMotion = movement.magnitude > (runSpeed * stopMotionModifier) ? true : false;
			}
			else if (stopMotion && Input.GetAxis("Horizontal") == 0) // stopMotion if not moving sideways
			{
				a.CrossFade ("stop", 0.3f);
				stopMotion = (a["stop"].time) >= 
					(a["stop"].length - 0.01f) ? false : true;
				
				a["stop"].speed = Input.GetKey (KeyCode.W) ? 100.0f : 1.0f;
			}
			// Turning, Moving, Idle etc.
			else if (movement.magnitude < walkSpeed)
			{
				if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
				// Turn around 
				{
					 // Left
					if (Input.GetAxis("Mouse X") < 0.0f)
					{
						a.CrossFade ("turn_L", 0.4f);
					}
					// Right
					else if (Input.GetAxis("Mouse X") > 0.0f)
					{
						a.CrossFade ("turn_R", 0.4f);
					}
					
					lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
							Time.deltaTime * shuffleSpeed);
				}
				else // Idle animation
				{
					a.CrossFade ("idle", 0.4f);
				}
			}
			else
			{
				// Backward / Forward animation handling
				float maxAngl = Input.GetAxis("Vertical") < 0 ? 91.0f : 140.0f;
				
				a["walk"].speed = a["jogging"].speed =
					Vector3.Angle (hero.forward, movement) > maxAngl ? -1.05f : 1.05f;
			
			    //  Move animations
				if (movement.magnitude >= walkSpeed)
				{
					// Walk 
					a.CrossFade ("walk", 0.4f);
					// Jog
					if (movement.magnitude >= runSpeed)
					{
						a.CrossFade ("jogging", 0.4f);
						//  Sprint
					    if (movement.magnitude >= sprintSpeed)
						{
							a["jogging"].speed = 1.5f;  
						}
					}
				}
				lastRootForward = hero.forward;
			}
		}
		// Unarmed animation logic
		if (aC != null && weaponState != WeaponState.None) 
		{
			UnarmedLogic ();
			// Unarmed animations
			UnarmedActions ();
		}
	}
	//=================================================================================================================o
	void Sneak ()
	{
		Vector3 vel = XZVelo;
		if (vel.magnitude < walkSpeed)
		{
			if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
			// Turn around 
			{
				// Left
				if (Input.GetAxis("Mouse X") < 0.0f)
				{
					a.CrossFade ("sneak_turn_L", 0.4f);
				}
				// Right
				else if (Input.GetAxis("Mouse X") > 0.0f)
				{
					a.CrossFade ("sneak_turn_R", 0.4f);
				}
				
				lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
						Time.deltaTime * shuffleSpeed);
			}
			else // Idle
			{ 
				a.CrossFade ("sneak_idle", 0.3f);
			}
		}
		else
		{
			// Backward / Forward animation handling
			float maxAngl = Input.GetAxis("Vertical") < 0 ? 91.0f : 140.0f;
			
			// Backward / forward handling
			a["sneak"].speed = Vector3.Angle (hero.forward, vel) > maxAngl ? -1.0f : 1.0f;
			
		    //  Move
			if (vel.magnitude >= walkSpeed)
			{
				a.CrossFade ("sneak", 0.3f);
			}
			
			lastRootForward = hero.forward;
		}
	}
	//=================================================================================================================o
	void UnarmedLogic ()
	{
		// Left mouse combo chain
		if (Input.GetButtonDown ("Fire1"))
		{
			if (actionState != ActionState.Unarmed_L1) 
			{
				actionState = ActionState.Unarmed_L1;
			}
			else if (actionState == ActionState.Unarmed_L1 && 
				a[aC.name].time > 0.4f)
			{
				actionState = ActionState.Unarmed_L2;
			}
		}
		// Rigt mouse combo chain
		else if (Input.GetButtonDown ("Fire2"))
		{
			if (actionState != ActionState.Unarmed_R1 &&
				actionState != ActionState.Unarmed_R2) 
			{
				actionState = ActionState.Unarmed_R1;
			}
			else if (actionState == ActionState.Unarmed_R1 &&
				a[aC.name].time > 0.4f)
			{
				actionState = ActionState.Unarmed_R2;
			}
		}
		// Holster weapon anim state 
		/*else if (Input.GetKeyDown (KeyCode.C))
		{
			if (weaponState == WeaponState.Sword && sword_Hand.active)
			{
				//actionState = ActionState.DrawSword;
			}
			else if (weaponState == WeaponState.Bow && bow_Hand.active)
			{
				actionState = ActionState.DrawBow;
			}
			else if (weaponState == WeaponState.Rifle && rifle_Hand.active)
			{
				actionState = ActionState.DrawRifle;
			}
			else if (weaponState == WeaponState.Pistol && pistol_Hand.active)
			{
				actionState = ActionState.DrawPistol;
			}
		}*/
		// Throw knife
		else if (Input.GetKeyDown (KeyCode.T))
		{
			actionState = ActionState.ThrowShuriken;
		}
		// Pull lever
		else if (Input.GetKeyDown (KeyCode.B))
		{
			actionState = ActionState.PullLever;
		}
		// Push button
		else if (Input.GetKeyDown (KeyCode.N))
		{
			actionState = ActionState.PushButton;
		}
		// Dance 1
		else if (Input.GetKeyDown (KeyCode.G))
		{
			actionState = ActionState.Dance1;
		}
		// Dance 2
		else if (Input.GetKeyDown (KeyCode.H))
		{
			actionState = ActionState.Dance2;
		}
		// Dance 3
		else if (Input.GetKeyDown (KeyCode.J))
		{
			actionState = ActionState.Dance3;
		}
	}
	//=================================================================================================================o
	void UnarmedActions ()
	{
		switch (actionState)
		{
		case ActionState.Unarmed_L1:
			a.CrossFade("unarmed_punch1", 0.2f);
			aC = a["unarmed_punch1"].clip;
			break;
			
		case ActionState.Unarmed_L2:
			a.CrossFade("unarmed_punch2", 0.1f);
			aC = a["unarmed_punch2"].clip;
			break;
			
		case ActionState.Unarmed_R1:
			a.CrossFade("unarmed_kick1", 0.2f);
			aC = a["unarmed_kick1"].clip;
			break;
			
		case ActionState.Unarmed_R2:
			a.CrossFade("unarmed_kick2", 0.1f);
			aC = a["unarmed_kick2"].clip;
			break;
			
		case ActionState.DrawSword:
			a.CrossFade("sword_holster", 0.3f); // Holster
			aC = a["sword_holster"].clip;
			break;
			
		case ActionState.DrawBow:
			a.CrossFade("bow_holster", 0.3f); // Holster
			aC = a["bow_holster"].clip;
			break;
			
		case ActionState.DrawRifle:
			a.CrossFade("rifle_holster", 0.3f); // Holster
			aC = a["rifle_holster"].clip;
			break;
			
		case ActionState.DrawPistol:
			a.CrossFade("pistol_holster", 0.3f); // Holster
			aC = a["pistol_holster"].clip;
			break;
			
		case ActionState.ThrowShuriken:
			a.CrossFade("throw_shuriken", 0.3f);
			aC = a["throw_shuriken"].clip;
			break;
			
		case ActionState.Dance1:
			a.CrossFade("dance_1", 0.3f);
			aC = a["dance_1"].clip;
			break;
			
		case ActionState.Dance2:
			a.CrossFade("dance_2", 0.3f);
			aC = a["dance_2"].clip;
			break;
			
		case ActionState.Dance3:
			if (!a.IsPlaying("dance_3"))
				a.CrossFade("dance_3", 0.3f);
			aC = a["dance_3"].clip;
			break;
			
		case ActionState.PullLever:
			if (!a.IsPlaying("pull_lever"))
				a.CrossFade("pull_lever", 0.3f);
			aC = a["pull_lever"].clip;
			break;
			
		case ActionState.PushButton:
			if (!a.IsPlaying("push_button"))
				a.CrossFade("push_button", 0.3f);
			aC = a["push_button"].clip;
			break;
			
		case ActionState.None:
			break;
		}
		
		// Switch to default if an animation is almost over
		if (a[aC.name].time > (a[aC.name].length - 0.1f))
		{
			actionState = ActionState.None;
		}
	}
	//=================================================================================================================o
	void JumpOff ()
	{
		// Do a alt jump when sprinting
		if (Input.GetKey (KeyCode.LeftShift))
		{
			inSprintJump = true;
		}
		AnimationClip jumpM = inSprintJump ? a ["jump2"].clip : a ["jump"].clip;
		// If not already playing
		if (!a.IsPlaying(jumpM.name))
		{
			a.CrossFade (jumpM.name, 0.2f);
		}
	}
	//=================================================================================================================o
	void ClimbMovement ()
	{
		float xAxis = Input.GetAxis("Horizontal");
		float yAxis = Input.GetAxis ("Vertical");
		
		switch (climbState)
		{
		case ClimbState.WallClimb:
			if (yAxis != 0.0f) // Forward & backward speed
			{
				a["climb_wallclimb"].speed = yAxis > 0.0f ? 1.0f : -1.0f;
			}
			else // No movement, freeze.
			{
				a["climb_wallclimb"].speed = 0.0f;
			}
			a.CrossFade ("climb_wallclimb", 0.4f);
			break;
			
		case ClimbState.WallJump:
			a.CrossFade ("climb_walljump", 0.4f);
			break;
			
		case ClimbState.PreOh:
			a.CrossFade ("climb_preOh", 0.3f);
			break;
			
		case ClimbState.Overhang:
			if (!overhangMotion)
			{
				StartCoroutine( OverhangRandom (a["climb_overhang"].clip, 
					a["climb_overhang2"].clip, a["climb_overhang3"].clip));
			}
			break;
			
		case ClimbState.Prepare:
			if (!prepareMotion)
			{
				StartCoroutine( PrepareRandom (a["climb_prepare"].clip, 
					a["climb_prepare2"].clip, a["climb_prepare3"].clip));
			}
			break;
			
		case ClimbState.PreShort:
			if (!preShortMotion) 
			{
				StartCoroutine( PreShortRandom (a["climb_preShort"].clip, 
					a["climb_preShort2"].clip, a["climb_preShort3"].clip));
			}
			break;
			
		case ClimbState.Ledge:
			if (!climbMotion)
			{
				StartCoroutine( ClimbRandom (a["climb_ledge"].clip,
					a["climb_ledge2"].clip, a["climb_ledge3"].clip));
				
				a["climb_ledge"].speed = 1.1f;
				a["climb_ledge2"].speed = 1.1f;
				a["climb_ledge3"].speed = 1.1f;
				
			}
			break;
			
		case ClimbState.Catch:
			a.CrossFade ("climb_catch", 0.0f);
			break;
			
		case ClimbState.Short:
			if (!shortMotion)
			{
				StartCoroutine( ShortRandom (a["climb_short"].clip, 
					a["climb_short2"].clip, a["climb_short3"].clip));
			}
			break;
			
		case ClimbState.Climb:
			if (!climbMotion)
			{
				StartCoroutine( ClimbRandom (a["climb_1"].clip,
					a["climb_2"].clip, a["climb_3"].clip));
			}
			break;
			
		case ClimbState.Edge:
			if (xAxis == 0.0f)
			{
				if (isShort)
				{
					a.CrossFade ("climb_hangShort", 0.15f);
				}
				else
					a.CrossFade ("climb_hang", 0.15f); // 0.15f
			}
			else
			{
				if (isShort)
				{
					a.CrossFade ("climb_edgeShort", 0.2f);
				}
				else
					a.CrossFade ("climb_edge", 0.2f);
				
				a["climb_edge"].speed = a["climb_edgeShort"].speed 
					= a["climb_corner"].speed = xAxis > 0.0f ? 1.0f : -1.0f;
			}
			break;
			
		case ClimbState.Top:
			a.CrossFade ("climb_lift", 0.2f);
			break;
			
		case ClimbState.Corner:
			a.CrossFade ("climb_corner", 0.4f);
			break;
			
		case ClimbState.HangL:
			a.CrossFade ("climb_hangL", 0.3f);
			break;
			
		case ClimbState.HangR:
			a.CrossFade ("climb_hangR", 0.3f);
			break;
			
		case ClimbState.None:
			break;
		}
	}
	//=================================================================================================================o
	void BalanceMovement ()
	{
		Vector3 movement = XZVelo;
		
		// If not in combat nor evading and sneaking
		if (actionState == ActionState.None && evadeState == EvadeState.None)
		{
			
			if (movement.magnitude < walkSpeed)
			{
				if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
				// Turn around in in standard and sneak
				{
					 // Left
					if (Input.GetAxis("Mouse X") < 0.0f)
					{
						a.CrossFade ("balance_turn_L", 0.4f);
					}
					// Right
					else if (Input.GetAxis("Mouse X") > 0.0f)
					{
						a.CrossFade ("balance_turn_R", 0.4f);
					}
					
					lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
							Time.deltaTime * shuffleSpeed);
				}
				else // Idle animation
				{
					a.CrossFade ("balance_idle", 0.2f);
				}
			}
			else
			{
				// Backward / forward animation handling
				a["balance_walk"].speed = a["balance_jogging"].speed =
					Vector3.Angle (hero.forward, movement) > 91.0f ? -1.0f : 1.0f;
				
			    // Move animations
				if (movement.magnitude >= walkSpeed)
				{
					// Walk 
					a.CrossFade ("balance_walk", 0.6f);
					// Jog
					if (movement.magnitude >= runSpeed)
					{
						a.CrossFade ("balance_jogging", 0.3f);
					}
				}
				lastRootForward = hero.forward;
			}
		}
	}
	//=================================================================================================================o
	void SwimMovement ()
	{
		float xAxis = Input.GetAxis("Horizontal");
		float yAxis = Input.GetAxis("Vertical");
		
		if (yAxis == 0.0f && xAxis == 0.0f)
		{
			a.CrossFade ("swim", 0.5f);
		}
		else if (yAxis > 0.0f)
		{
			a.CrossFade ("swim_forwards", 0.5f);
		}
		else if (yAxis < 0.0f)
		{
			a.CrossFade ("swim_backwards", 0.5f);
		}
		else if (xAxis < 0.0f)
		{
			a.CrossFade ("swim_left", 0.5f);
		}
		else if (xAxis > 0.0f)
		{
			a.CrossFade ("swim_right", 0.5f);
		}
		
		
		if (evadeState != EvadeState.None) // Cancel Evade
		{
			EvadeReset();
		}
	}
	//=================================================================================================================o
	void FreeFall ()
	{
		a.CrossFade ("fall", 0.4f);
	}
	//=================================================================================================================o
	void Dash ()
	{
		// Is the roll animation playing playing
		impactRoll = a.IsPlaying("roll_impact") && 
			a[impactM.name].time < (a[impactM.name].length - 0.1f) ? true : false;
		
		// PLay Land animation
		a.CrossFade (impactM.name);
	}
	//=================================================================================================================o
	void SwordMovement ()
	{
		Vector3 movement = XZVelo;
		
		// If No Combat animation is running
		if (actionState == ActionState.None && evadeState == EvadeState.None)
		{
			// Stop animation trigger
			if (Input.GetKeyUp (KeyCode.W))
			{
				stopMotionSword = movement.magnitude > (runSpeed * stopMotionModifier) ? true : false;
			}
			else if (stopMotionSword && Input.GetAxis("Horizontal") == 0) // stopMotion if not moving sideways 
			{
				a.CrossFade ("sword_stop");
				stopMotionSword = (a["sword_stop"].time) >= 
					(a["sword_stop"].length - 0.01f) ? false : true;
				
				a["sword_stop"].speed = Input.GetKey(KeyCode.W) ? 100.0f : 1.0f;
			}
			// Turning, Moving, Idle etc.
			else if (movement.magnitude < walkSpeed)
			{
				if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
				// Turn around 
				{
					// Left
					if (Input.GetAxis("Mouse X") < 0.0f)
					{
						a.CrossFade ("sword_turn_L", 0.4f);
					}
					// Right
					else if (Input.GetAxis("Mouse X") > 0.0f)
					{
						a.CrossFade ("sword_turn_R", 0.4f);
					}
					
					lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
							Time.deltaTime * shuffleSpeed);
				}
				else // Idle animation
				{
					a.CrossFade ("sword_idle", 0.4f);
				}
			}
			else
			{
				// Backward / forward animation handling
				float maxAngl = Input.GetAxis("Vertical") < 0 ? 91.0f : 140.0f;
				
				a["sword_walk"].speed = a["sword_jogging"].speed =
					Vector3.Angle (hero.forward, movement) > maxAngl ? -1.0f : 1.0f;
				
			    // Move animations
				if (movement.magnitude >= walkSpeed)
				{
					// Walk
					a.CrossFade ("sword_walk", 0.4f);
					// Jog
					if (movement.magnitude >= runSpeed)
					{
						a.CrossFade ("sword_jogging", 0.4f);
						// Sprint
					    if (movement.magnitude >= sprintSpeed)
						{
							a["sword_jogging"].speed = 1.5f;
						}
					}
				}
				lastRootForward = hero.forward;
			}
		}
		// Trigger sword attacks
		if (aC != null)
		{
			SwordLogic ();
			// Sword animations
			SwordAttacks ();
		}
	}
	//=================================================================================================================o
	void SwordLogic ()
	{
		if (doMeleeDel != null)
		{
			// Left mouse button
			if (Input.GetButtonDown("Fire1"))
			{
				if (actionState != ActionState.Attack_L1 && 
					actionState != ActionState.Attack_L2 && 
					actionState != ActionState.Attack_L3) 
				{
					actionState = ActionState.Attack_L1;
					doMeleeDel("Atk_L1");  // Trail FX Delegate
				}
				else if (actionState == ActionState.Attack_L1 && 
					a[aC.name].time > 0.4f)
				{
					actionState = ActionState.Attack_L2;
					doMeleeDel("Atk_L2");
				}
				else if (actionState == ActionState.Attack_L2 && 
					a[aC.name].time > 0.5f)
				{
					actionState = ActionState.Attack_L3;
					doMeleeDel("Atk_L3");
				}
			}
			
			// Right mouse button
			else if (Input.GetButtonDown("Fire2"))
			{
				if (actionState != ActionState.Attack_R1 && 
					actionState != ActionState.Attack_R2 && 
					actionState != ActionState.Attack_R3) 
				{
					actionState = ActionState.Attack_R1;
					doMeleeDel("Atk_R1");
				}
				else if (actionState == ActionState.Attack_R1 && 
					a[aC.name].time > 0.4f)
				{
					actionState = ActionState.Attack_R2;
					doMeleeDel("Atk_R2");
				}
				else if (actionState == ActionState.Attack_R2 && 
					a[aC.name].time > 0.5f)
				{
					actionState = ActionState.Attack_R3;
					doMeleeDel("Atk_R3");
				}
			}
			/*else if (Input.GetKeyDown (KeyCode.C)) // Draw
			{
				actionState = ActionState.DrawSword;
			}*/
		}
	}
	//=================================================================================================================o
	void SwordAttacks ()
	{
		switch (actionState)
		{
		case ActionState.Attack_L1:
			a.CrossFade("sword_slash1", 0.1f);
			aC = a["sword_slash1"].clip;
			break;
			
		case ActionState.Attack_L2:
			a.CrossFade("sword_slash2", 0.2f);
			aC = a["sword_slash2"].clip;
			break;
			
		case ActionState.Attack_L3:
			a.CrossFade("sword_slash2.5", 0.1f);
			aC = a["sword_slash2.5"].clip;
			break;
			
		case ActionState.Attack_R1:
			a.CrossFade("sword_slash3", 0.1f);
			aC = a["sword_slash3"].clip;
			break;
			
		case ActionState.Attack_R2:
			a.CrossFade("sword_slash4", 0.2f);
			aC = a["sword_slash4"].clip;
			break;
			
		case ActionState.Attack_R3:
			a.CrossFade("sword_slash5", 0.2f);
			aC = a["sword_slash5"].clip;
			break;
			
		case ActionState.DrawSword:
			a.CrossFade("sword_draw", 0.3f);
			aC = a["sword_draw"].clip;
			break;
			
		case ActionState.None:
			break;
		}
		
		// Switch to default if an animation is almost over
		if (a[aC.name].time > (a[aC.name].length - 0.1f))
		{
			actionState = ActionState.None;
			if (doMeleeDel != null) { doMeleeDel("None"); }
		}
	}
	//=================================================================================================================o
	void BowMovement ()
	{
		Vector3 movement = XZVelo;
		
		// If No Combat animation is running
		if (actionState == ActionState.None && evadeState == EvadeState.None)
		{
			// Stop animation trigger
			if (Input.GetKeyUp (KeyCode.W))
			{
				stopMotionSword = movement.magnitude > (runSpeed * stopMotionModifier) ? true : false;
			}
			else if (stopMotionSword && Input.GetAxis("Horizontal") == 0) // stopMotion if not moving sideways 
			{
				a.CrossFade ("stop");
				stopMotionSword = (a["stop"].time) >= 
					(a["stop"].length - 0.01f) ? false : true;
				
				a["stop"].speed = Input.GetKey(KeyCode.W) ? 100.0f : 1.0f;
			}
			// Turning, Moving, Idle etc.
			else if (movement.magnitude < walkSpeed)
			{
				if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
				// Turn around 
				{
					// Left
					if (Input.GetAxis("Mouse X") < 0.0f)
					{
						a.CrossFade ("bow_turn_L", 0.4f);
					}
					// Right
					else if (Input.GetAxis("Mouse X") > 0.0f)
					{
						a.CrossFade ("bow_turn_R", 0.4f);
					}
					
					lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
							Time.deltaTime * shuffleSpeed);
				}
				else // Idle animation
				{
					a.CrossFade ("bow_idle", 0.4f);
				}
			}
			else
			{
				// Backward / forward animation handling
				float maxAngl = Input.GetAxis("Vertical") < 0 ? 91.0f : 140.0f;
				
				a["bow_walk"].speed = a["bow_jogging"].speed =
					Vector3.Angle (hero.forward, movement) > maxAngl ? -1.0f : 1.0f;
				
			    // Move animations
				if (movement.magnitude >= walkSpeed)
				{
					// Walk
					a.CrossFade ("bow_walk", 0.4f);
					// Jog
					if (movement.magnitude >= runSpeed)
					{
						a.CrossFade ("bow_jogging", 0.4f);
						// Sprint
					    if (movement.magnitude >= sprintSpeed)
						{
							a["bow_jogging"].speed = 1.5f;
						}
					}
				}
				lastRootForward = hero.forward;
			}
		}
		// Trigger bow attack & draw/holster bow
		if (aC != null)
		{
			// Left mouse button & bow shoot is not playing already
			if (Input.GetButtonDown("Fire1") && !a.IsPlaying("bow_shoot"))
			{
				if (actionState != ActionState.ShootBow)
					actionState = ActionState.ShootBow;
			}
			// "C" for combat modus
			else if (Input.GetKeyDown (KeyCode.C))
			{
				actionState = ActionState.DrawBow;
			}
			
			// Play animation
			if (actionState == ActionState.ShootBow)
			{
				a.CrossFade("bow_shoot", 0.1f);
				aC = a["bow_shoot"].clip;
			}
			else if (actionState == ActionState.DrawBow)
			{
				a.CrossFade("bow_draw", 0.1f);
				aC = a["bow_draw"].clip;
			}
			
			// Switch to default if an animation is almost over
			if (a[aC.name].time > (a[aC.name].length - 0.1f))
			{
				actionState = ActionState.None;
			}
		}
	}
	//=================================================================================================================o
	void RifleMovement ()
	{
		Vector3 movement = XZVelo;
		
		// If No Combat animation is running
		if (actionState == ActionState.None && evadeState == EvadeState.None)
		{
			// Stop animation trigger
			if (Input.GetKeyUp (KeyCode.W))
			{
				stopMotionSword = movement.magnitude > (runSpeed * stopMotionModifier) ? true : false;
			}
			else if (stopMotionSword && Input.GetAxis("Horizontal") == 0) // stopMotion if not moving sideways 
			{
				a.CrossFade ("stop");
				stopMotionSword = (a["stop"].time) >= 
					(a["stop"].length - 0.01f) ? false : true;
				
				a["stop"].speed = Input.GetKey(KeyCode.W) ? 100.0f : 1.0f;
			}
			// Turning, Moving, Idle etc.
			else if (movement.magnitude < walkSpeed)
			{
				if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
				// Turn around
				{
					// Left
					if (Input.GetAxis("Mouse X") < 0.0f)
					{
						a.CrossFade ("rifle_turn_L", 0.4f);
					}
					// Right
					else if (Input.GetAxis("Mouse X") > 0.0f)
					{
						a.CrossFade ("rifle_turn_R", 0.4f);
					}
					
					lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
							Time.deltaTime * shuffleSpeed);
				}
				else // Idle animation
				{
					if (!a.IsPlaying("rifle_idle2"))
						a.CrossFade ("rifle_idle2", 0.4f);
				}
			}
			else
			{
				// Backward / forward animation handling
				float maxAngl = Input.GetAxis("Vertical") < 0 ? 91.0f : 140.0f;
				
				a["rifle_walk"].speed = a["rifle_jogging"].speed =
					Vector3.Angle (hero.forward, movement) > maxAngl ? -1.0f : 1.0f;
				
			    // Move animations
				if (movement.magnitude >= walkSpeed)
				{
					// Walk
					a.CrossFade ("rifle_walk", 0.4f);
					// Jog
					if (movement.magnitude >= runSpeed)
					{
						a.CrossFade ("rifle_jogging", 0.4f);
						// Sprint
					    if (movement.magnitude >= sprintSpeed)
						{
							a["rifle_jogging"].speed = 1.5f;
						}
					}
				}
				lastRootForward = hero.forward;
			}
		}
		// Trigger rifle attack & draw/holster rifle
		if (aC != null)
		{
			// Left mouse button
			if (Input.GetButton("Fire1"))
			{
				if (actionState != ActionState.ShootRifle)
					actionState = ActionState.ShootRifle;
				if (doSpeedDel != null) { doSpeedDel(0.0f); }
			}
			// "C" for combat modus
			else if (Input.GetKeyDown (KeyCode.C))
			{
				actionState = ActionState.DrawRifle;
			}
			// "R" for Reload
			else if (Input.GetKeyDown (KeyCode.R))
			{
				actionState = ActionState.ReloadRifle;
			}
			
			// Play animation
			if (actionState == ActionState.ShootRifle)
			{
				a.CrossFade("rifle_shoot", 0.1f);
				aC = a["rifle_shoot"].clip;
			}
			else if (actionState == ActionState.DrawRifle)
			{
				a.CrossFade("rifle_draw", 0.1f);
				aC = a["rifle_draw"].clip;
			}
			else if (actionState == ActionState.ReloadRifle)
			{
				a.CrossFade("rifle_reload", 0.1f);
				aC = a["rifle_reload"].clip;
			}
			
			// Switch to default if an animation is almost over
			if (a[aC.name].time > (a[aC.name].length - 0.1f))
			{
				actionState = ActionState.None;
			}
		}
	}
	//=================================================================================================================o
	void PistolMovement ()
	{
		Vector3 movement = XZVelo;
		
		// If No Combat animation is running
		if (actionState == ActionState.None && evadeState == EvadeState.None)
		{
			// Stop animation trigger
			if (Input.GetKeyUp (KeyCode.W))
			{
				stopMotionSword = movement.magnitude > (runSpeed * stopMotionModifier) ? true : false;
			}
			else if (stopMotionSword && Input.GetAxis("Horizontal") == 0) // stopMotion if not moving sideways 
			{
				a.CrossFade ("stop");
				stopMotionSword = (a["stop"].time) >= 
					(a["stop"].length - 0.01f) ? false : true;
				
				a["stop"].speed = Input.GetKey(KeyCode.W) ? 100.0f : 1.0f;
			}
			// Turning, Moving, Idle etc.
			else if (movement.magnitude < walkSpeed)
			{
				if (Vector3.Angle (lastRootForward, hero.forward) > 1.0f)
				// Turn around
				{
					// Left
					if (Input.GetAxis("Mouse X") < 0.0f)
					{
						a.CrossFade ("pistol_turn_L", 0.4f);
					}
					// Right
					else if (Input.GetAxis("Mouse X") > 0.0f)
					{
						a.CrossFade ("pistol_turn_R", 0.4f);
					}
			
					lastRootForward = Vector3.Slerp (lastRootForward, hero.forward, 
							Time.deltaTime * shuffleSpeed);
				}
				else // Idle animation
				{
					if (!a.IsPlaying("pistol_idle1"))
						a.CrossFade ("pistol_idle1", 0.4f);
				}
			}
			else
			{
				// Backward / forward animation handling
				float maxAngl = Input.GetAxis("Vertical") < 0 ? 91.0f : 140.0f;
				
				a["pistol_walk"].speed = a["pistol_jogging"].speed =
					Vector3.Angle (hero.forward, movement) > maxAngl ? -1.0f : 1.0f;
				
			    //  Move animations
				if (movement.magnitude >= walkSpeed)
				{
					// Walk
					a.CrossFade ("pistol_walk", 0.4f);
					// Jog
					if (movement.magnitude >= runSpeed)
					{
						a.CrossFade ("pistol_jogging", 0.4f);
						// Sprint
						if (movement.magnitude >= sprintSpeed)
						{
							a["pistol_jogging"].speed = 1.5f;
						}
					}
				}
				lastRootForward = hero.forward;
			}
		}
		// Trigger pistol attack & draw/holster pistol
		if (aC != null)
		{
			// Left mouse button
			if (Input.GetButton("Fire1"))
			{
				if (actionState != ActionState.ShootPistol)
					actionState = ActionState.ShootPistol;
				if (doSpeedDel != null) { doSpeedDel(0.0f); }
			}
			// "C" for combat modus
			else if (Input.GetKeyDown (KeyCode.C))
			{
				actionState = ActionState.DrawPistol;
			}
			// "R" for Reload
			else if (Input.GetKeyDown (KeyCode.R))
			{
				actionState = ActionState.ReloadPistol;
			}
			// Play animation
			if (actionState == ActionState.ShootPistol)
			{
				a.CrossFade("pistol_shoot", 0.1f);
				aC = a["pistol_shoot"].clip;
			}
			else if (actionState == ActionState.DrawPistol)
			{
				a.CrossFade("pistol_draw", 0.1f);
				aC = a["pistol_draw"].clip;
			}
			else if (actionState == ActionState.ReloadPistol)
			{
				a.CrossFade("pistol_reload", 0.1f);
				aC = a["pistol_reload"].clip;
			}
			
			// Switch to default if an animation is almost over
			if (a[aC.name].time > (a[aC.name].length - 0.1f))
			{
				actionState = ActionState.None;
			}
		}
	}
	//=================================================================================================================o
	void Update ()
	{
		if (useMotions)
		{
			switch (mainState) 
			{
			case MainState.Normal:
				Movement ();
				break;
			case MainState.Jumping:
				JumpOff ();
				break;
			case MainState.Falling:
				FreeFall ();
				break;
			case MainState.Landing:
				Dash ();
				break;
			case MainState.Climbing:
				ClimbMovement ();
				break;
			case MainState.Swimming:
				SwimMovement ();
				break;
			case MainState.Balance:
				BalanceMovement ();
				break;
			case MainState.Sneak:
				Sneak ();
				break;
			case MainState.Evade:
				EvadeMotions ();
				break;
			case MainState.Physic:
				if (standUpM == "Front")
				{
					a.CrossFade("stand_up", 0.2f);
				}
				else if (standUpM == "Back")
				{
					a.CrossFade("stand_up_back", 0.2f);
				}
				break;
			case MainState.Combat:
				//SwordMovement ();
				
				switch (weaponState) // Switch with "Q" in HeroMotor
				{
				case WeaponState.Unarmed:
					Movement ();
					break;
				case WeaponState.Sword:
					SwordMovement ();
					break;
				case WeaponState.Bow:
					BowMovement ();
					break;
				case WeaponState.Rifle:
					RifleMovement ();
					break;
				case WeaponState.Pistol:
					PistolMovement ();
					break;
				case WeaponState.None:
					break;
				}
				break;
			}
		}
	}
	//=================================================================================================================o
	void FixedUpdate ()
	{
		if (useMotions)
		{
			if (hMotor.Grounded)
			{
				inSprintJump = false;
				
				bool inCombat = sword_Hand.active || bow_Hand.active || rifle_Hand.active || pistol_Hand.active;
				
				AtKStateSpeedSender ();
				
				// Falling or/and Jumping & Gounded
				if (mainState == MainState.Falling || (mainState == MainState.Jumping && canLand)) 
				{
					DoLand ();
				}
				
				// Jumping, Grounded
				else if (mainState == MainState.Jumping)
				{
					canLand = true;
				}
				
				// Landing smoothness, Grounded
				else if (mainState == MainState.Landing && !impactRoll)
				{
					
					// Start moving
					if (XZVelo.magnitude > walkSpeed *10) 
					{
						mainState = inCombat ? MainState.Combat : MainState.Normal;
					}
					// Back to "normal" if land motion is almost over
					else if (a[impactM.name].time >= (a[impactM.name].length - 0.3f))
					{
						mainState = inCombat ? MainState.Combat : MainState.Normal;
					}
				}
				
				// Climb handling, Grounded
				else if (mainState == MainState.Climbing)
				{
					if (climbState == ClimbState.None)
					{
						//mainState = MainState.Normal;
						mainState = inCombat ? MainState.Combat : MainState.Normal;
					}
				}
				
				// Swim handling, Grounded
				else if (mainState == MainState.Swimming)
				{
					//mainState = MainState.Normal;
					mainState = inCombat ? MainState.Combat : MainState.Normal;
				}
			}
			
			// Not Grounded
			else if (mainState == MainState.Jumping)
			{
				canLand = true;
			}
			else
			{
				Fall ();
			}
		}
	}
	//=================================================================================================================o
	
	void LateUpdate ()
	{
		if (useMotions) 
		{
			if (useProceduralMotions) 
			{
				Vector3 movement = XZVelo;
				float targetAngle = Vector3.Angle (movement, new Vector3 (hero.forward.x, 0.0f, hero.forward.z));
				// Only calculate if we are moving
				if (movement.magnitude >= 1)
				{
					// Negative rotation if shortest angle
					if (Vector3.Angle (movement, hero.right) > Vector3.Angle (movement, hero.right * -1))
					{
						targetAngle *= -1.0f;
					}
					
					// When walking backwards, don't rotate over 90 degrees and rotate opposite
					if (Mathf.Abs (targetAngle) > 91.0f && Input.GetAxis("Vertical") < 0)
					{
						targetAngle = targetAngle + (targetAngle > 0 ? -180.0f : 180.0f);
					}
					
					// Moving backwards without input after (Evade, combat etc.)
					else if (Mathf.Abs (targetAngle) > 91.0f && XZVelo.z < 0 && Input.GetAxis("Horizontal") == 0)
					{
						targetAngle = 0;
					}
					
					// lean left and right
					if (movement.magnitude >= runSpeed && Input.GetAxis ("Horizontal") == 0)
					{	
					    leanRot = Mathf.Lerp (leanRot, targetAngle, Time.deltaTime * rotateSpeed);
					}
				}
				else // Back to normal
				{
					leanRot = 0.0f;
					targetAngle = 0.0f;
					currRot = Mathf.Lerp (currRot, 0f, Time.deltaTime * rotateSpeed);
				}
				
				// Rotate bones, if grounded and not in an evade action
				if (hMotor.Grounded && evadeState == EvadeState.None)
				{
					// Update rotation
					currRot = Mathf.Lerp (currRot, targetAngle, Time.deltaTime * rotateSpeed);
					
					// Lean left and right, even in the air
					rootBone.RotateAround (rootBone.position, hero.forward, -leanRot * -0.3f);
					//spine.RotateAround (spine.position, hero.forward, leanRot * -0.6f);
					
					// Rotate the model at the root level
					rootBone.RotateAround (rootBone.position, hero.up, currRot);
					// Upper body rotation while moving
					spine.RotateAround (spine.position, hero.up, currRot * -0.3f);
					spine2.RotateAround (spine2.position, hero.up, currRot * -0.3f);
					head.RotateAround (head.position, hero.up, currRot * -0.3f);
				}
			}
		}
	}
	//=================================================================================================================o
	
	IEnumerator OverhangRandom (AnimationClip a1, AnimationClip a2, AnimationClip a3)
	{
		overhangMotion = true;
		int index = Random.Range(1,4);
		
		if (index == 1)
		{
			a.CrossFade(a1.name, 0.1f);
			yield return new WaitForSeconds (a1.length);
			overhangMotion = false;
		}
		else if (index == 2)
		{
			a.CrossFade(a2.name, 0.1f);
			yield return new WaitForSeconds (a2.length);
			overhangMotion = false;
		}
		else if (index == 3)
		{
			a.CrossFade(a3.name, 0.1f);
			yield return new WaitForSeconds (a3.length);
			overhangMotion = false;
		}
	}
	//=================================================================================================================o
	IEnumerator ClimbRandom (AnimationClip a1, AnimationClip a2, AnimationClip a3)
	{
		climbMotion = true;
		int index = Random.Range(1,4);
		
		if (index == 1)
		{
			a.CrossFade(a1.name, 0.1f);
			yield return new WaitForSeconds (a1.length);
			climbMotion = false;
		}
		else if (index == 2)
		{
			a.CrossFade(a2.name, 0.1f);
			yield return new WaitForSeconds (a2.length);
			climbMotion = false;
		}
		else if (index == 3)
		{
			a.CrossFade(a3.name, 0.1f);
			yield return new WaitForSeconds (a3.length);
			climbMotion = false;
		}
	}
	//=================================================================================================================o
	IEnumerator PrepareRandom (AnimationClip a1, AnimationClip a2, AnimationClip a3)
	{
		prepareMotion = true;
		int index = Random.Range(1,4);
		
		if (index == 1)
		{
			a.CrossFade(a1.name, 0.1f);
			yield return new WaitForSeconds (a1.length);
			prepareMotion = false;
		}
		else if (index == 2)
		{
			a.CrossFade(a2.name, 0.1f);
			yield return new WaitForSeconds (a2.length);
			prepareMotion = false;
		}
		else if (index == 3)
		{
			a.CrossFade(a3.name, 0.1f);
			yield return new WaitForSeconds (a3.length);
			prepareMotion = false;
		}
	}
	//=================================================================================================================o
	IEnumerator PreShortRandom (AnimationClip a1, AnimationClip a2, AnimationClip a3)
	{
		preShortMotion = true;
		int index = Random.Range(1,4);
		
		if (index == 1)
		{
			a.CrossFade(a1.name, 0.1f);
			yield return new WaitForSeconds (a1.length);
			preShortMotion = false;
		}
		else if (index == 2)
		{
			a.CrossFade(a2.name, 0.1f);
			yield return new WaitForSeconds (a2.length);
			preShortMotion = false;
		}
		else if (index == 3)
		{
			a.CrossFade(a3.name, 0.1f);
			yield return new WaitForSeconds (a3.length);
			preShortMotion = false;
		}
	}
	//=================================================================================================================o
	IEnumerator ShortRandom (AnimationClip a1, AnimationClip a2, AnimationClip a3)
	{
		shortMotion = true;
		int index = Random.Range(1,4);
		
		if (index == 1)
		{
			a.CrossFade(a1.name, 0.1f);
			yield return new WaitForSeconds (a1.length);
			shortMotion = false;
		}
		else if (index == 2)
		{
			a.CrossFade(a2.name, 0.1f);
			yield return new WaitForSeconds (a2.length);
			shortMotion = false;
		}
		else if (index == 3)
		{
			a.CrossFade(a3.name, 0.1f);
			yield return new WaitForSeconds (a3.length);
			shortMotion = false;
		}
	}
	//=================================================================================================================o
	
	// Time switch coroutine
	IEnumerator DrawHolster (GameObject go_On, GameObject go_Off, float seconds)
	{
		isWeaponDraw = true;
		yield return new WaitForSeconds (seconds);
		go_On.active = true;
		go_Off.active = false;
		// "Cooldown" till the stance can be switched again
		yield return new WaitForSeconds (1f);
		isWeaponDraw = false;
	}
	//=================================================================================================================o
	
	// Wrapmode and time settings
	void MotionSettings ()
	{
		// Speed settings
		a["sneak_turn_L"].speed = 1.1f;
		a["sneak_turn_R"].speed = 1.1f;
		a["sword_turn_L"].speed = 1.1f;
		a["sword_turn_R"].speed = 1.1f;
		a["turn_L"].speed = 1.1f;
		a["turn_R"].speed = 1.1f;
		
		// Balance
		a["balance_idle"].wrapMode = WrapMode.Loop;
		a["balance_jogging"].wrapMode = WrapMode.Loop;
		a["balance_turn_L"].wrapMode = WrapMode.Loop;
		a["balance_turn_R"].wrapMode = WrapMode.Loop;
		a["balance_walk"].wrapMode = WrapMode.Loop;
		
		a["cliff_jump"].wrapMode = WrapMode.ClampForever;
		
		// Climb
		a["climb_1"].wrapMode = WrapMode.Once;
		a["climb_2"].wrapMode = WrapMode.Once;
		a["climb_3"].wrapMode = WrapMode.Once;
		a["climb_catch"].wrapMode = WrapMode.ClampForever;
		a["climb_corner"].wrapMode = WrapMode.Loop;
		a["climb_edge"].wrapMode = WrapMode.Loop;
		//a["climb_edge_L"].wrapMode = WrapMode.Loop;
		//a["climb_edge_R"].wrapMode = WrapMode.Loop;
		a["climb_edgeShort"].wrapMode = WrapMode.Loop;
		a["climb_hang"].wrapMode = WrapMode.Loop;
		a["climb_hangL"].wrapMode = WrapMode.Loop;
		a["climb_hangR"].wrapMode = WrapMode.Loop;
		a["climb_hangShort"].wrapMode = WrapMode.Loop;
		a["climb_idle"].wrapMode = WrapMode.Loop;
		a["climb_ledge"].wrapMode = WrapMode.ClampForever;
		a["climb_ledge2"].wrapMode = WrapMode.ClampForever;
		a["climb_ledge3"].wrapMode = WrapMode.ClampForever;
		a["climb_lift"].wrapMode = WrapMode.ClampForever;
		a["climb_overhang"].wrapMode = WrapMode.ClampForever;
		a["climb_overhang2"].wrapMode = WrapMode.ClampForever;
		a["climb_overhang3"].wrapMode = WrapMode.ClampForever;
		a["climb_preOh"].wrapMode = WrapMode.Once;
		a["climb_prepare"].wrapMode = WrapMode.Once;
		a["climb_prepare2"].wrapMode = WrapMode.Once;
		a["climb_prepare3"].wrapMode = WrapMode.Once;
		a["climb_preShort"].wrapMode = WrapMode.Once;
		a["climb_preShort2"].wrapMode = WrapMode.Once;
		a["climb_preShort3"].wrapMode = WrapMode.Once;
		a["climb_short"].wrapMode = WrapMode.ClampForever;
		a["climb_short2"].wrapMode = WrapMode.ClampForever;
		a["climb_short3"].wrapMode = WrapMode.ClampForever;
		a["climb_wallclimb"].wrapMode = WrapMode.Loop;
		a["climb_walljump"].wrapMode = WrapMode.Loop;
		
		// Normal
		a["fall"].wrapMode = WrapMode.Loop;
		a["idle"].wrapMode = WrapMode.Loop;
		a["jogging"].wrapMode = WrapMode.Loop;
		a["jump"].wrapMode = WrapMode.ClampForever;
		a["jump2"].wrapMode = WrapMode.ClampForever;
		a["land"].wrapMode = WrapMode.ClampForever;
		a["walk"].wrapMode = WrapMode.Loop;
		
		// Roll /evade
		a["roll"].wrapMode = WrapMode.Once;
		//a["roll_air"].wrapMode = WrapMode.ClampForever;
		a["roll_back"].wrapMode = WrapMode.Once;
		a["roll_impact"].wrapMode = WrapMode.ClampForever;
		//a["roll_side"].wrapMode = WrapMode.Loop;
		a["roll_side_L"].wrapMode = WrapMode.ClampForever;
		a["roll_side_R"].wrapMode = WrapMode.ClampForever;
		
		a["run2"].wrapMode = WrapMode.Loop;
		a["shuffle"].wrapMode = WrapMode.Loop;
		
		// Sneak
		a["sneak"].wrapMode = WrapMode.Loop;
		a["sneak_idle"].wrapMode = WrapMode.Loop;
		a["sneak_turn_L"].wrapMode = WrapMode.Loop;
		a["sneak_turn_R"].wrapMode = WrapMode.Loop;
		
		// Stand up
		a["stand_up"].wrapMode = WrapMode.Once;
		a["stand_up_back"].wrapMode = WrapMode.Once;
		
		// Stop
		a["stop"].wrapMode = WrapMode.ClampForever;
		
		// Swim
		a["swim"].wrapMode = WrapMode.Loop;
		a["swim_backwards"].wrapMode = WrapMode.Loop;
		a["swim_forwards"].wrapMode = WrapMode.Loop;
		a["swim_left"].wrapMode = WrapMode.Loop;
		a["swim_right"].wrapMode = WrapMode.Loop;
		
		// Sword
		a["sword_draw"].wrapMode = WrapMode.ClampForever;
		a["sword_holster"].wrapMode = WrapMode.ClampForever;
		a["sword_idle"].wrapMode = WrapMode.Loop;
		a["sword_jogging"].wrapMode = WrapMode.Loop;
		a["sword_slash1"].wrapMode = WrapMode.ClampForever;
		a["sword_slash2"].wrapMode = WrapMode.ClampForever;
		a["sword_slash2.5"].wrapMode = WrapMode.ClampForever;
		a["sword_slash3"].wrapMode = WrapMode.ClampForever;
		a["sword_slash4"].wrapMode = WrapMode.ClampForever;
		a["sword_slash5"].wrapMode = WrapMode.ClampForever;
		a["sword_stop"].wrapMode = WrapMode.ClampForever;
		a["sword_turn"].wrapMode = WrapMode.Loop;
		a["sword_turn_L"].wrapMode = WrapMode.Loop;
		a["sword_turn_R"].wrapMode = WrapMode.Loop;
		a["sword_walk"].wrapMode = WrapMode.Loop;
		
		// Turn
		a["turn"].wrapMode = WrapMode.Loop;
		a["turn_L"].wrapMode = WrapMode.Loop;
		a["turn_R"].wrapMode = WrapMode.Loop;
		
		// Unarmed
		a["unarmed_kick1"].wrapMode = WrapMode.ClampForever;
		a["unarmed_kick2"].wrapMode = WrapMode.ClampForever;
		a["unarmed_punch1"].wrapMode = WrapMode.ClampForever;
		a["unarmed_punch2"].wrapMode = WrapMode.ClampForever;
		
		a["throw_shuriken"].wrapMode = WrapMode.ClampForever;
		
		//print ("Animation settings, done.");
	}
}
