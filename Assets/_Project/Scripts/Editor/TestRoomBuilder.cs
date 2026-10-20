using System.Linq;
using Platformer.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 메뉴 한 번으로 테스트 룸 씬을 만든다.
    /// 카메라(Pixel Perfect) 설정 → Ground 타일맵 구성 → 텍스트 맵대로 타일 칠하기 → 저장.
    /// 여러 번 실행해도 기존 오브젝트를 찾아 다시 설정하므로 안전하다.
    /// </summary>
    public static class TestRoomBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/TestRoom.unity";
        const string LegacyScenePath = "Assets/TestRoom.unity";
        const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";
        const string SpritePath = "Assets/_Project/Art/Placeholder/Placeholder_Tile.png";
        const string TilePath = "Assets/_Project/Art/Placeholder/Tile_Placeholder.asset";
        const string MovementSettingsPath = "Assets/_Project/Data/PlayerMovementSettings.asset";
        const string NoFrictionPath = "Assets/_Project/Data/NoFriction.physicsMaterial2D";

        const int PixelsPerUnit = 16;
        const int ReferenceWidth = 320;
        const int ReferenceHeight = 180;

        static readonly Color BackgroundColor = new Color32(0x1A, 0x1A, 0x2E, 0xFF);
        static readonly Color GroundColor = new Color32(0x6C, 0x7A, 0x89, 0xFF);
        static readonly Color PlayerColor = new Color32(0xE8, 0x83, 0x3A, 0xFF);

        // 임시 플레이어: 12x36px. 바닥(y = -4) 위 왼쪽에 세운다
        static readonly Vector2 PlayerSize = new(0.75f, 2.25f);
        static readonly Vector2 PlayerSpawn = new(-7f, -4f + 2.25f * 0.5f);

        // 테스트 룸 지형 (20x12 타일 = 1화면 방). '#' = Ground 타일, '.' = 빈칸
        // 맨 윗줄이 y = 5, 맨 아랫줄이 y = -6, 왼쪽 끝이 x = -10
        static readonly string[] Layout =
        {
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#..................#",
            "#...........####...#",
            "#..................#",
            "#...####...........#",
            "#..................#",
            "#..................#",
            "####################",
            "####################",
        };
        const int OriginX = -10;
        const int TopY = 5;

        [MenuItem("Tools/Platformer/Build Test Room")]
        static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = OpenOrCreateScene();
            if (!scene.IsValid()) return;

            TileBase tile = LoadOrCreateTile();
            if (tile == null) return;

            SetupCamera();
            Tilemap ground = SetupGround();
            Paint(ground, tile);
            SetupPlayer();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuildSettings();

            Debug.Log($"[TestRoomBuilder] 테스트 룸 생성 완료: {ScenePath}");
        }

        static Scene OpenOrCreateScene()
        {
            if (!AssetExists(ScenePath))
            {
                if (AssetExists(LegacyScenePath))
                {
                    // 수동으로 Assets 루트에 만든 씬은 정해진 폴더로 옮긴다
                    string error = AssetDatabase.MoveAsset(LegacyScenePath, ScenePath);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogError($"[TestRoomBuilder] 씬 이동 실패: {error}");
                        return default;
                    }
                }
                else if (!AssetDatabase.CopyAsset(TemplateScenePath, ScenePath))
                {
                    // SampleScene에는 카메라와 Global Light 2D가 이미 들어 있다
                    Debug.LogError($"[TestRoomBuilder] 씬 생성 실패: {TemplateScenePath} 복사 불가");
                    return default;
                }
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static TileBase LoadOrCreateTile()
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
            if (tile != null) return tile;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogError($"[TestRoomBuilder] 스프라이트를 찾을 수 없음: {SpritePath}");
                return null;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, TilePath);
            return tile;
        }

        static void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;

            var ppc = GetOrAdd<PixelPerfectCamera>(cam.gameObject);
            ppc.assetsPPU = PixelsPerUnit;
            ppc.refResolutionX = ReferenceWidth;
            ppc.refResolutionY = ReferenceHeight;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
        }

        static Tilemap SetupGround()
        {
            Grid grid = Object.FindAnyObjectByType<Grid>();
            if (grid == null)
                grid = new GameObject("Grid").AddComponent<Grid>();

            Tilemap tilemap = grid.GetComponentInChildren<Tilemap>();
            if (tilemap == null)
            {
                var go = new GameObject("Ground");
                go.transform.SetParent(grid.transform, false);
                tilemap = go.AddComponent<Tilemap>();
                go.AddComponent<TilemapRenderer>();
            }

            GameObject ground = tilemap.gameObject;
            ground.name = "Ground";

            int layer = LayerMask.NameToLayer("Ground");
            if (layer < 0)
                Debug.LogError("[TestRoomBuilder] 'Ground' 레이어가 없습니다. Tags and Layers를 확인하세요.");
            else
                ground.layer = layer;

            tilemap.color = GroundColor;

            var body = GetOrAdd<Rigidbody2D>(ground);
            body.bodyType = RigidbodyType2D.Static;

            var tileCollider = GetOrAdd<TilemapCollider2D>(ground);
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var composite = GetOrAdd<CompositeCollider2D>(ground);
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            return tilemap;
        }

        static void Paint(Tilemap tilemap, TileBase tile)
        {
            tilemap.ClearAllTiles();

            for (int row = 0; row < Layout.Length; row++)
            {
                string line = Layout[row];
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] == '#')
                        tilemap.SetTile(new Vector3Int(OriginX + col, TopY - row, 0), tile);
                }
            }

            tilemap.CompressBounds();
        }

        static void SetupPlayer()
        {
            PlayerMovement movement = Object.FindAnyObjectByType<PlayerMovement>();
            GameObject player = movement != null ? movement.gameObject : new GameObject("Player");

            player.name = "Player";
            player.tag = "Player";
            int layer = LayerMask.NameToLayer("Player");
            if (layer < 0)
                Debug.LogError("[TestRoomBuilder] 'Player' 레이어가 없습니다. Tags and Layers를 확인하세요.");
            else
                player.layer = layer;
            player.transform.position = PlayerSpawn;

            var body = GetOrAdd<Rigidbody2D>(player);
            body.bodyType = RigidbodyType2D.Dynamic;
            // 벽에 밀착한 채 공중에서 걸리지 않도록 마찰 0
            body.sharedMaterial = LoadOrCreateNoFriction();

            var box = GetOrAdd<BoxCollider2D>(player);
            box.size = PlayerSize;
            box.offset = Vector2.zero;

            GetOrAdd<PlayerInputReader>(player);
            movement = GetOrAdd<PlayerMovement>(player);

            // private [SerializeField] 필드는 SerializedObject로 설정한다
            var serialized = new SerializedObject(movement);
            serialized.FindProperty("settings").objectReferenceValue = LoadOrCreateMovementSettings();
            serialized.FindProperty("groundLayers").intValue = LayerMask.GetMask("Ground", "OneWay");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Transform visual = SetupPlayerVisual(player);

            var crouch = GetOrAdd<PlayerCrouch>(player);
            var crouchSerialized = new SerializedObject(crouch);
            crouchSerialized.FindProperty("obstacleLayers").intValue = LayerMask.GetMask("Ground");
            crouchSerialized.FindProperty("visual").objectReferenceValue = visual;
            crouchSerialized.ApplyModifiedPropertiesWithoutUndo();

            var ledgeGrab = GetOrAdd<PlayerLedgeGrab>(player);
            var ledgeSerialized = new SerializedObject(ledgeGrab);
            ledgeSerialized.FindProperty("ledgeLayers").intValue = LayerMask.GetMask("Ground");
            ledgeSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform SetupPlayerVisual(GameObject player)
        {
            Transform visual = player.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(player.transform, false);
            }

            visual.localPosition = Vector3.zero;
            visual.localScale = new Vector3(PlayerSize.x, PlayerSize.y, 1f);

            var sprite = GetOrAdd<SpriteRenderer>(visual.gameObject);
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            sprite.color = PlayerColor;
            sprite.sortingOrder = 10;
            return visual;
        }

        static PlayerMovementSettings LoadOrCreateMovementSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerMovementSettings>(MovementSettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<PlayerMovementSettings>();
            AssetDatabase.CreateAsset(settings, MovementSettingsPath);
            return settings;
        }

        static PhysicsMaterial2D LoadOrCreateNoFriction()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (material != null) return material;

            material = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
            return material;
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.path != ScenePath && s.path != LegacyScenePath)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }

        static bool AssetExists(string path) =>
            !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
    }
}
