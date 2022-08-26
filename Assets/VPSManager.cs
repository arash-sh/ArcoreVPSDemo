using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using TMPro;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine.Geometry;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using UnityEngine;
public class VPSManager : MonoBehaviour
{
    public ARCoreExtensions Extension;
    //public GameObject ARSession;
    public GameObject ARSessionOrigin;
    public TextMeshProUGUI UIText;
    public GameObject Anchor;
    public ArcGISLocationComponent ArcGISCameraLocation;
    public ArcGISMapComponent ArcGISMap;

    private AREarthManager EarthManager;
    private ARAnchorManager AnchorManager;
    private bool MapInitialized = false;
    private bool GeoEnabled = false;
    private double latitude = 34.056955;
    private double longitude = -117.196068;
    private double altitude = 400;
    private float timer = 0;

    // Start is called before the first frame update
    void Start()
    {
        //var cameraGeospatialPose = EarthManager.CameraGeospatialPose;
        //var earthTrackingState = EarthManager.EarthTrackingState;

        EarthManager = ARSessionOrigin.GetComponent<AREarthManager>();
        AnchorManager = ARSessionOrigin.GetComponent<ARAnchorManager>();


    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //Debug.Log("Quittttttttttt");
            Application.Quit();
        }

        //Debug.Log(Extension.ARCoreExtensionsConfig.GeospatialMode.ToString());
        //GeospatialManager.Instance.EarthManager

        if (timer < .5)
        {

            timer += Time.deltaTime;
        }

        if (Input.touchCount == 2)
        {

            if (EarthManager.EarthTrackingState == TrackingState.Tracking)
            {
                GeospatialPose cameraGeospatialPose = EarthManager.CameraGeospatialPose;
                Vector3 originRotation = ARSessionOrigin.transform.rotation.eulerAngles;
                originRotation.y = (float)cameraGeospatialPose.Heading - Camera.main.transform.localEulerAngles.y;
                ARSessionOrigin.transform.rotation = Quaternion.Euler(originRotation);
                //Camera.main.transform.localRotation = Quaternion.Euler(45,0,0);
                //ARSessionOrigin.GetComponent<ARSessionOrigin>().MakeContentAppearAt(Camera.main.transform, Camera.main.transform.position, Quaternion.Euler(45, 0, 0));
            }
        }
        if (Input.touchCount == 3)
        {
            Camera.main.transform.localRotation = Quaternion.Euler(Camera.main.transform.localRotation.eulerAngles + new Vector3(0, 2, 0));
        }
        if (Input.touchCount == 4)
        {
            ARSessionOrigin.transform.rotation = Quaternion.Euler(ARSessionOrigin.transform.rotation.eulerAngles + new Vector3(0, 2, 0));
        }
        

        else
        {
            timer = 0;

            //if (!GeoEnabled)
            //{
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
                            //Debug.Log("1111111111111111#");
                            return;
                        }
                        else if (Extension.ARCoreExtensionsConfig.GeospatialMode == GeospatialMode.Enabled)
                        {
                            GeoEnabled = true;
                            //Debug.Log("222222222222222#");
                        }
                        break;
                }
            //}
            //TrackingState earthTrackingState = EarthManager.EarthTrackingState;

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
                    ArcGISMap.OriginPosition = new ArcGISPoint(
                        cameraGeospatialPose.Longitude,
                        cameraGeospatialPose.Latitude,
                        ArcGISMap.OriginPosition.Z,
                        new ArcGISSpatialReference(4326));
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


                string info = string.Format(
                    "{0}\n{1}\n{2:0.###} m\n{3:0.###} °N\n-------\nL Acc: <color=\"{7}\">{4:0.###}</color> m\nE Acc: <color=\"{8}\">{5:0.###}</color> m\nY Acc: <color=\"{9}\">{6:0.###}</color> °",
                cameraGeospatialPose.Longitude,
                cameraGeospatialPose.Latitude,
                cameraGeospatialPose.Altitude,
                cameraGeospatialPose.Heading,
                horAcc,verAcc,heaAcc,
                horAcc<horThres?"green":"red", 
                verAcc<verThres?"green":"red", 
                heaAcc<heaThres?"green":"red");;

                string info2 = string.Format("\n-------\nCamera (world):\n{0}\n{1}",
                    transform.position.ToString(),
                    transform.rotation.eulerAngles.ToString()
                    ) ;

                ////Debug.Log(info);
                //if (Anchor == null)
                //{
                //    var geoAnchor =
                //        AnchorManager.AddAnchor(
                //            latitude,
                //            longitude,
                //            cameraGeospatialPose.Altitude,
                //            Quaternion.AngleAxis(180f, Vector3.up));
                //    Anchor = Instantiate(AnchorManager.anchorPrefab, geoAnchor.transform);

                //}

                string info3 = string.Format("\n-------\nAnchor:<color=\"{1}\">\n{0}</color>", 
                    Anchor != null ? Anchor.transform.position.ToString() : "",
                    MapInitialized ? "green": "red");
                string info4 = string.Format("\n-------\nAR Origin (world):\n{0}\n{1}", 
                    ARSessionOrigin.transform.position.ToString(), ARSessionOrigin.transform.rotation.eulerAngles.ToString());



                UIText.text = info + info2 + info3 + info4;
            }
            else
            {
                UIText.text = string.Format("tracking status: {0} {1:0.###}", EarthManager.EarthTrackingState.ToString(), timer);
                Debug.Log(string.Format("$$$$$$$$$$$$$ {0}", EarthManager.EarthTrackingState.ToString()));
            }
        }


    }
}



//public GameObject GeospatialAssetPrefab;
//public ARAnchorManager AnchorManager;
//public Quaternion quaternion;

//public void SetCube()
//{
//    altitude = cameraGeospatialPose.Altitude;

//    if (earthTrackingState == TrackingState.Tracking)
//    {
//        var anchor =
//            AnchorManager.AddAnchor(
//                latitude,
//                longitude,
//                altitude,
//                quaternion);
//        var anchoredAsset = Instantiate(GeospatialAssetPrefab, anchor.transform);
//    }
//}