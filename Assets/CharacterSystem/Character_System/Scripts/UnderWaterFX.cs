using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BlurEffect))]
[RequireComponent(typeof(GlowEffect))]

// Needs to be placed on a Camera
public class UnderWaterFX : MonoBehaviour 
{
	
	BlurEffect bE;
	GlowEffect gE;
	SphereCollider sC;
	
	
	void Start ()
	{
		bE = transform.GetComponent <BlurEffect> () as BlurEffect;
		gE = transform.GetComponent <GlowEffect> () as GlowEffect;
		bE.enabled = false;
		gE.enabled = false;
		gE.glowIntensity = 0f;
		sC = transform.GetComponent <SphereCollider> () as SphereCollider;
		sC.radius = 0.2f;
		sC.isTrigger = true;
		rigidbody.isKinematic = true;
		gameObject.layer = 4; // Water layer
	}
	
	
	void OnTriggerEnter (Collider other)
	{
		if (other.gameObject.layer == 4) // Other is in Water layer
		{
			StartCoroutine( On() );
		}
	}
	void OnTriggerExit (Collider other)
	{
		if (other.gameObject.layer == 4) // Water layer
		{
			StartCoroutine( Off() );
		}
	}
	
	
	
	IEnumerator On ()
	{
		yield return null;
		if (!bE.enabled)
		{
			bE.enabled = true;
			if (!gE.enabled)
				gE.enabled = true;
		}
	}
	IEnumerator Off ()
	{
		yield return null;
		if (bE.enabled)
		{
			bE.enabled = false;
			if (gE.enabled)
				gE.enabled = false;
		}
	}
}
