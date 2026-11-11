using NUnit.Framework;
using UnityEngine;
using Outpost404.Player;
using Outpost404.CameraControl;

namespace Outpost404.Tests
{
    public class PlayerControllerTests
    {
        private GameObject playerObj;
        private Rigidbody rb;
        private CapsuleCollider col;
        private PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            playerObj = new GameObject("TestPlayer");
            playerObj.tag = "Player";
            col = playerObj.AddComponent<CapsuleCollider>();
            rb = playerObj.AddComponent<Rigidbody>();
            controller = playerObj.AddComponent<PlayerController>();
            controller.ConfigureRigidbody();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObj != null)
            {
                Object.DestroyImmediate(playerObj);
            }
        }

        [Test]
        public void Rigidbody_ConfiguredCorrectlyForTopDownMovement()
        {
            Assert.AreEqual(RigidbodyInterpolation.Interpolate, rb.interpolation, "Rigidbody should be interpolated for smooth motion.");
            Assert.AreEqual(CollisionDetectionMode.Continuous, rb.collisionDetectionMode, "Collision detection should be continuous.");
            Assert.IsTrue((rb.constraints & RigidbodyConstraints.FreezeRotationX) != 0, "Rotation X should be frozen.");
            Assert.IsTrue((rb.constraints & RigidbodyConstraints.FreezeRotationZ) != 0, "Rotation Z should be frozen.");
            Assert.IsTrue(rb.useGravity, "Gravity should remain active.");
        }

        [Test]
        public void InputOverride_NormalizesVectorAndCalculatesSpeed()
        {
            controller.MoveSpeed = 6f;
            controller.SprintMultiplier = 1.5f;

            // Diagonal input (1, 1) has magnitude sqrt(2) ~ 1.414, should be clamped to 1
            controller.SetInputOverride(new Vector2(1f, 1f), false);

            Assert.IsTrue(controller.IsMoving, "Player should be moving.");
            Assert.IsFalse(controller.IsSprinting, "Sprint should be disabled.");
            Assert.LessOrEqual(controller.CurrentMoveDirection.magnitude, 1.01f, "Direction magnitude should be normalized.");

            // Sprinting enabled
            controller.SetInputOverride(new Vector2(0f, 1f), true);
            Assert.IsTrue(controller.IsSprinting, "Sprint should be enabled.");
        }

        [Test]
        public void InputOverride_ExtremeInputs_ClampedGracefully()
        {
            controller.SetInputOverride(new Vector2(100f, 100f), false);
            Assert.LessOrEqual(controller.CurrentMoveDirection.magnitude, 1.001f, "Extreme inputs must be normalized.");
        }

        [Test]
        public void ZeroInput_ResultsInIdleState()
        {
            controller.SetInputOverride(Vector2.zero, false);
            Assert.IsFalse(controller.IsMoving, "Zero input should result in IsMoving == false.");
            Assert.AreEqual(Vector3.zero, controller.CurrentMoveDirection, "Move direction should be zero when idle.");
        }

        [Test]
        public void ZeroMoveSpeed_DoesNotThrowOrCauseNaN()
        {
            controller.MoveSpeed = 0f;
            controller.SetInputOverride(new Vector2(1f, 0f), false);
            Assert.IsFalse(float.IsNaN(controller.CurrentMoveDirection.x));
            Assert.IsFalse(float.IsNaN(controller.CurrentMoveDirection.z));
        }
    }

    public class TopDownCameraFollowTests
    {
        private GameObject camObj;
        private GameObject targetObj;
        private TopDownCameraFollow camFollow;

        [SetUp]
        public void SetUp()
        {
            targetObj = new GameObject("TestTarget");
            targetObj.transform.position = new Vector3(5f, 0f, 10f);

            camObj = new GameObject("TestCamera");
            camFollow = camObj.AddComponent<TopDownCameraFollow>();
            camFollow.Target = targetObj.transform;
            camFollow.Offset = new Vector3(0f, 14f, -9f);
            camFollow.CameraPitch = 56f;
            camFollow.CameraYaw = 0f;
            camFollow.TargetOffset = new Vector3(0f, 1f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (camObj != null) Object.DestroyImmediate(camObj);
            if (targetObj != null) Object.DestroyImmediate(targetObj);
        }

        [Test]
        public void ComputeDestination_WithoutYaw_FollowsDirectOffset()
        {
            Vector3 expected = (targetObj.transform.position + new Vector3(0f, 1f, 0f)) + new Vector3(0f, 14f, -9f);
            Vector3 actual = camFollow.ComputeDestination();

            Assert.AreEqual(expected.x, actual.x, 0.001f, "X coordinate should match expected offset.");
            Assert.AreEqual(expected.y, actual.y, 0.001f, "Y coordinate should match expected offset.");
            Assert.AreEqual(expected.z, actual.z, 0.001f, "Z coordinate should match expected offset.");
        }

        [Test]
        public void ComputeDestination_WithYaw90_RotatesOffsetAroundTarget()
        {
            camFollow.CameraYaw = 90f;
            Vector3 actual = camFollow.ComputeDestination();
            Vector3 center = targetObj.transform.position + camFollow.TargetOffset;

            float horizontalDistance = Vector2.Distance(new Vector2(actual.x, actual.z), new Vector2(center.x, center.z));
            Assert.AreEqual(9f, horizontalDistance, 0.05f, "Horizontal distance from target center should remain 9 units regardless of yaw.");
            Assert.AreEqual(center.y + 14f, actual.y, 0.001f, "Vertical height should remain 14 units above target offset.");
        }

        [Test]
        public void ComputeDestination_WithYaw45_MaintainsRadialDistance()
        {
            camFollow.CameraYaw = 45f;
            Vector3 actual = camFollow.ComputeDestination();
            Vector3 center = targetObj.transform.position + camFollow.TargetOffset;

            float horizontalDistance = Vector2.Distance(new Vector2(actual.x, actual.z), new Vector2(center.x, center.z));
            Assert.AreEqual(9f, horizontalDistance, 0.05f, "Horizontal radius must remain 9 units at isometric 45 degree yaw.");
        }

        [Test]
        public void ComputeDestination_NullTarget_ReturnsCameraPositionWithoutException()
        {
            camFollow.Target = null;
            camObj.transform.position = new Vector3(12f, 34f, 56f);

            Vector3 destination = Vector3.zero;
            Assert.DoesNotThrow(() => destination = camFollow.ComputeDestination());
            Assert.AreEqual(camObj.transform.position, destination);
        }

        [Test]
        public void SnapToTarget_ImmediatelySetsPositionAndRotation()
        {
            camFollow.SnapToTarget();

            Vector3 expectedPos = camFollow.ComputeDestination();
            Assert.AreEqual(expectedPos.x, camObj.transform.position.x, 0.001f);
            Assert.AreEqual(expectedPos.y, camObj.transform.position.y, 0.001f);
            Assert.AreEqual(expectedPos.z, camObj.transform.position.z, 0.001f);

            Quaternion expectedRot = Quaternion.Euler(56f, 0f, 0f);
            Assert.AreEqual(expectedRot.eulerAngles.x, camObj.transform.rotation.eulerAngles.x, 0.1f);
        }
    }
}
