#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Outpost404.Player;
using Outpost404.CameraControl;

namespace Outpost404.EditorTools
{
    public static class Phase1SceneSetup
    {
        [MenuItem("Outpost 404/Setup Phase 1 Scene", false, 10)]
        public static void BuildPhase1Scene()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Ensure Materials folder exists and load/create stylized prototype materials
            Material groundMat = GetOrCreateMaterial("Assets/Materials/M_Ground.mat", new Color(0.18f, 0.22f, 0.25f));
            Material playerMat = GetOrCreateMaterial("Assets/Materials/M_Player.mat", new Color(0.92f, 0.50f, 0.12f));
            Material visorMat = GetOrCreateMaterial("Assets/Materials/M_Visor.mat", new Color(0.0f, 0.85f, 0.95f), new Color(0.0f, 0.85f, 0.95f) * 1.5f);
            Material obstacleMat = GetOrCreateMaterial("Assets/Materials/M_Obstacle.mat", new Color(0.32f, 0.36f, 0.42f));

            // 1. Directional Light (atmospheric sci-fi survey sunlight)
            Light sunLight = Object.FindFirstObjectByType<Light>();
            GameObject lightObj;
            if (sunLight == null)
            {
                lightObj = new GameObject("Directional Light");
                sunLight = lightObj.AddComponent<Light>();
            }
            else
            {
                lightObj = sunLight.gameObject;
            }
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(0.95f, 0.96f, 1.0f);
            sunLight.intensity = 1.15f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Ground Terrain / Floor
            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
            }
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // 3. Player Setup
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
            if (player == null)
            {
                player = new GameObject("Player");
            }

            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.transform.rotation = Quaternion.identity;

            CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = player.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = Vector3.zero;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null) rb = player.AddComponent<Rigidbody>();
            rb.mass = 75f;
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
#else
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
#endif
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null) controller = player.AddComponent<PlayerController>();

            // Visual Representation child
            Transform visualTransform = player.transform.Find("Visual");
            GameObject body;
            if (visualTransform == null)
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Visual";
                body.transform.SetParent(player.transform, false);
                body.transform.localPosition = Vector3.zero;

                Collider bodyCol = body.GetComponent<Collider>();
                if (bodyCol != null) Object.DestroyImmediate(bodyCol);
            }
            else
            {
                body = visualTransform.gameObject;
            }
            body.GetComponent<MeshRenderer>().sharedMaterial = playerMat;

            // Visor orientation indicator child
            Transform visorTransform = body.transform.Find("Visor_OrientationIndicator");
            GameObject visor;
            if (visorTransform == null)
            {
                visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "Visor_OrientationIndicator";
                visor.transform.SetParent(body.transform, false);
                visor.transform.localPosition = new Vector3(0f, 0.45f, 0.4f);
                visor.transform.localScale = new Vector3(0.5f, 0.2f, 0.3f);

                Collider visorCol = visor.GetComponent<Collider>();
                if (visorCol != null) Object.DestroyImmediate(visorCol);
            }
            else
            {
                visor = visorTransform.gameObject;
            }
            visor.GetComponent<MeshRenderer>().sharedMaterial = visorMat;

            // 4. Main Camera Setup
            Camera cam = Camera.main;
            GameObject camObj;
            if (cam == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            else
            {
                camObj = cam.gameObject;
            }

            TopDownCameraFollow camFollow = camObj.GetComponent<TopDownCameraFollow>();
            if (camFollow == null) camFollow = camObj.AddComponent<TopDownCameraFollow>();
            camFollow.Target = player.transform;
            camFollow.Offset = new Vector3(0f, 14f, -9f);
            camFollow.CameraPitch = 56f;
            camFollow.CameraYaw = 0f;
            camFollow.SmoothTime = 0.16f;
            camFollow.SnapToTarget();

            // 5. Environmental obstacles for collision verification
            CreateObstaclePillar("Obstacle_Pillar_East", new Vector3(6f, 1f, 3f), new Vector3(2f, 2f, 2f), obstacleMat);
            CreateObstaclePillar("Obstacle_Pillar_West", new Vector3(-5f, 1f, -4f), new Vector3(2f, 2f, 2f), obstacleMat);
            CreateObstaclePillar("Obstacle_Pillar_North", new Vector3(0f, 1f, 8f), new Vector3(3f, 2f, 1f), obstacleMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Outpost 404] Phase 1 scene generated and saved successfully!");
        }

        private static Material GetOrCreateMaterial(string relativePath, Color color, Color? emissionColor = null)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(relativePath);
            if (mat == null)
            {
                string dir = Path.GetDirectoryName(relativePath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                Shader shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                if (emissionColor.HasValue)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissionColor.Value);
                }
                AssetDatabase.CreateAsset(mat, relativePath);
            }
            return mat;
        }

        private static void CreateObstaclePillar(string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject pillar = GameObject.Find(name);
            if (pillar == null)
            {
                pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = name;
            }
            pillar.transform.position = pos;
            pillar.transform.localScale = scale;
            if (mat != null)
            {
                pillar.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
        }
    }
}
#endif
