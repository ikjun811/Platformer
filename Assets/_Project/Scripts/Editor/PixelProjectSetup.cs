using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Platformer.EditorTools
{
    /// <summary>
    /// GDD v2 8장의 픽셀아트 화면 설정을 메뉴로 적용한다.
    /// - 소팅 레이어: Default 뒤에 Background → Tiles → Props → Characters → Projectiles → FX → UI 순서로 추가·정렬한다
    /// - Pixel Perfect Camera: 현재 씬의 Main Camera에 기준 해상도 640x360, PPU 64로 붙인다
    /// 여러 번 실행해도 같은 결과가 되도록 이미 있는 항목은 다시 맞추기만 한다.
    /// </summary>
    public static class PixelProjectSetup
    {
        const string TagManagerPath = "ProjectSettings/TagManager.asset";

        // 뒤에 있을수록 앞에 그려진다
        static readonly string[] SortingLayers =
        {
            "Background", "Tiles", "Props", "Characters", "Projectiles", "FX", "UI",
        };

        // 640x360 = 가로 10타일 x 세로 5.625타일 (1타일 = 64px)
        const int AssetsPixelsPerUnit = 64;
        const int ReferenceWidth = 640;
        const int ReferenceHeight = 360;

        [MenuItem("Tools/Platformer/Setup/Sorting Layers")]
        static void SetupSortingLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(TagManagerPath)[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");

            int defaultIndex = FindLayer(layers, "Default");
            for (int i = 0; i < SortingLayers.Length; i++)
            {
                int index = FindLayer(layers, SortingLayers[i]);
                if (index < 0)
                {
                    index = layers.arraySize;
                    layers.InsertArrayElementAtIndex(index);
                    SerializedProperty added = layers.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("name").stringValue = SortingLayers[i];
                    added.FindPropertyRelative("uniqueID").intValue = NewUniqueId(layers);
                    added.FindPropertyRelative("locked").boolValue = false;
                }

                // Default 바로 뒤부터 정해진 순서로 놓는다
                int target = defaultIndex + 1 + i;
                if (index != target)
                    layers.MoveArrayElement(index, target);
            }

            tagManager.ApplyModifiedProperties();

            var names = new string[layers.arraySize];
            for (int i = 0; i < layers.arraySize; i++)
                names[i] = layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            Debug.Log($"[PixelProjectSetup] 소팅 레이어 순서: {string.Join(" → ", names)}");
        }

        [MenuItem("Tools/Platformer/Setup/Pixel Perfect Camera (Current Scene)")]
        static void SetupPixelPerfectCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[PixelProjectSetup] 현재 씬에 MainCamera 태그가 붙은 카메라가 없습니다.");
                return;
            }

            ApplyPixelPerfectCamera(cam);
            EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
            Debug.Log($"[PixelProjectSetup] {cam.name}에 Pixel Perfect Camera 적용 ({ReferenceWidth}x{ReferenceHeight}, PPU {AssetsPixelsPerUnit}). 씬을 저장하세요.");
        }

        /// <summary>카메라에 GDD v2 기준 Pixel Perfect Camera를 붙이고 값을 맞춘다 (씬 빌더에서도 쓴다).</summary>
        internal static void ApplyPixelPerfectCamera(Camera cam)
        {
            Undo.RecordObject(cam, "Setup Pixel Perfect Camera");
            cam.orthographic = true;

            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc == null)
                ppc = Undo.AddComponent<PixelPerfectCamera>(cam.gameObject);

            Undo.RecordObject(ppc, "Setup Pixel Perfect Camera");
            ppc.assetsPPU = AssetsPixelsPerUnit;
            ppc.refResolutionX = ReferenceWidth;
            ppc.refResolutionY = ReferenceHeight;
            // 창 크기가 정수배가 아니어도 보이는 범위(10 x 5.625타일)가 같도록 남는 부분은 검은 테두리로 둔다
            ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
            // 스프라이트 위치를 픽셀 격자에 맞춘다 (크기를 정수배로만 쓰므로 렌더 텍스처 확대는 쓰지 않는다)
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            EditorUtility.SetDirty(ppc);
        }

        static int FindLayer(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name)
                    return i;
            }
            return -1;
        }

        static int NewUniqueId(SerializedProperty layers)
        {
            while (true)
            {
                int id = Random.Range(1, int.MaxValue);
                bool used = false;
                for (int i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").intValue == id)
                    {
                        used = true;
                        break;
                    }
                }
                if (!used) return id;
            }
        }
    }
}
