using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;

/// <summary>
/// Handles elements properties
/// </summary>
public class MetadataManager : MonoBehaviour,IPointerDownHandler, IPointerClickHandler//, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string revitId;
    public string smithId;
    public string xmlNode;
    public GameObject gm;

    public void OnMouseDown()
    {

        //Debug.Log(getDataFromXml());
        //MoveWithWand.setText(getDataFromXml());
    }

    /// <summary>
    /// Return a formatted string containg the properties contained in the xml element
    /// </summary>
    /// <returns></returns>
    public string getDataFromXml()
    {

        if (xmlNode == null)
            return "no metadata";

        IEnumerable<XElement> properties = XElement.Parse(xmlNode)
            .Elements("KeyValueProperty");

        string output = "";//"id: " + revitId + "\n";
        bool toJoin = false;
        foreach (XElement keyTypeValueNode in properties)
        {
            string key = keyTypeValueNode.Attribute("name").Value;
            //                                                                         //
            string type = keyTypeValueNode.Attribute("type").Value;  //TODO casting
            //                                                                         //
            string value = keyTypeValueNode.Attribute("val").Value;
            output += key + ": " + value + (toJoin ? ", ||" : "\n");
            toJoin = !toJoin;
        }
        Debug.Log(output);
        return output;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float timeClicked;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.GetWandButton() == HoloTrackWand.Buttons.Primary)
        {
            if (MoveWithWand.mode == MoveWithWand.optionsEn.spawnOption)
            {

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.parent = gm.transform;
                sphere.transform.position = eventData.pointerCurrentRaycast.worldPosition;
                sphere.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                sphere.GetComponent<MeshRenderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            }else if (MoveWithWand.mode == MoveWithWand.optionsEn.movePiece)
            {
                MoveWithWand.selectedPart = gameObject;
            }

            else
            {
                Debug.Log("Someone clicked with primary button");
                //float distance = Vector3.Distance(eventData.worldPosition, transform.position);
                if (Time.time-timeClicked<0.25f)
                    gm.transform.parent.GetComponent<MoveWithWand>().setText(getDataFromXml());
            }

        }

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.GetWandButton() == HoloTrackWand.Buttons.Primary)
        {
            if (MoveWithWand.mode == MoveWithWand.optionsEn.moveModel){
                timeClicked = Time.time;
            }

        }
    }
}
