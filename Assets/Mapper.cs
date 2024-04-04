using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.IO;
using System;

public class Mapper : MonoBehaviour
{
    private ARCameraManager m_CameraManager;
    private int imageOrder = 0;

    void Start()
    {
        // Attempt to get the ARCameraManager component on the same GameObject
        m_CameraManager = GetComponent<ARCameraManager>();

        // Check if the ARCameraManager is null and log an error if it is
        if (m_CameraManager == null)
        {
            Debug.LogError("ARCameraManager not found on the same GameObject as Mapper script.");
            return; // Exit the Start method to prevent further errors
        }

        string mapDirectory = Path.Combine(Application.persistentDataPath, "map");

        // Check if the "map" directory exists before attempting to delete it
        if (Directory.Exists(mapDirectory))
        {
            try
            {
                // Delete the "images" and "Pose" directories along with their contents
                Directory.Delete(Path.Combine(mapDirectory, "images"), true);
                Directory.Delete(Path.Combine(mapDirectory, "Pose"), true);
                Debug.Log("Previous image and pose data deleted successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("Exception while deleting previous data: " + e);
            }
        }
        // Create the directories if they don't exist
        string imageDirectory = Path.Combine(Application.persistentDataPath, "map/images");
        string poseDirectory = Path.Combine(Application.persistentDataPath, "map/Pose");

        try
        {
            if (!Directory.Exists(imageDirectory))
                Directory.CreateDirectory(imageDirectory);

            if (!Directory.Exists(poseDirectory))
                Directory.CreateDirectory(poseDirectory);

        }
        catch (Exception e)
        {
            Debug.LogError("Exception while creating directories: " + e);
            return; // Exit the Start method to prevent further errors
        }
    }

    public void StartMapping()
    {
        InvokeRepeating("AsynchronousConversion", 1.0f, 0.2f);

        if (m_CameraManager.TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics))
        {
            SaveCameraIntrinsics("CameraIntrinsics_" + imageOrder.ToString() + ".txt",cameraIntrinsics);

        }
    }

    void AsynchronousConversion()
    {
        try
        {
            if (m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                StartCoroutine(ConvertImageAsync(image));
                image.Dispose();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Exception during image acquisition: " + e);
        }
    }

    IEnumerator ConvertImageAsync(XRCpuImage image)
    {
        var request = image.ConvertAsync(new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            outputFormat = TextureFormat.RGB24,
            transformation = XRCpuImage.Transformation.MirrorY
        });
        
        while (!request.status.IsDone())
            yield return null;

        if (request.status != XRCpuImage.AsyncConversionStatus.Ready)
        {
            Debug.LogErrorFormat("Request failed with status {0}", request.status);
            request.Dispose();
            yield break;
        }

        var rawData = request.GetData<byte>();
        var texture = new Texture2D(
            request.conversionParams.outputDimensions.x,
            request.conversionParams.outputDimensions.y,
            request.conversionParams.outputFormat,
            false);
        texture.LoadRawTextureData(rawData);
        texture.Apply();

        SaveCameraPose("CameraPose_" + imageOrder.ToString() + ".txt");
        //SaveCameraIntrinsics("CameraIntrinsics_" + imageOrder.ToString() + ".txt", m_CameraManager.GetActiveCamera());

        SaveTextureAsJPG(texture, "Image_" + imageOrder.ToString() + ".jpg");

        imageOrder++;
        request.Dispose();
    }

    void SaveTextureAsJPG(Texture2D texture, string fileName)
    {
        try
        {
            byte[] jpgData = texture.EncodeToJPG();
            string filePath = Path.Combine(Application.persistentDataPath, "map/images", fileName);
            File.WriteAllBytes(filePath, jpgData);
            Debug.Log("Texture saved as JPG: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Exception while saving texture as JPG: " + e);
        }
    }

    void SaveCameraPose(string fileName)
    {
        try
        {
            Matrix4x4 poseMat = Matrix4x4.TRS(Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one);
            float[] pose = new float[16];
            pose[0] = poseMat.m00; pose[1] = poseMat.m01; pose[2] = poseMat.m02; pose[3] = poseMat.m03;
            pose[4] = poseMat.m10; pose[5] = poseMat.m11; pose[6] = poseMat.m12; pose[7] = poseMat.m13;
            pose[8] = poseMat.m20; pose[9] = poseMat.m21; pose[10] = poseMat.m22; pose[11] = poseMat.m23;
            pose[12] = poseMat.m30; pose[13] = poseMat.m31; pose[14] = poseMat.m32; pose[15] = poseMat.m33;

            string filePath = Path.Combine(Application.persistentDataPath, "map/Pose", fileName);
            File.WriteAllLines(filePath, Array.ConvertAll(pose, element => element.ToString()));
            Debug.Log("Camera pose saved: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Exception while saving camera pose: " + e);
        }
    }

    void SaveCameraIntrinsics(string fileName, XRCameraIntrinsics intrinsics)
    {
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "map/Pose", fileName);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Fx " + intrinsics.focalLength.x.ToString());
                writer.WriteLine("Fy " + intrinsics.focalLength.y.ToString());
                writer.WriteLine("Px " + intrinsics.principalPoint.x.ToString());
                writer.WriteLine("Py " + intrinsics.principalPoint.y.ToString());
                //writer.WriteLine("Resolution: " + intrinsics.resolution.ToString());

            }

            Debug.Log("Camera intrinsics saved: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Exception while saving camera intrinsics: " + e);
        }
    }
}
