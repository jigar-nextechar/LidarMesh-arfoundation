using System;
using UnityEngine;
using Unity.XR.CoreUtils;
using GLTFast;
using GLTFast.Export;
using GLTFast.Logging;
using System.Collections.Generic;

namespace UnityEngine.XR.ARFoundation
{
    public class RuntimeExporterMono : MonoBehaviour
    {
        public GameObject meshManager;
        public XROrigin origin;
        //public string RelativeFolderPath = "/Ignore/RuntimeExport/";
        public string FileName = "TestExport.fbx";
        //public string TextureFolderName = "FBXTextures/";
        public bool UseGUI = true;

        public void StartMeshing()
        {
            if (origin.TrackablesParent != null)
            {
                foreach (Transform item in origin.TrackablesParent)
                {
                    Destroy(item.gameObject);
                }
            }
            meshManager.SetActive(true);
        }

        public void Export()
        {
            meshManager.SetActive(false);
            //ExportGameObject(rootObjectToExport, RelativeFolderPath, FileName, TextureFolderName);
            //SimpleExport();
            AdvancedExport();
        }

        async void SimpleExport()
        {
            // Example of gathering GameObjects to be exported (recursively)
            //var rootLevelNodes = GameObject.FindGameObjectsWithTag("ExportMe");
            List<GameObject> rootLevelNodes = new List<GameObject>();
            if (origin.TrackablesParent.childCount > 1)
            {
                for (int i = 1; i < origin.TrackablesParent.childCount; i++)
                {
                    rootLevelNodes.Add(origin.TrackablesParent.GetChild(i).gameObject);
                }
            }

            // GameObjectExport lets you create glTFs from GameObject hierarchies
            var export = new GameObjectExport();

            // Add a scene
            export.AddScene(rootLevelNodes.ToArray());

            string fullFileName = Application.persistentDataPath + "/" + FileName;
            // Async glTF export
            bool success = await export.SaveToFileAndDispose(fullFileName);

            if (!success)
            {
                Debug.LogError("Something went wrong exporting a glTF");
            }
            else
            {
                try
                {
                    new NativeShare()
                        .AddFile(fullFileName)
                        .SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + "\n Selected app: " + shareTarget))
                        .Share();
                }
                catch (Exception e)
                {
                    Debug.LogError("error in native share" + e);
                }
            }
        }

        async void AdvancedExport()
        {

            // CollectingLogger lets you programmatically go through
            // errors and warnings the export raised
            var logger = new CollectingLogger();

            // ExportSettings and GameObjectExportSettings allow you to configure the export
            // Check their respective source for details

            // ExportSettings provides generic export settings
            var exportSettings = new ExportSettings
            {
                Format = GltfFormat.Binary,
                FileConflictResolution = FileConflictResolution.Overwrite,
                // Export everything except cameras or animation
                ComponentMask = ~(ComponentType.Camera | ComponentType.Animation),
                // Boost light intensities
                LightIntensityFactor = 100f,
            };

            // GameObjectExportSettings provides settings specific to a GameObject/Component based hierarchy
            var gameObjectExportSettings = new GameObjectExportSettings
            {
                // Include inactive GameObjects in export
                OnlyActiveInHierarchy = false,
                // Also export disabled components
                DisabledComponents = true
            };

            // GameObjectExport lets you create glTFs from GameObject hierarchies
            var export = new GameObjectExport(exportSettings, gameObjectExportSettings, logger: logger);

            List<GameObject> rootLevelNodes = new();
            if (origin != null && origin.TrackablesParent != null)
            {
                rootLevelNodes= GetChildwithMesh(origin.TrackablesParent.gameObject);
                Debug.Log("added TrackablesParent to export");
            }
            else
            {
                Debug.Log("does not found parent");
            }

            // Add a scene
            export.AddScene(rootLevelNodes.ToArray(), "My new glTF scene");

            string fullFileName = Application.persistentDataPath + "/" + FileName;

            // Async glTF export
            var success = await export.SaveToFileAndDispose(fullFileName);

            if (!success)
            {
                Debug.LogError("Something went wrong exporting a glTF");
                // Log all exporter messages
                logger.LogAll();
            }
        }

        private List<GameObject> GetChildwithMesh(GameObject obj)
        {
            if (null == obj)
                return null;
            List<GameObject> listOfChildren = new List<GameObject>();
            int childWithMesh = 0;
            foreach (Transform child in obj.transform)
            {
                if (null == child)
                    continue;
                //child.gameobject contains the current child you can do whatever you want like add it to an array
                if (child.GetComponent<MeshFilter>() != null)
                {
                    childWithMesh++;
                    var dataArray = Mesh.AcquireReadOnlyMeshData(child.GetComponent<MeshFilter>().mesh);
                    if (dataArray.Length > 0 && dataArray[0].vertexCount > 0)
                        listOfChildren.Add(child.gameObject);
                }
            }
            Debug.Log($"total childs {obj.transform.childCount} child with mesh {childWithMesh} final count {listOfChildren.Count}");
            return listOfChildren;
        }
    }
}

