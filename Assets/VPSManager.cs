using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;
using Google.XR.ARCoreExtensions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using TMPro;
using Esri.HPFramework;
using Esri.GameEngine.Geometry;
using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using UnityEngine;

public class VPSManager : MonoBehaviour
{
    public ARCoreExtensions Extension;
    public GameObject ARSessionOrigin;
    public GameObject DebugUI;
    public GameObject Anchor;
    public GameObject PipePrefab;
    public GameObject TreePrefab;
    public ArcGISLocationComponent ArcGISCameraLocation;
    public GameObject InfoCardTemplate;

    private bool ShowDebug = true;

    private ArcGISMapComponent arcGISMapComponent;
    private AREarthManager EarthManager;
    private bool MapInitialized = false;
    private bool GeoEnabled = false;
    private bool WaitingForResponse = false;
    private float timer = 0;
    private List<GameObject> PipeGOs = new();

    void Start()
    {
        EarthManager = ARSessionOrigin.GetComponent<AREarthManager>();
        arcGISMapComponent = FindObjectOfType<ArcGISMapComponent>();

        if (arcGISMapComponent == null)
        {
            Debug.LogError("Unable to find ArcGISMap");
            return;
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }


        // Tap with four fingers to load the ploylines (useful for when the map can't be initialized due to low accuracy)
        if (Input.touchCount == 4)
        {
            if (PipeGOs.Count == 0)
            {
                GetPolyLineFeatures();
            }
        }

        //if (Input.touchCount == 3)
        //{
        //    foreach (GameObject a in PipeGOs)
        //    {
        //        a.transform.position = a.transform.position + new Vector3(0, 1, 0);
        //    }
        //    Debug.Log(string.Format("new y = {0}", PipeGOs[0].transform.position.y));
        //}

        //if (Input.touchCount == 4)
        //{
        //    foreach (GameObject a in PipeGOs)
        //    {
        //        a.transform.position = a.transform.position - new Vector3(0, 1, 0);
        //    }
        //    Debug.Log(string.Format("new y = {0}", PipeGOs[0].transform.position.y));
        //}


        if (timer < .5)
        {
            timer += Time.deltaTime;
        }
        else
        {
            timer = 0;
            if (!GeoEnabled)
            {
                var featureSupport = EarthManager.IsGeospatialModeSupported(GeospatialMode.Enabled);
                switch (featureSupport)
                {
                    case FeatureSupported.Unknown:
                        Debug.Log("FeatureSupported UnKnown");
                        return;
                    case FeatureSupported.Unsupported:
                        Debug.Log("Geospatial API is not supported by this devices.");
                        return;
                    case FeatureSupported.Supported:
                        if (Extension.ARCoreExtensionsConfig.GeospatialMode == GeospatialMode.Disabled)
                        {
                            Debug.Log("Geospatial sample switched to GeospatialMode.Enabled.");
                            Extension.ARCoreExtensionsConfig.GeospatialMode =
                                GeospatialMode.Enabled;
                            GeoEnabled = true;
                            return;
                        }
                        else if (Extension.ARCoreExtensionsConfig.GeospatialMode == GeospatialMode.Enabled)
                        {
                            GeoEnabled = true;
                        }
                        break;
                }
            }

            if (EarthManager.EarthTrackingState == TrackingState.Tracking)
            {
                GeospatialPose cameraGeospatialPose = EarthManager.CameraGeospatialPose;

                double horAcc = cameraGeospatialPose.HorizontalAccuracy;
                double verAcc = cameraGeospatialPose.VerticalAccuracy;
                double heaAcc = cameraGeospatialPose.HeadingAccuracy;

                float horThres = 2;
                float heaThres = 4;
                float verThres = 2;

                if (!MapInitialized && horAcc < horThres)
                {
                    arcGISMapComponent.OriginPosition = new ArcGISPoint(
                        cameraGeospatialPose.Longitude,
                        cameraGeospatialPose.Latitude,
                        arcGISMapComponent.OriginPosition.Z,
                        new ArcGISSpatialReference(4326));
                    GetPolyLineFeatures();
                    MapInitialized = true;

                }

                ArcGISCameraLocation.Position = new ArcGISPoint(
                    cameraGeospatialPose.Longitude,
                    cameraGeospatialPose.Latitude,
                    ArcGISCameraLocation.Position.Z,
                    new ArcGISSpatialReference(4326));

                ArcGISCameraLocation.Rotation = new ArcGISRotation(0, 0, 0);

                if (heaAcc < heaThres)
                {
                    Vector3 originRotation = ARSessionOrigin.transform.rotation.eulerAngles;
                    originRotation.y = (float)cameraGeospatialPose.Heading - Camera.main.transform.localEulerAngles.y;
                    ARSessionOrigin.transform.rotation = Quaternion.Euler(originRotation);
                }

                if (ShowDebug)
                {
                    string MapInitString = string.Format("\n-------\nMap Origin Initialized? <color=\"{1}\">{0}</color>",
                        MapInitialized ? "Yes" : "No (low accuracy)",
                        MapInitialized ? "green" : "red");
                    string CameraString = string.Format("\n-------\nDevice Pose(world):\n{0}\n{1}",
                        transform.position.ToString(),
                        transform.rotation.eulerAngles.ToString());
                    string AROriginString = string.Format("\n-------\nAR Origin Pose(world):\n{0}\n{1}",
                        ARSessionOrigin.transform.position.ToString(), ARSessionOrigin.transform.rotation.eulerAngles.ToString());
                    DebugUI.GetComponentInChildren<TextMeshProUGUI>().text =
                        GetPoseString(cameraGeospatialPose, horThres, verThres, heaThres) + MapInitString + CameraString + AROriginString;
                }

            }
            else
            {
                if (ShowDebug)
                {
                    DebugUI.GetComponentInChildren<TextMeshProUGUI>().text = string.Format("tracking status: {0} {1:0.###}", EarthManager.EarthTrackingState.ToString(), timer);
                }
            }
        }
    }




