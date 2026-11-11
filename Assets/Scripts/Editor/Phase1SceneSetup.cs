#if UNITY_EDITOR
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

            // 1. Directional Light
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
            sunLight.intensity = 1.1f;
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
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null) controller = player.AddComponent<PlayerController>();

            // Visual Representation child
            Transform visualTransform = player.transform.Find("Visual");
            if (visualTransform == null)
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Visual";
                body.transform.SetParent(player.transform, false);
                body.transform.localPosition = Vector3.zero;

                // Remove collider on the visual child so main collider handles physics
                Collider bodyCol = body.GetComponent<Collider>();
                if (bodyCol != null) Object.DestroyImmediate(bodyCol);

                // Add visor to clearly show front facing orientation
                GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visor.name = "Visor_OrientationIndicator";
                visor.transform.SetParent(body.transform, false);
                visor.transform.localPosition = new Vector3(0f, 0.45f, 0.4f);
                visor.transform.localScale = new Vector3(0.5f, 0.2f, 0.3f);
                Collider visorCol = visor.GetComponent<Collider>();
                if (visorCol != null) Object.DestroyImmediate(visorCol);
            }

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
            camFollow.SnapToTarget();

            // 5. Environmental obstacles for collision verification
            CreateObstaclePillar("Obstacle_Pillar_East", new Vector3(6f, 1f, 3f), new Vector3(2f, 2f, 2f));
            CreateObstaclePillar("Obstacle_Pillar_West", new Vector3(-5f, 1f, -4f), new Vector3(2f, 2f, 2f));
            CreateObstaclePillar("Obstacle_Pillar_North", new Vector3(0f, 1f, 8f), new Vector3(3f, 2f, 1f));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Outpost 404] Phase 1 scene generated and saved successfully!");
        }

        private static void CreateObstaclePillar(string name, Vector3 pos, Vector3 scale)
        {
            GameObject pillar = GameObject.Find(name);
            if (pillar == null)
            {
                pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = name;
            }
            pillar.transform.position = pos;
            pillar.transform.localScale = scale;
        }
    }
}
#endif
