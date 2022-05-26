using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace LightmapBaker
{
    public class LightmapBaker : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualTreeAsset;

        [MenuItem("Tools/TA/Lightmap Baker")]
        public static void ShowWindow()
        {
            LightmapBaker wnd = GetWindow<LightmapBaker>();
            wnd.titleContent = new GUIContent("Lightmap Baker");
        }

        [SerializeField] private SceneAsset[] scenes;

        private int? currentBakeSceneIndex;

        private Button StartButton => rootVisualElement.Q<Button>("start");
        private Button StopButton => rootVisualElement.Q<Button>("stop");
        private VisualElement InputData => rootVisualElement.Q<VisualElement>("inputData");
        private VisualElement ActiveBakeData => rootVisualElement.Q<VisualElement>("activeBakeData");
        private ProgressBar BakeProgressBar => rootVisualElement.Q<ProgressBar>("bakeProgress");

        public void CreateGUI()
        {
            rootVisualElement.Add(visualTreeAsset.Instantiate());
            var windowSo = new SerializedObject(this);
            rootVisualElement.Bind(windowSo);
            
            StartButton.clicked += StartBake;
            StopButton.clicked += StopBake;
            
            ShowActiveBakeElements(false);
        }

        private void StartBake()
        {
            Lightmapping.ForceStop();
            Lightmapping.bakeCompleted += TryBakeNextScene;
            
            currentBakeSceneIndex = -1;
            TryBakeNextScene();
            
            ShowActiveBakeElements(true);
        }

        private void StopBake()
        {
            Lightmapping.bakeCompleted -= TryBakeNextScene;
            Lightmapping.ForceStop();
            
            BakeProgressBar.title = $"Bake stopped: {currentBakeSceneIndex}/{scenes.Length}";
            currentBakeSceneIndex = null;

            ShowActiveBakeElements(false);
        }

        private void TryBakeNextScene()
        {
            Assert.IsTrue(currentBakeSceneIndex != null, "Bake started without null scene index");
            currentBakeSceneIndex++;
            if (scenes == null || scenes.Length == currentBakeSceneIndex)
            {
                Lightmapping.bakeCompleted -= TryBakeNextScene;
                
                currentBakeSceneIndex = null;
                BakeProgressBar.value = 1;
                BakeProgressBar.title = "Complete bake";
                
                ShowActiveBakeElements(false);
            }
            else
            {
                var sceneIndex = currentBakeSceneIndex.Value;
                var activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(activeScene);
                
                var nextScene = scenes[sceneIndex];
                if (nextScene == null)
                {
                    TryBakeNextScene();
                    return;
                }

                float progress = sceneIndex / (float)scenes.Length;
                BakeProgressBar.value = progress;
                BakeProgressBar.title = $"Bake {sceneIndex}/{scenes.Length}: {nextScene.name}";
                
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(nextScene));
                Lightmapping.BakeAsync();
            }
        }

        private void ShowActiveBakeElements(bool isActiveBake)
        {
            StartButton.SetEnabled(!isActiveBake);
            InputData.SetEnabled(!isActiveBake);

            ActiveBakeData.SetEnabled(isActiveBake);
            StopButton.SetEnabled(isActiveBake);
        }
    }
}