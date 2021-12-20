using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains methods specific for your project
/// </summary>
public class Afterimport : MonoBehaviour
{

    static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (null == obj)
        {
            return;
        }

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (null == child)
            {
                continue;
            }
            Afterimport.SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    /// <summary>
    /// This method is called after the model is imported
    /// </summary>
    /// <param name="obj">The imported model</param>
    public static void afterImport(GameObject obj)
    {
        SetLayerRecursively(obj, 5);//5 is uiì
        Debug.Log("Import Complete");
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
