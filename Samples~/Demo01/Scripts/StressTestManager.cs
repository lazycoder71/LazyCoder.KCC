using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

namespace LFramework.KCC.Demo01
{
    public class StressTestManager : MonoBehaviour
    {
        public Camera Camera;
        public LayerMask UIMask;

        public InputField CountField;
        public Image RenderOn;
        public Image SimOn;
        public Image InterpOn;
        public ExampleCharacterController CharacterPrefab;
        public ExampleAIController AIController;
        public int SpawnCount = 100;
        public float SpawnDistance = 2f;

        private void Start()
        {
            KCCSystem.EnsureCreation();
            CountField.text = SpawnCount.ToString();
            UpdateOnImages();

            KCCSystem.Settings.AutoSimulation = false;
            KCCSystem.Settings.Interpolate = false;
        }

        private void Update()
        {
            KCCSystem.Simulate(Time.deltaTime, KCCSystem.CharacterMotors, KCCSystem.PhysicsMovers);
        }

        private void UpdateOnImages()
        {
            RenderOn.enabled = Camera.cullingMask == -1;
            SimOn.enabled = Physics.simulationMode == SimulationMode.FixedUpdate;
            InterpOn.enabled = KCCSystem.Settings.Interpolate;
        }

        public void SetSpawnCount(string count)
        {
            if (int.TryParse(count, out int result))
            {
                SpawnCount = result;
            }
        }

        public void ToggleRendering()
        {
            if (Camera.cullingMask == -1)
            {
                Camera.cullingMask = UIMask;
            }
            else
            {
                Camera.cullingMask = -1;
            }
            UpdateOnImages();
        }

        public void TogglePhysicsSim()
        {
            Physics.simulationMode = Physics.simulationMode == SimulationMode.FixedUpdate ? SimulationMode.Script : SimulationMode.FixedUpdate;
            UpdateOnImages();
        }

        public void ToggleInterpolation()
        {
            KCCSystem.Settings.Interpolate = !KCCSystem.Settings.Interpolate;
            UpdateOnImages();
        }

        public void Spawn()
        {
            for (int i = 0; i < AIController.Characters.Count; i++)
            {
                Destroy(AIController.Characters[i].gameObject);
            }
            AIController.Characters.Clear();

            int charsPerRow = Mathf.CeilToInt(Mathf.Sqrt(SpawnCount));
            Vector3 firstPos = ((charsPerRow * SpawnDistance) * 0.5f) * -Vector3.one;
            firstPos.y = 0f;

            for (int i = 0; i < SpawnCount; i++)
            {
                int row = i / charsPerRow;
                int col = i % charsPerRow;
                Vector3 pos = firstPos + (Vector3.right * row * SpawnDistance) + (Vector3.forward * col * SpawnDistance);

                ExampleCharacterController newChar = Instantiate(CharacterPrefab);
                newChar.Motor.SetPosition(pos);

                AIController.Characters.Add(newChar);
            }
        }
    }
}