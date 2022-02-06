#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;


public class MeshResetWizard : ScriptableWizard
{
	public GameObject combineParent;


	[MenuItem("Optimization/Mesh Reset Wizard")]
	static void CreateWizard()
	{
		var wizard = DisplayWizard<MeshResetWizard>("Mesh Reset Wizard");

		// If there is selection, and the selection of one Scene object, auto-assign it
		var selectionObjects = Selection.objects;
		if (selectionObjects != null && selectionObjects.Length == 1) {
			var firstSelection = selectionObjects[0] as GameObject;
			if (firstSelection != null) {
				wizard.combineParent = firstSelection;
			}
		}
	}
	/// <summary>
	/// Releases a burst of multicolored particles centered on the GameObject
	/// </summary>
	/// <param name="intensity">How dense the particles are, from 0.0 to 1.0</param>
	public void OnWizardCreate()
	{

		// Verify there is existing object root, ptherwise bail.
		if (combineParent == null) {
			Debug.LogError("Mesh Reset Wizard: Parent of objects to combne not assigned. Operation cancelled.");
			return;
		}

		MeshFilter meshFilter = combineParent.GetComponent<MeshFilter>();

		// Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
		var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
		if (meshRenderer == null) {
			Debug.LogWarning("The Mesh Filter on object " + meshFilter.name + " has no Mesh Renderer component attached. Skipping.");
			return;
		}
		string meshName = "Assets/" + combineParent.transform.parent.name + "/CombinedMeshes_" + combineParent.name.Substring(combineParent.name.IndexOf(']') + 2)+".asset";
		Debug.Log(meshName);

		Mesh m = (Mesh)AssetDatabase.LoadAssetAtPath(meshName, typeof(Mesh));
		meshFilter.sharedMesh = m;
		combineParent.name = combineParent.name.Split('[')[0] +"[vtx="+meshFilter.sharedMesh.triangles.Length+"]"+combineParent.name.Split(']')[1];

	}
}
#endif