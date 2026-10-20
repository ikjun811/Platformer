using System.Linq;
using Platformer.Gameplay;
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
    /// 카메라 설정 → Ground 타일맵 구성 → 텍스트 맵대로 타일 칠하기 → 플레이어 구성 → 저장.
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
        const string CameraSettingsPath = "Assets/_Project/Data/CameraSettings.asset";
        const string CombatSettingsPath = "Assets/_Project/Data/PlayerCombatSettings.asset";
        const string CharacterArtFolder = "Assets/_Project/Art/Characters/";

        // 세로 11.25타일이 보이는 카메라 (GDD 5.1)
        const float CameraSize = 5.625f;

        static readonly Color BackgroundColor = new Color32(0x1A, 0x1A, 0x2E, 0xFF);
        static readonly Color GroundColor = new Color32(0x6C, 0x7A, 0x89, 0xFF);

        // 임시 플레이어: 12x36px. 바닥(y = -4) 위 왼쪽에 세운다
        static readonly Vector2 PlayerSize = new(0.75f, 2.25f);
        static readonly Vector2 PlayerSpawn = new(-7f, -4f + 2.25f * 0.5f);

        // 테스트 룸 지형 (40x24 타일 = 가로 2화면 x 세로 2화면). '#' = Ground 타일, '.' = 빈칸
        // 맨 윗줄이 y = 17, 맨 아랫줄이 y = -6, 왼쪽 끝이 x = -10
        // 아래층 왼쪽은 기존 지형, 오른쪽 계단 발판으로 위층 바닥 구멍(x = 18~23)을 지나 위층으로 올라간다
        // y = -2 줄의 발판 아래는 높이 2타일 통로다 (서서는 못 지나가고 슬라이드로 통과). x = 16~23은 긴 슬라이드 통로
        static readonly string[] Layout =
        {
            "########################################",  // y = 17 천장
            "#......................................#",
            "#......................................#",
            "#......................................#",
            "#...........####...............#####...#",  // y = 13
            "#......................................#",
            "#......................................#",
            "#....#####..............####...........#",  // y = 10
            "#................#####.................#",  // y = 9
            "#......................................#",
            "#......................................#",
            "############################......######",  // y = 6 위층 바닥
            "#......................................#",
            "#............................####......#",  // y = 4
            "#......................................#",
            "#......................................#",
            "#......................####............#",  // y = 1
            "#...........####.......................#",  // y = 0
            "#......................................#",
            "#...####..........####....########.....#",  // y = -2
            "#......................................#",
            "#......................................#",
            "########################################",  // y = -5 아래층 바닥
            "########################################",  // y = -6
        };
        const int OriginX = -10;
        const int TopY = 17;

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
            PlayerMovement player = SetupPlayer();
            SetupCameraFollow(player);

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
            cam.orthographicSize = CameraSize;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;

            // 고해상도 일러스트로 전환하면서 Pixel Perfect Camera는 쓰지 않는다 (GDD 8장)
            var ppc = cam.GetComponent<PixelPerfectCamera>();
            if (ppc != null)
                Object.DestroyImmediate(ppc);
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

        static void SetupCameraFollow(PlayerMovement player)
        {
            Camera cam = Camera.main;
            var follow = GetOrAdd<CameraFollow>(cam.gameObject);

            // 지형 전체를 카메라가 보여줄 수 있는 영역으로 쓴다
            int width = Layout[0].Length;
            int height = Layout.Length;
            var bounds = new Rect(OriginX, TopY - height + 1, width, height);

            var serialized = new SerializedObject(follow);
            serialized.FindProperty("settings").objectReferenceValue = LoadOrCreateAsset<CameraSettings>(CameraSettingsPath);
            serialized.FindProperty("target").objectReferenceValue = player;
            serialized.FindProperty("bounds").rectValue = bounds;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static PlayerMovement SetupPlayer()
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
            serialized.FindProperty("settings").objectReferenceValue = LoadOrCreateAsset<PlayerMovementSettings>(MovementSettingsPath);
            serialized.FindProperty("groundLayers").intValue = LayerMask.GetMask("Ground", "OneWay");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SpriteRenderer visual = SetupPlayerVisual(player);

            var crouch = GetOrAdd<PlayerCrouch>(player);
            var crouchSerialized = new SerializedObject(crouch);
            crouchSerialized.FindProperty("obstacleLayers").intValue = LayerMask.GetMask("Ground");
            // 앉기·슬라이드는 애니메이션 프레임으로 표시하므로 임시 세로 축소는 쓰지 않는다
            crouchSerialized.FindProperty("visual").objectReferenceValue = null;
            crouchSerialized.ApplyModifiedPropertiesWithoutUndo();

            GetOrAdd<PlayerSlide>(player);
            SetupPlayerCombat(player);

            var ledgeGrab = GetOrAdd<PlayerLedgeGrab>(player);
            var ledgeSerialized = new SerializedObject(ledgeGrab);
            ledgeSerialized.FindProperty("ledgeLayers").intValue = LayerMask.GetMask("Ground");
            ledgeSerialized.ApplyModifiedPropertiesWithoutUndo();

            SetupPlayerAnimator(player, visual);

            return movement;
        }

        static void SetupPlayerCombat(GameObject player)
        {
            var combatSettings = LoadOrCreateAsset<PlayerCombatSettings>(CombatSettingsPath);
            var placeholder = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            // 임시 조준 표시: 총구 위치를 보여주는 작은 사각형
            Transform marker = player.transform.Find("AimMarker");
            if (marker == null)
            {
                marker = new GameObject("AimMarker").transform;
                marker.SetParent(player.transform, false);
            }
            marker.localScale = new Vector3(0.25f, 0.25f, 1f);
            var markerSprite = GetOrAdd<SpriteRenderer>(marker.gameObject);
            markerSprite.sprite = placeholder;
            markerSprite.color = Color.white;
            markerSprite.sortingOrder = 11;

            var aim = GetOrAdd<PlayerAim>(player);
            var aimSerialized = new SerializedObject(aim);
            aimSerialized.FindProperty("settings").objectReferenceValue = combatSettings;
            aimSerialized.FindProperty("aimMarker").objectReferenceValue = marker;
            aimSerialized.ApplyModifiedPropertiesWithoutUndo();

            var shooter = GetOrAdd<PlayerShooter>(player);
            var shooterSerialized = new SerializedObject(shooter);
            shooterSerialized.FindProperty("settings").objectReferenceValue = combatSettings;
            shooterSerialized.FindProperty("hitLayers").intValue = LayerMask.GetMask("Ground", "Enemy");
            shooterSerialized.FindProperty("beamSprite").objectReferenceValue = placeholder;
            shooterSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SpriteRenderer SetupPlayerVisual(GameObject player)
        {
            Transform visual = player.transform.Find("Visual");
            if (visual == null)
            {
                visual = new GameObject("Visual").transform;
                visual.SetParent(player.transform, false);
            }

            // 위치(충돌체 바닥)와 스프라이트는 플레이 중에 PlayerAnimator가 정한다
            visual.localPosition = new Vector3(0f, -PlayerSize.y * 0.5f, 0f);
            visual.localScale = Vector3.one;

            var sprite = GetOrAdd<SpriteRenderer>(visual.gameObject);
            sprite.sprite = null;
            sprite.color = Color.white;
            sprite.sortingOrder = 10;
            return sprite;
        }

        static void SetupPlayerAnimator(GameObject player, SpriteRenderer visual)
        {
            var animator = GetOrAdd<PlayerAnimator>(player);
            var serialized = new SerializedObject(animator);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = visual;

            // 시트는 Tools/ArtPipeline/process_sprites.py가 만든다. 칸 크기·초당 프레임은 그 스크립트의 설정과 맞춘다
            SetClip(serialized, "idle", "Player_Idle", 384, 6f);
            SetClip(serialized, "run", "Player_Run", 384, 14f);
            SetClip(serialized, "jump", "Player_Jump", 384, 0f);
            SetClip(serialized, "crouch", "Player_Crouch", 384, 0f);
            SetClip(serialized, "slide", "Player_Slide", 384, 10f);
            SetClip(serialized, "ledgeHang", "Player_LedgeHang", 384, 0f);
            SetClip(serialized, "ledgeClimb", "Player_LedgeClimb", 512, 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetClip(SerializedObject animator, string field, string sheetName, int cellWidth, float framesPerSecond)
        {
            string path = $"{CharacterArtFolder}{sheetName}.png";
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (sheet == null)
                Debug.LogWarning($"[TestRoomBuilder] 캐릭터 시트를 찾을 수 없음: {path}");

            SerializedProperty clip = animator.FindProperty(field);
            clip.FindPropertyRelative("sheet").objectReferenceValue = sheet;
            clip.FindPropertyRelative("cellWidth").intValue = cellWidth;
            clip.FindPropertyRelative("pivotX").intValue = 192;
            clip.FindPropertyRelative("framesPerSecond").floatValue = framesPerSecond;
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
