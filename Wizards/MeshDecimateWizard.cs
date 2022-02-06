#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityMeshSimplifier;

public class MeshDecimateWizard : ScriptableWizard
{
	public GameObject combineParent;
	public float quality_1_for_lossless= 1f;


	[MenuItem("Optimization/Mesh Decimate Wizard")]
	static void CreateWizard()
	{
		var wizard = DisplayWizard<MeshDecimateWizard>("Mesh Decimate Wizard");

		// If there is selection, and the selection of one Scene object, auto-assign it
		var selectionObjects = Selection.objects;
		if (selectionObjects != null && selectionObjects.Length == 1) {
			var firstSelection = selectionObjects[0] as GameObject;
			if (firstSelection != null) {
				wizard.combineParent = firstSelection;
			}
		}
	}

	public void OnWizardCreate()
	{

		// Verify there is existing object root, ptherwise bail.
		if (combineParent == null) {
			Debug.LogError("Mesh Decimate Wizard: Parent of objects to combne not assigned. Operation cancelled.");
			return;
		}

		MeshFilter meshFilter = combineParent.GetComponent<MeshFilter>();

		// Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
		var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
		if (meshRenderer == null) {
			Debug.LogWarning("The Mesh Filter on object " + meshFilter.name + " has no Mesh Renderer component attached. Skipping.");
			return;
		}
			
		MeshSimplifier meshSimplifier = new MeshSimplifier();
		meshSimplifier.Initialize(meshFilter.sharedMesh);
		if (quality_1_for_lossless == 1)
			meshSimplifier.SimplifyMeshLossless();
		else meshSimplifier.SimplifyMesh(quality_1_for_lossless);

		meshFilter.sharedMesh = meshSimplifier.ToMesh();
		combineParent.name = combineParent.name.Split('[')[0] +"[vtx="+meshFilter.sharedMesh.triangles.Length+"]"+combineParent.name.Split(']')[1];

	}
}
#endif