    public async void GetPolyLineFeatures()
    {
        if (WaitingForResponse)
        {
            return;
        }

        WaitingForResponse = true;

        string results = await SewageSystemQuery(ArcGISCameraLocation.Position);

        if (results.Contains("error")) // Server returned an error
        {
            WaitingForResponse = false;
            return;
        }
        else
        {

            //var linePoints = new List<Vector3> ();
            var lineEnds = new Queue<Vector3>();
            float pipeScale = 2;



            Unity.Mathematics.double3 HPOrigin = arcGISMapComponent.GetComponent<HPRoot>().RootUniversePosition;


            // Parse the query response
            var response = JObject.Parse(results);
            var features = response.SelectToken("features");
            if (features is JArray featureArray)
            {
                for (int i = 0; i < featureArray.Count; i++) // Check if the response included any result  
                {
                    var geometry = featureArray[i].SelectToken("geometry");
                    var type = featureArray[i].SelectToken("attributes").SelectToken("TYPE");
                    var street = featureArray[i].SelectToken("attributes").SelectToken("STREETNAME");

                    var path = geometry.SelectToken("paths");

                    if (path is JArray pathArray)
                    {
                        for (int j = 0; j < pathArray.Count; j++)
                        {
                            if (pathArray[j] is JArray locations)
                            {
                                for (int k = 0; k < locations.Count; k++)
                                {
                                    if (locations[k] is JArray)
                                    {
                                        var lon = locations[k][0];
                                        var lat = locations[k][1];

                                        ArcGISPoint point = new ArcGISPoint((double)lon, (double)lat, new ArcGISSpatialReference(4326));
                                        Unity.Mathematics.double3 engineLocation = arcGISMapComponent.View.GeographicToWorld(point);

                                        lineEnds.Enqueue(new Vector3(
                                            (float)(engineLocation.x - HPOrigin.x),
                                            -10,
                                            (float)(engineLocation.z - HPOrigin.z)));
                                        if (lineEnds.Count == 2)
                                        {
                                            Vector3 start = lineEnds.Dequeue();
                                            Vector3 end = lineEnds.Peek();
                                            Vector3 mid = Vector3.Lerp(end, start, 0.5f) + 6f * Vector3.up;

                                            GameObject pipeGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

                                            Pipe pipeComp = pipeGO.AddComponent<Pipe>();
                                            pipeGO.AddComponent<BoxCollider>();
                                            pipeGO.transform.SetPositionAndRotation(mid, Quaternion.LookRotation(Vector3.down, start - end));
                                            pipeGO.transform.localScale = new Vector3(pipeScale, (end - start).magnitude / 2, pipeScale);
                                            PipeGOs.Add(pipeGO);
                                            if (pipeComp != null)
                                            {
                                                pipeComp.type = (string)type;
                                                pipeComp.street = (string)street;
                                                pipeComp.InfoCardTemplate = InfoCardTemplate;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    lineEnds.Clear();
                }
            }
        }
        WaitingForResponse = false;
    }

    public async void GetPointFeatures()
    {
        if (WaitingForResponse)
        {
            return;
        }

        WaitingForResponse = true;

        string results = await TreeLocationQuery(ArcGISCameraLocation.Position);

        if (results.Contains("error")) // Server returned an error
        {
            WaitingForResponse = false;
            return;
        }
        else
        {
            Unity.Mathematics.double3 HPOrigin = arcGISMapComponent.GetComponent<HPRoot>().RootUniversePosition;

            // Parse the query response
            var response = JObject.Parse(results);
            var features = response.SelectToken("features");
            if (features is JArray featureArray)
            {
                for (int i = 0; i < featureArray.Count; i++)
                {
                    var geometry = featureArray[i].SelectToken("geometry");
                    var botanical = featureArray[i].SelectToken("attributes").SelectToken("BOTANICAL");
                    var common = featureArray[i].SelectToken("attributes").SelectToken("COMMON");

                    var lon = geometry.SelectToken("x");
                    var lat = geometry.SelectToken("y");

                    ArcGISPoint point = new ArcGISPoint((double)lon, (double)lat, new ArcGISSpatialReference(4326));
                    Unity.Mathematics.double3 engineLocation = arcGISMapComponent.View.GeographicToWorld(point);

                    Vector3 location = new Vector3(
                        (float)(engineLocation.x - HPOrigin.x),
                        0,
                        (float)(engineLocation.z - HPOrigin.z)
                        );

                    GameObject treeGO = Instantiate(TreePrefab, location, Quaternion.identity, ARSessionOrigin.transform);
                    Tree treeComp = treeGO.GetComponent<Tree>();

                    treeGO.transform.localScale = new Vector3(3, 3, 3);
                    PipeGOs.Add(treeGO);
                    if (treeComp != null)
                    {
                        treeComp.botanical = (string)botanical;
                        treeComp.common = (string)common;
                        treeComp.InfoCardTemplate = InfoCardTemplate;
                    }
                }
            }
        }
        WaitingForResponse = false;
    }

    private string GetEnvelopeString(ArcGISPoint center, float xExtent, float yExtent)
    {
        ArcGISPoint projectedPoint = GeoUtils.ProjectToSpatialReference(center, new ArcGISSpatialReference(3857));

        string envelopeStr = string.Format("{{\"xmin\":{0},\"ymin\":{1},\"xmax\":{2},\"ymax\":{3}}}",
        projectedPoint.X - xExtent, projectedPoint.Y - yExtent, projectedPoint.X + xExtent, projectedPoint.Y + yExtent);

        return envelopeStr;
    }

    private async Task<string> SewageSystemQuery(ArcGISPoint center)
    {

        float xExtent, yExtent;
        xExtent = yExtent = 250;


        string geometryStr = GetEnvelopeString(center, xExtent, yExtent);

        string URL = "https://services.arcgis.com/FLM8UAw9y5MmuVTV/ArcGIS/rest/services/Sewer_System_View/FeatureServer/7/query/";
        IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("geometry", geometryStr),
            new KeyValuePair<string, string>("geometryType", "esriGeometryEnvelope"),
            new KeyValuePair<string, string>("spatialRel",  "esriSpatialRelContains"),
            new KeyValuePair<string, string>("resultType", "none"),
            new KeyValuePair<string, string>("distance", "0.0"),
            new KeyValuePair<string, string>("units", "esriSRUnit_Meter"),
            new KeyValuePair<string, string>("returnGeodetic", "false"),
            new KeyValuePair<string, string>("outFields", "TYPE,STREETNAME"),
            new KeyValuePair<string, string>("returnGeometry", "true"),
            new KeyValuePair<string, string>("featureEncoding", "esriDefault"),
            new KeyValuePair<string, string>("multipatchOption", "xyFootprint"),
            new KeyValuePair<string, string>("inSR", "3857"),
            new KeyValuePair<string, string>("outSR", "4326"),
            new KeyValuePair<string, string>("applyVCSProjection", "false"),
            new KeyValuePair<string, string>("returnIdsOnly", "false"),
            new KeyValuePair<string, string>("returnUniqueIdsOnly", "false"),
            new KeyValuePair<string, string>("returnCountOnly", "false"),
            new KeyValuePair<string, string>("returnExtentOnly", "false"),
            new KeyValuePair<string, string>("returnQueryGeometry", "false"),
            new KeyValuePair<string, string>("returnDistinctValues", "false"),
            new KeyValuePair<string, string>("cacheHint", "false"),
            new KeyValuePair<string, string>("returnZ", "false"),
            new KeyValuePair<string, string>("returnM", "false"),
            new KeyValuePair<string, string>("returnExceededLimitFeatures", "true"),
            new KeyValuePair<string, string>("sqlFormat", "none"),
            new KeyValuePair<string, string>("f", "pjson"),
        };

        HttpClient client = new HttpClient();
        HttpContent content = new FormUrlEncodedContent(payload);
        HttpResponseMessage response = await client.PostAsync(URL, content);

        response.EnsureSuccessStatusCode();
        string results = await response.Content.ReadAsStringAsync();
        return results;
    }

    private async Task<string> TreeLocationQuery(ArcGISPoint center)
    {

        float xExtent, yExtent;
        xExtent = yExtent = 250;

        string geometryStr = GetEnvelopeString(center, xExtent, yExtent);

        string URL = "https://services.arcgis.com/FLM8UAw9y5MmuVTV/ArcGIS/rest/services/Trees/FeatureServer/0/query/";
        IEnumerable<KeyValuePair<string, string>> payload = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("geometry", geometryStr),
            new KeyValuePair<string, string>("geometryType", "esriGeometryEnvelope"),
            new KeyValuePair<string, string>("spatialRel",  "esriSpatialRelContains"),
            new KeyValuePair<string, string>("resultType", "none"),
            new KeyValuePair<string, string>("distance", "0.0"),
            new KeyValuePair<string, string>("units", "esriSRUnit_Meter"),
            new KeyValuePair<string, string>("returnGeodetic", "false"),
            new KeyValuePair<string, string>("outFields", "BOTANICAL,COMMON"),
            new KeyValuePair<string, string>("returnGeometry", "true"),
            new KeyValuePair<string, string>("featureEncoding", "esriDefault"),
            new KeyValuePair<string, string>("multipatchOption", "xyFootprint"),
            new KeyValuePair<string, string>("inSR", "3857"),
            new KeyValuePair<string, string>("outSR", "4326"),
            new KeyValuePair<string, string>("applyVCSProjection", "false"),
            new KeyValuePair<string, string>("returnIdsOnly", "false"),
            new KeyValuePair<string, string>("returnUniqueIdsOnly", "false"),
            new KeyValuePair<string, string>("returnCountOnly", "false"),
            new KeyValuePair<string, string>("returnExtentOnly", "false"),
            new KeyValuePair<string, string>("returnQueryGeometry", "false"),
            new KeyValuePair<string, string>("returnDistinctValues", "false"),
            new KeyValuePair<string, string>("cacheHint", "false"),
            new KeyValuePair<string, string>("returnZ", "false"),
            new KeyValuePair<string, string>("returnM", "false"),
            new KeyValuePair<string, string>("returnExceededLimitFeatures", "true"),
            new KeyValuePair<string, string>("sqlFormat", "none"),
            new KeyValuePair<string, string>("f", "pjson"),
        };

        HttpClient client = new HttpClient();
        HttpContent content = new FormUrlEncodedContent(payload);
        HttpResponseMessage response = await client.PostAsync(URL, content);

        response.EnsureSuccessStatusCode();
        string results = await response.Content.ReadAsStringAsync();
        return results;
    }

    public void ToggleDebug()
    {
        ShowDebug = !ShowDebug;
        DebugUI.SetActive(ShowDebug);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private string GetPoseString(GeospatialPose cameraGeospatialPose, float horizontalAccuracyThreshold, float verticalAccuracyThreshold, float headingAccuracyThreshold)
    {
        double horizontalAcc = cameraGeospatialPose.HorizontalAccuracy;
        double verticalAcc = cameraGeospatialPose.VerticalAccuracy;
        double headingAcc = cameraGeospatialPose.HeadingAccuracy;

        string str = string.Format(
        "Lon: {0:0.###}\nLat: {1:0.###}\nAlt: {2:0.###} m\nHeading:{3:0.##}°\n-------\nAccuracy:\nHor: <color=\"{7}\">{4:0.###} m</color>\nVert: <color=\"{8}\">{5:0.###} m</color>\nHead: <color=\"{9}\">{6:0.###} °</color>",
        cameraGeospatialPose.Longitude,
        cameraGeospatialPose.Latitude,
        cameraGeospatialPose.Altitude,
        cameraGeospatialPose.Heading,
        horizontalAcc, verticalAcc, headingAcc,
        horizontalAcc < horizontalAccuracyThreshold ? "green" : "red",
        verticalAcc < verticalAccuracyThreshold ? "green" : "red",
        headingAcc < headingAccuracyThreshold ? "green" : "red");
        return str;
    }
}
