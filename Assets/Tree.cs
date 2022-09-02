using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tree : MonoBehaviour
{
    public Color DefaultColor = Color.green;
    public Color HighlightColor = Color.yellow;

    public string botanical = "BOTANICAL";
    public string common = "COMMON";
    public GameObject InfoCardTemplate;

    private GameObject InfoCard;
    private string InfoText = "";
    private Camera MainCamera;

    void Start()
    {
        GetComponent<MeshRenderer>().material.SetColor("_Color", DefaultColor);
        MainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount == 1 && !string.IsNullOrWhiteSpace(InfoText))
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = MainCamera.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                InfoCard = Instantiate(InfoCardTemplate, hit.point + 6f * Vector3.up, Quaternion.LookRotation(ray.direction));
                InfoCard.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                TextMeshProUGUI t = InfoCard.GetComponentInChildren<TextMeshProUGUI>();
                t.text = GetDescription();
            }
            InfoText = "";
        }
    }

    void OnMouseDown()
    {
        Debug.Log(GetDescription());
        GetComponent<MeshRenderer>().material.SetColor("_Color", HighlightColor);
        InfoText = GetDescription();
    }
    void OnMouseUp()
    {
        GetComponent<MeshRenderer>().material.SetColor("_Color", DefaultColor);
        Destroy(InfoCard);
        InfoCard = null;
    }

    public string GetDescription()
    {
        string descr = string.Format("{0}\n({1})", common, botanical); 
        return descr;
    }

}
