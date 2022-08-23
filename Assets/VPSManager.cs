using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using TMPro;
using UnityEngine;
public class VPSManager : MonoBehaviour
{
    private AREarthManager EarthManager;
    public ARCoreExtensions Extension;
    public GameObject SessionOrigin;
    public TextMeshProUGUI UIText;

    private double latitude;
    private double longitude;
    private double altitude;
    private float timer = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        //var cameraGeospatialPose = EarthManager.CameraGeospatialPose;
        //var earthTrackingState = EarthManager.EarthTrackingState;

        EarthManager = SessionOrigin.GetComponent<AREarthManager>();
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
        else
        {

            timer = 0;


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
                    if (Extension.ARCoreExtensionsConfig.GeospatialMode ==
                        GeospatialMode.Disabled)
                    {
                        Debug.Log("Geospatial sample switched to GeospatialMode.Enabled.");
                        Extension.ARCoreExtensionsConfig.GeospatialMode =
                            GeospatialMode.Enabled;
                        return;
                    }
                    break;
            }

            //TrackingState earthTrackingState = EarthManager.EarthTrackingState;

            if (EarthManager.EarthTrackingState == TrackingState.Tracking)
            {

                GeospatialPose cameraGeospatialPose = EarthManager.CameraGeospatialPose;
                string info = string.Format("{0}\n{1}\n{2:0.###} m\n{3:0.###} °N\n-------\nL Acc: {4:0.###} m\nE Acc: {5:0.###} m\nY Acc: {6:0.###}°",
                cameraGeospatialPose.Longitude,
                cameraGeospatialPose.Latitude,
                cameraGeospatialPose.Altitude,
                cameraGeospatialPose.Heading,
                cameraGeospatialPose.HorizontalAccuracy,
                cameraGeospatialPose.VerticalAccuracy,
                cameraGeospatialPose.HeadingAccuracy);

                string info2 = string.Format("\n-------\nCamera:\n{0}\n{1}", transform.position.ToString(), transform.rotation.eulerAngles.ToString());
                //Debug.Log(info);
                UIText.text = info + info2;
            }
            else
            {
                UIText.text = string.Format("tracking status: {0} {1}", EarthManager.EarthTrackingState.ToString(), timer);
                //Debug.Log(string.Format("$$$$$$$$$$$$$ {0}", earthTrackingState.ToString()));
            }
        }

        if (EarthManager.EarthTrackingState == TrackingState.Tracking)
        {
            var anchor =
                AnchorManager.AddAnchor(
                    latitude,
                    longitude,
                    altitude,
                    quaternion);
            var anchoredAsset = Instantiate(GeospatialAssetPrefab, anchor.transform);
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