///// <summary>
///// Simple mono component that shows how to export an object at runtime.
///// Attach this to any game object and assign RootObjectToExport
///// </summary>
//public class RuntimeExporterMono : MonoBehaviour
//{
//	//public GameObject rootObjectToExport;
//	//public string RelativeFolderPath = "/Ignore/RuntimeExport/";
//	//public string FileName = "TestFBXExport.fbx";
//	//public string TextureFolderName = "FBXTextures/";
//	//public bool UseGUI = true;

//	public void Export()
//	{
//		ExportGameObject(rootObjectToExport, RelativeFolderPath, FileName, TextureFolderName);
//	}

//	//void OnGUI()
//	//{
//	//	if(UseGUI == false)
//	//		return;

//	//	if(rootObjectToExport != null && GUI.Button(new Rect(10, 10, 150, 50), "Export FBX"))
//	//	{
//	//		this.ExportGameObject();
//	//	}
//	//}

//	//public bool ExportGameObject()
//	//{
//	//	return ExportGameObject(rootObjectToExport, RelativeFolderPath, FileName, TextureFolderName);
//	//}

//	/// <summary>
//	/// Will export to whatever folder path is provided within the Assets folder
//	/// </summary>
//	/// <param name="rootGameObject"></param>
//	/// <param name="folderPath"></param>
//	/// <param name="fileName"></param>
//	/// <param name="textureFolderName"></param>
//	/// <returns></returns>
//	public static async bool ExportGameObject(GameObject rootGameObject, string folderPath, string fileName, string textureFolderName)
//	{
//		if (rootGameObject == null)
//		{
//			Debug.Log("Root game object is null, please assign it");
//			return false;
//		}

//		// forces use of forward slash for directory names
//		folderPath = folderPath.Replace('\\', '/');
//		textureFolderName = textureFolderName.Replace('\\', '/');

//		folderPath = Application.persistentDataPath + folderPath;

//		if (System.IO.Directory.Exists(folderPath) == false)
//		{
//			System.IO.Directory.CreateDirectory(folderPath);
//		}

//		if (System.IO.Path.GetExtension(fileName).ToLower() != ".fbx")
//		{
//			Debug.LogError(fileName + " does not end in .fbx, please save a file with the extension .fbx");
//			return false;
//		}

//		if (folderPath[folderPath.Length - 1] != '/')
//			folderPath += "/";

//		if (System.IO.File.Exists(folderPath + fileName))
//			System.IO.File.Delete(folderPath + fileName);

//        bool exported = await FBXExporter.ExportGameObjAtRuntime(rootGameObject, folderPath, fileName, textureFolderName);

//#if UNITY_EDITOR
//		UnityEditor.AssetDatabase.Refresh();
//#endif
//		if (exported)
//		{
//			try
//			{
//				new NativeShare()
//					.AddFile(folderPath, fileName)
//					.SetCallback((result, shareTarget) => Debug.Log("Share result: " + result + "\n Selected app: " + shareTarget))
//					.Share();
//			}
//			catch (Exception e)
//			{
//				Debug.LogError("error in native share" + e);
//			}
//		}
//		else
//		{
//			Debug.Log("Export FBX Failed");
//		}

//		return exported;
//	}
//}