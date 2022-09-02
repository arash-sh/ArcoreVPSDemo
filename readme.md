
# Demo AR project using ARCore Visual Positioning System (VPS)

## Setting up a Unity Project using 

The following actions are needed for setting up the Unity project for Android development using ARCore VPS. The list is specific to development for Samsung Galaxy S22 as of September 2022, other device may require tweaking in the settings (e.g., rendering settings) or may not support all the features. The list may not be comprehensive.

- Switch the platform to Android in the Build Setting.

- Add ARCore Extension using package manager (currently through git link)

- The above step should automatically add the compatible versions of ARFoundation and ARCore XR to the project as well. If that’s not the case, add them manually.

- In the scene, add the following game objects: AR Session, AR Session Origin (should have AR Camera as child), and ARCore Extensions.

- If occlusion culling is being used, add AR Occlusion Manager component to the AR Camera game object (requires AR Camera Background component as well). Setup the component as desired

- Create an AR Extensions Config asset and enable geospatial mode. In the Inspector window, setup AR Extensions game object by specifying the relevant game objects/assets.

- In the Project Settings under XR Plug-in Management:

	- Chose ARCore

	- Under ARCore Extensions, add an API key (Sign up for Google Cloud Platform and enable ARCore API.

	- Under ARCore Extensions, enable Geospatial

	- Add an Earth Manager component to AR Session game object

- In the Project Settings under Player select Android tab. Under Other Settings:

	- Remove Vulkan from the Graphics APIs list (keep OpenGL)

	- Require ES3.2 (necessary?)

	- Change color space to linear (necessary?)

	- Disable ARMv7 and enable AEM64

	- Set Scripting Backend to IL2CPP

	- Set Internet Access to Require

## Setting up the ArcGIS Map

Note: The ArcGIS Maps SDK OpenSSL binaries. Refer to the [deployment documentation](https://developers.arcgis.com/unity/deployment/) of the SDK.

Add an ArcGISMap game object to the scene and configure it as needed. In order for he map to be loaded, add an ArcGISCamera as a child of the map game object. Remove the camera component from it and configure the ArcGIS Camera with appropriate FOV and Viewport.

## How the Current Implementation Works

After launching, the app waits for the tracking to start (requires internet). Once that happens, the location of the ArcGISCamera is set to the detected geolocation of the device (represented by ARCamera). Current implementation assumes that this happens quickly enough that the device is still at (roughly) the same location as when the app started. This should be a marginal effect but if there is an issue, the offset of the device relative to AR Session Origin should be applied to the ArcGISCamera as well.

Initially, the origin of the map is set to a fixed location as defined in the inspector, but once the positioning has reached a good horizontal accuracy (thresholds are defined in the code) the map’s latitude and longitude are set to the respective location. To reach better positioning accuracy, point the camera to buildings, streets, and other distinct objects that can be compared to Google Street View data.

Regarding the vertical positioning, the altitude provided by Android devices differs from the elevation that ArcGIS Maps uses. Currently, the map is placed at a hard-coded elevation. For this, set the altitude of the map’s origin to a value that is a couple of meters higher than the elevation of earth’s surface at the location that you intend to use the app at (use for example Google Maps to determine this elevation). A better solution should be implemented for future (e.g., changing height through UI, raycast for determining altitude).

When the app starts, the rotation of the AR Session Origin is set to (0,0,0). This means that the north direction of the map, will be aligned with the forward direction of the device when the app was launched rather than the actual north. To fix this, whenever the heading accuracy is acceptable, the rotation of the AR Session Origin’s yaw is updated  `detected heading direction – ARCamera’s local yaw`. This way, the (0,0,0) rotation of the device corresponds to north. If the AR Session Origin is parent to game objects other than the AR Camera, those objects need to be relocated/reoriented. Alternatively, the hierarchy should be extended so that AR Camera is grandchild of AR Session Origin and the intermediary game object is updated instead of the AR Session Origin.

Note: At some point, the tracking stopped working with no apparent reason. The workaround that I found was to toggle the current value of the Geospatial mode under the ARCore Extensions Config asset each time a new build is made (the new value is the opposite of what currently is deployed to the device). If the value is Disabled, it will be enabled in the code, so the positioning will still work.

## Current Scenes

There are two scenes currently developed that can be loaded through the UI.  
The CampusBuildings scene overlays the Esri campus buildings with a wireframe model loaded as a scene layer. 
The SewageSystem scene visualizes the underground sewage pipes in Redlands by querying a feature layer. Toughing the pipes shows their type and the respective street name. 

The VPSManager script also includes an implementation for visualizing trees in Redlands, but this functionality is not yet fully working.   