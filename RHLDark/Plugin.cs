using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace RHLDark {
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("RelicHuntersLegend.exe")]
    public class Plugin : BaseUnityPlugin {
        private Transform uiRoot;
        private bool didLoadingScreen;

        private Color Gray => new Color(0.9f, 0.9f, 0.9f);
        private Color AlmostBlack => new Color(0.1f, 0.1f, 0.1f);

        private void Awake() {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Start() {
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.GetActiveScene_Injected(out var scene);
            // Logger.LogInfo($"Current Scene: {scene.name}");
        } 

        private void OnSceneLoad(Scene scene, LoadSceneMode mode) {
            uiRoot = null;
            var sceneRoots = scene.GetRootGameObjects();

            if (!didLoadingScreen) {
                var screen = (LoadingManager)TypeUtil.GetStaticPrivateField(typeof(LoadingManager), "_instance");
                var container = (Transform)TypeUtil.GetPrivateField(screen, "_container");
                HasChild(container, "Background", out Transform background);
                background.GetComponent<Image>().color = AlmostBlack;

                didLoadingScreen = true;
            }

            if (scene.name == "Launcher") {
                try {
                    foreach (var obj in sceneRoots) {
                        if (obj.name == "UI") {
                            HasChild(obj.transform, "MainMenuWindowsController", out uiRoot);
                            LauncherPass();
                        }
                    }
                } catch (Exception ex) {
                    Logger.LogError("Error during Launcher Setup: " + ex);
                }
            } else if (scene.name == "Gameplay") {
                try {
                    foreach (var obj in sceneRoots) {
                        if (obj.name == "_MustHavePrefabs") {
                            if (HasChild(obj.transform, "MainGameCanvas", out var canvas)) {
                                HasChild(canvas, "InGameWindowsController", out uiRoot);
                                GameplayPass();
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.LogError("Error during Gameplay Setup: " + ex);
                }
            }

            if (!uiRoot) return;

            if (HasChild(uiRoot, "Windows", out var windows)) {
                SharedPass(GetAllChildren(windows));
            }
        }

        private Transform GetWindow(string name) {
            HasChild(uiRoot, "Windows", out Transform windows);
            HasChild(windows, name, out Transform result);
            return result;
        }

        private void SharedPass(Transform[] windows) {
            foreach (var window in windows) {
                if (HasChild(window, "Background", out var backgroundContainer)) {
                    if (HasChild(backgroundContainer, "FlatBackground", out var middle)) {
                        var comp = middle.GetComponent<Image>();
                        comp.color = AlmostBlack;
                    } else if (backgroundContainer.TryGetComponent<Image>(out var image)) {
                        image.color = AlmostBlack;
                    }
                }
            }

            {
                HasChild(uiRoot, "HangingFeedbackSystem", out Transform feedback);
                HasChild(feedback, "root", out var root);
                HasChild(root, "Background", out var feedbackground);
                feedbackground.GetComponent<Image>().color = AlmostBlack;
            }
        }

        private void LauncherPass() {
            var login = GetWindow("LoginWindow");
            HasChild(login, "ScreenPivot", out var pivot);

            HasChild(login, "LoginBackground", out var back);
            back.GetComponent<Image>().color = AlmostBlack;

            HasChild(pivot, "FoundersAlpha", out Transform alpha);
            alpha.GetComponent<TextMeshProUGUI>().color = Color.white;
        }

        private void GameplayPass() {
            var window = GetWindow("HunterWindow");
            var parallax = window.GetComponent<UIParallax>();
            StartCoroutine(ParallaxRoutine(parallax));

            if (HasChild(window, "Background", out Transform container)) {
                HasChild(container, "FlatBackground", out var flat);
                flat.gameObject.SetActive(false);
            }

            if (HasChild(window, "Data", out Transform data)) {
                HasChild(data, "HunterDisplayGUI", out var display);
                HasChild(data, "HunterInformation", out var info);

                display.gameObject.SetActive(false);

                {
                    HasChild(info, "HunterScreenPlayerCard", out var card);
                    HasChild(card, "PortraitMask", out var portrait);
                    HasChild(info, "Level", out var level);

                    level.GetComponentInChildren<TextMeshProUGUI>().color = Gray;

                    foreach (var text in card.GetComponentsInChildren<TextMeshProUGUI>()) {
                        text.color = Gray;
                    }

                    portrait.gameObject.SetActive(false);

                    var pos = card.localPosition;
                    pos.x = -703;
                    card.localPosition = pos;
                }
            }
        }

        private bool HasChild(Transform parent, string childName, out Transform child) {
            for (int i = 0; i < parent.childCount; i++) {
                var trans = parent.GetChild(i);
                if (trans == parent) continue;
                if (trans.name == childName) {
                    child = trans;
                    return true;
                }
            }

            child = null;
            return false;
        }

        private Transform[] GetAllChildren(Transform parent) {
            var arr = new Transform[parent.childCount];
            for (int i = 0; i < arr.Length; i++) {
                arr[i] = parent.GetChild(i);
            }
            return arr;
        }

        private T[] GetAllChildrenExcluding<T>(Transform parent, params string[] excluding) where T : MonoBehaviour {
            return parent.GetComponentsInChildren<T>().Where(x => {
                foreach (var str in excluding) {
                    if (x.transform.name.EndsWith(str)) {
                        return false;
                    }
                }
                return true;
            }).ToArray();
        }

        private IEnumerator ParallaxRoutine(UIParallax parallax) {
            yield return new WaitUntil(() => ((float)TypeUtil.GetPrivateField(parallax, "_currentMultiplier")) == 0.15f);
            TypeUtil.SetPrivateField(parallax, "_currentMultiplier", 0.39f);
            TypeUtil.SetPrivateField(parallax, "_lerpSpeed", 10000f);
        }
    }
}
