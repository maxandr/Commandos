using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]

public class TrailFX : MonoBehaviour 
{
	Transform startPoint;
	public Transform endPoint = null;

	public float time = 0.4f;
	public float minDistance = 0.1f;

	public Color startColor = Color.white;
	public Color endColor = new Color (1, 1, 1, 0);
	public string atkState = "None";
	
	public class TrailSection
	{
		public Vector3 startP;
		public Vector3 endP;
		public float time;
	}
	
	MeshFilter mf;
	HeroAnim hMotion;
	List <TrailSection> sections = new List <TrailSection>();
	bool isClean = false;
	
	void Start ()
	{
		startPoint = transform;
		hMotion = transform.root.GetComponent <HeroAnim> () as HeroAnim;
		mf = GetComponent <MeshFilter> () as MeshFilter;
		hMotion.doMeleeDel += DoAtkState;
	}
	
	void DoAtkState (string s)
	{
		atkState = s;
	}
	
	void LateUpdate ()
	{
		
		if (atkState != "None" && hMotion.mainState == HeroAnim.MainState.Combat) 
		{
			Vector3 startPosition = startPoint.position;
			Vector3 endPosition = endPoint.position;
			float now = Time.time;
			
			// Remove old Sections
			while (sections.Count > 0 && now > sections[sections.Count - 1].time + time) 
			{
				sections.RemoveAt (sections.Count - 1);
				isClean = false;
			}
			
			// Add a new trail section
			if (sections.Count == 0 || (sections [0].startP - startPosition).sqrMagnitude > minDistance * minDistance)
			{
				TrailSection section = new TrailSection ();
				section.startP = startPosition;
				section.endP = endPosition;
				
				section.time = now;
				sections.Insert (0, section);
			}
			
			// Rebuild the mesh
			Mesh mesh = GetComponent <MeshFilter> ().mesh as Mesh;
			mesh.Clear ();
			
			if (sections.Count < 2)
				return;
			
			Vector3[] vertices = new Vector3[sections.Count * 2];
			Color[] colors = new Color[sections.Count * 2];
			Vector2[] uv = new Vector2[sections.Count * 2];
			
			//TrailSection previousSection = sections [0];
			TrailSection currentSection = sections [0];
			
			// Use matrix instead of transform.TransformPoint for performance reasons
			Matrix4x4 localSpaceTransform = transform.worldToLocalMatrix;
			
			// Generate vertex, uv and colors
			for (int i = 0; i < sections.Count; i++)
			{
				//previousSection = currentSection;
				currentSection = sections [i];
				// Calculate u for texture uv and color interpolation
				float u = 0.0f;
				if (i != 0)
					u = Mathf.Clamp01 ((Time.time - currentSection.time) / time);
				
				// Calculate upwards direction
				//Vector3 endPos = currentSection.endP;
				
				// Generate vertices
				vertices [i * 2 + 0] = localSpaceTransform.MultiplyPoint (currentSection.startP);
				vertices [i * 2 + 1] = localSpaceTransform.MultiplyPoint (currentSection.endP);
				
				uv [i * 2 + 0] = new Vector2 (u, 0);
				uv [i * 2 + 1] = new Vector2 (u, 1);
				
				// fade colors out over time
				Color interpolatedColor = Color.Lerp (startColor, endColor, u);
				colors [i * 2 + 0] = interpolatedColor;
				colors [i * 2 + 1] = interpolatedColor;
			}
			
			// Generate triangles indices
			int[] triangles = new int[(sections.Count - 1) * 2 * 3];
			for (int i = 0; i < triangles.Length / 6; i++) 
			{
				triangles [i * 6 + 0] = i * 2;
				triangles [i * 6 + 1] = i * 2 + 1;
				triangles [i * 6 + 2] = i * 2 + 2;
				
				triangles [i * 6 + 3] = i * 2 + 2;
				triangles [i * 6 + 4] = i * 2 + 1;
				triangles [i * 6 + 5] = i * 2 + 3;
			}
			
			// Assign to mesh
			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.uv = uv;
			mesh.triangles = triangles;
		}
		else
		{
			if (!isClean) 
			{
				sections.Clear ();
				if (mf.mesh) 
				{
					mf.mesh.Clear ();
				}
				isClean = true;
			}
		}
	}
}
