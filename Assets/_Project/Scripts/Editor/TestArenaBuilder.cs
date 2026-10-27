using System;
using System.Linq;
using Platformer.Enemies;
using Platformer.Gameplay;
using Platformer.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Platformer.EditorTools
{
    /// <summary>
    /// 메뉴 한 번으로 GDD v2 테스트 씬(TestArena)을 만든다.
    /// marsh-sci-fi 타일로 30x12타일 2층 소탕 방을 칠하고, 얇은 발판(OneWay)·계단·플레이어·임시 적·픽셀 카메라를 배치한다.
    /// 씬 안의 생성 오브젝트(Grid, Walkways, Player, TestEnemies)는 실행할 때마다 지우고 다시 만든다.
    /// 수치 SO는 Data/V2에 없을 때만 새 크기 기준 임시값으로 만들고, 이미 있으면 건드리지 않는다 (N1에서 조정한 값 보호).
    /// </summary>
    public static class TestArenaBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";
        const string TileArtFolder = "Assets/_Project/Art/Tiles/";
        const string TileAssetFolder = "Assets/_Project/Art/Tiles/TileAssets";
        const string PropsFolder = "Assets/_Project/Art/Props/";
        const string MarineFolder = "Assets/_Project/Art/Characters/Marine/";
        const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/Placeholder_Tile.png";
        const string NoFrictionPath = "Assets/_Project/Data/NoFriction.physicsMaterial2D";
        const string DataFolder = "Assets/_Project/Data/V2";

        static readonly Color BackgroundColor = new Color32(0x0A, 0x08, 0x14, 0xFF);
        static readonly Color EnemyColor = new Color32(0xE0, 0x86, 0x2E, 0xFF);

        // 해병 키 약 40px, 몸통 폭 약 20px (GDD v2 8장)
        static readonly Vector2 PlayerSize = new(0.3125f, 0.625f);
        // 임시 적: 러너 몸길이·키에 맞춘 상자 (N3에서 러너로 교체)
        static readonly Vector2 EnemySize = new(1.2f, 0.6f);

        // 얇은 발판 그림(32x32)에서 위쪽 13px만 판이다
        const float WalkwayThickness = 13f / 64f;

        // 30x12타일. 맨 윗줄이 y = 11, 맨 아랫줄이 y = 0, 왼쪽 끝이 x = 0
        // '#' = 벽·바닥·천장 (위치에 따라 그림을 고른다), '.' = 빈칸 (배경만)
        // '=' = 얇은 발판. 그 칸의 윗면이 발판 윗면이다 (y = 3 줄이면 윗면 y = 4)
        // 'S' = 계단 블록 8칸 한 묶음, 'P' = 플레이어 시작, 'E' = 임시 적
        // 바닥 윗면 y = 2, 발판 윗면 y = 4 / 6 / 8 (한 층 2타일, GDD 5.3)
        static readonly string[] Layout =
        {
            "##############################",  // y = 11 천장 기둥
            "##############################",  // y = 10 천장 환기구
            "#............................#",  // y = 9
            "#............................#",  // y = 8
            "#..======....................#",  // y = 7 발판 (윗면 8)
            "#............................#",  // y = 6
            "#.........==========.........#",  // y = 5 발판 (윗면 6)
            "#............................#",  // y = 4
            "#=========...........========#",  // y = 3 발판 (윗면 4)
            "#.P.........SSSSSSSS.....E...#",  // y = 2 바닥 위 (계단, 시작 위치, 적)
            "##############################",  // y = 1 바닥면
            "##############################",  // y = 0 기초
        };
        const int TopY = 11;
        static int Width => Layout[0].Length;
        static int Height => Layout.Length;

        static readonly string[] StairSequence = { "1a1a", "1a2a", "1a2b", "1a3a", "1b1a", "1b2a", "1b2b", "1b3a" };

        // 벽 아래쪽(y = 2) 배경 특수 타일: x → 코드. 그 외는 민 벽(4i1a)
        static readonly (int x, string code)[] LowerWallProps =
        {
            (1, "4c1a"), (6, "4d1a"), (9, "4j1a"), (22, "4f1a"), (24, "4h1a"), (26, "4g1a"), (28, "4e1a"),
        };
        // 벽 위쪽(y = 3) 배경 장식 타일
        static readonly (int x, string code)[] UpperWallProps =
        {
            (8, "3e1a"), (14, "3b1a"), (17, "3c1a"), (20, "3d1a"),
        };

        [MenuItem("Tools/Platformer/Build Test Arena")]
        static void Build()
        {
            if (!ValidateLayout()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = OpenOrCreateScene();
            if (!scene.IsValid()) return;

            foreach (string rootName in new[] { "Grid", "Walkways", "Player", "TestEnemies" })
            {
                GameObject old = GameObject.Find(rootName);
                if (old != null) Object.DestroyImmediate(old);
            }

            EnsureFolder(TileAssetFolder);
            EnsureFolder(DataFolder);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            PaintBackground(CreateTilemap(grid, "BackWall", "Background", null));
            PaintSolid(CreateTilemap(grid, "Ground", "Tiles", "Ground"));
            BuildWalkways();
            PlayerMovement player = BuildPlayer();
            BuildTestEnemies();
            SetupCamera(player);
            SetupGlobalLight();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AddToBuildSettings();

            Debug.Log($"[TestArenaBuilder] 테스트 아레나 생성 완료: {ScenePath}");
        }

        static bool ValidateLayout()
        {
            for (int row = 0; row < Layout.Length; row++)
            {
                if (Layout[row].Length == Width) continue;
                Debug.LogError($"[TestArenaBuilder] Layout {row}번째 줄 길이가 {Layout[row].Length}입니다 ({Width}이어야 함)");
                return false;
            }
            return true;
        }

        static Scene OpenOrCreateScene()
        {
            // SampleScene에는 카메라와 Global Light 2D가 이미 들어 있다
            if (!AssetExists(ScenePath) && !AssetDatabase.CopyAsset(TemplateScenePath, ScenePath))
            {
                Debug.LogError($"[TestArenaBuilder] 씬 생성 실패: {TemplateScenePath} 복사 불가");
                return default;
            }
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ───────── 타일맵 ─────────

        static Tilemap CreateTilemap(Grid grid, string name, string sortingLayer, string physicsLayer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingLayerName = sortingLayer;

            if (physicsLayer == null) return tilemap;

            go.layer = RequireLayer(physicsLayer);
            go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
            go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
            return tilemap;
        }

        // 모든 빈칸 뒤에 벽을 칠한다: 아래 두 줄은 복도 벽(4행·3행), 그 위는 어두운 샤프트(7~9행)
        static void PaintBackground(Tilemap tilemap)
        {
            for (int y = 2; y <= TopY - 2; y++)
            {
                for (int x = 1; x < Width - 1; x++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), GetTile(BackgroundCode(x, y), UnityEngine.Tilemaps.Tile.ColliderType.None));
            }
            tilemap.CompressBounds();
        }

        static string BackgroundCode(int x, int y)
        {
            bool left = x == 1, right = x == Width - 2;
            if (y == 2)
            {
                foreach (var (px, code) in LowerWallProps)
                    if (px == x) return Corridor(code);
                return Corridor("4i1a");
            }
            if (y == 3)
            {
                if (left) return Corridor("3a1a");
                if (right) return Corridor("3a3a");
                foreach (var (px, code) in UpperWallProps)
                    if (px == x) return Corridor(code);
                return Corridor(new[] { "3a2a", "3a2b", "3a2c" }[x % 3]);
            }

            // 샤프트: 천장 바로 아래는 7행(위쪽 그림자), 그 아래로 8행·9행을 번갈아 쌓는다
            char row = y == TopY - 2 ? '7' : (y % 2 == 0 ? '8' : '9');
            if (left) return Shaft($"{row}a1a");
            if (right) return Shaft($"{row}a3a");
            if (row == '9') return Shaft("9a2a");
            return Shaft(x % 2 == 0 ? $"{row}a2a" : $"{row}a2b");
        }

        static void PaintSolid(Tilemap tilemap)
        {
            var grid = UnityEngine.Tilemaps.Tile.ColliderType.Grid;
            int stairIndex = 0;

            for (int row = 0; row < Height; row++)
            {
                int y = TopY - row;
                for (int x = 0; x < Width; x++)
                {
                    char c = Layout[row][x];
                    var cell = new Vector3Int(x, y, 0);
                    if (c == '#')
                        tilemap.SetTile(cell, GetTile(SolidCode(x, y), grid));
                    else if (c == 'S')
                        // 계단은 그림 모양(계단꼴)대로 충돌한다
                        tilemap.SetTile(cell, GetTile($"Stair/Stair_{StairSequence[stairIndex++ % StairSequence.Length]}", UnityEngine.Tilemaps.Tile.ColliderType.Sprite));
                }
            }
            tilemap.CompressBounds();
        }

        static string SolidCode(int x, int y)
        {
            int last = Width - 1;
            switch (y)
            {
                case TopY:
                    return Corridor(x == 0 ? "1a1a" : x == last ? "1a3a" : x % 2 == 0 ? "1a2a" : "1a2b");
                case TopY - 1:
                    return Corridor(x == 0 ? "2a1a" : x == last ? "2a3a" : "2a2a");
                case 1:
                    return Corridor(FloorCode(x));
                case 0:
                    return Corridor(x == 0 ? "6a1a" : x == last ? "6a3a" : "6a2a");
            }
            // 좌우 벽: 바닥판 끝 타일을 세워 쌓는다 (테두리가 방 안쪽을 향하게)
            return Corridor(x == 0 ? "5a3a" : "5a1a");
        }

        // 바닥면은 a 묶음(0~9) · b 묶음(10~17) · a 묶음(18~29)으로 나눠 끝 타일이 이어지게 한다
        static string FloorCode(int x)
        {
            if (x >= 10 && x <= 17)
                return x == 10 ? "5b1a" : x == 17 ? "5b3a" : x % 2 == 0 ? "5b2a" : "5b2b";

            int start = x < 10 ? 0 : 18, end = x < 10 ? 9 : Width - 1;
            if (x == start) return "5a1a";
            if (x == end) return "5a3a";
            return new[] { "5a2a", "5a2b", "5a2c", "5a2d" }[(x - start) % 4];
        }

        static string Corridor(string code) => $"Corridor/Corridor_{code}";
        static string Shaft(string code) => $"Shaft/Shaft_{code}";

        // Tiles/<폴더>/<이름>.png 스프라이트로 타일 에셋을 만들거나 갱신한다
        static TileBase GetTile(string spritePath, Tile.ColliderType colliderType)
        {
            string name = spritePath[(spritePath.LastIndexOf('/') + 1)..];
            string assetPath = $"{TileAssetFolder}/{name}_{colliderType}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TileArtFolder}{spritePath}.png");
            if (sprite == null)
                Debug.LogError($"[TestArenaBuilder] 타일 그림이 없습니다: {TileArtFolder}{spritePath}.png (python Tools/ArtPipeline/convert_marsh.py 실행 필요)");

            tile.sprite = sprite;
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ───────── 얇은 발판 ─────────

        static void BuildWalkways()
        {
            var root = new GameObject("Walkways").transform;
            for (int row = 0; row < Height; row++)
            {
                int x = 0;
                while (x < Width)
                {
                    if (Layout[row][x] != '=') { x++; continue; }
                    int start = x;
                    while (x < Width && Layout[row][x] == '=') x++;
                    BuildWalkway(root, start, x - 1, TopY - row + 1, row);
                }
            }
        }

        // 32px 조각을 이어 붙인 발판 하나. 위에서만 밟히고 아래·옆에서는 통과한다
        static void BuildWalkway(Transform root, int startX, int endX, int top, int row)
        {
            int cells = endX - startX + 1;
            var go = new GameObject($"Walkway_{startX}_{top}") { layer = RequireLayer("OneWay") };
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(startX, top, 0f);

            // 벽에 붙은 끝은 받침대(1d1a), 허공 쪽 끝은 마감 조각
            bool leftWall = startX > 0 && Layout[row][startX - 1] == '#';
            bool rightWall = endX < Width - 1 && Layout[row][endX + 1] == '#';
            int pieces = cells * 2;
            for (int i = 0; i < pieces; i++)
            {
                string code = "1a2a";
                bool flip = false;
                if (i == 0) code = leftWall ? "1d1a" : "1a1a";
                else if (i == pieces - 1) { code = rightWall ? "1d1a" : "1c2a"; flip = rightWall; }

                var piece = new GameObject($"Piece_{i}");
                piece.transform.SetParent(go.transform, false);
                // 피벗이 아래 중앙이고 그림 높이가 0.5타일이므로, 그림 윗면이 발판 윗면에 오도록 0.5 내린다
                piece.transform.localPosition = new Vector3(0.25f + i * 0.5f, -0.5f, 0f);
                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropsFolder}Platform_{code}.png");
                renderer.flipX = flip;
                renderer.sortingLayerName = "Props";
            }

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(cells, WalkwayThickness);
            box.offset = new Vector2(cells * 0.5f, -WalkwayThickness * 0.5f);
            box.usedByEffector = true;
            var effector = go.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 180f;
        }

        // ───────── 플레이어 ─────────

        static PlayerMovement BuildPlayer()
        {
            var player = new GameObject("Player") { tag = "Player", layer = RequireLayer("Player") };
            player.transform.position = FindMarker('P') + new Vector2(0f, PlayerSize.y * 0.5f);

            var body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            // 벽에 밀착한 채 공중에서 걸리지 않도록 마찰 0
            body.sharedMaterial = LoadOrCreateNoFriction();
            player.AddComponent<BoxCollider2D>().size = PlayerSize;

            player.AddComponent<PlayerInputReader>();
            var movement = player.AddComponent<PlayerMovement>();
            Configure(movement, s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadMovementSettings();
                s.FindProperty("groundLayers").intValue = LayerMask.GetMask("Ground", "OneWay");
            });

            SpriteRenderer visual = CreateChildSprite(player, "Visual", "Characters");
            SpriteRenderer charge = CreateChildSprite(player, "ChargeEffect", "Projectiles");
            charge.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            charge.enabled = false;

            Configure(player.AddComponent<PlayerShooter>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadCombatSettings();
                s.FindProperty("hitLayers").intValue = LayerMask.GetMask("Ground", "Enemy");
                s.FindProperty("beamSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
                s.FindProperty("chargeEffect").objectReferenceValue = charge;
            });

            Configure(player.AddComponent<PlayerAnimator>(), s =>
            {
                s.FindProperty("spriteRenderer").objectReferenceValue = visual;
                // 점프 전용 그림이 없어 달리기 시트를 임시로 쓴다 (GDD 3.3: 점프는 run_3 고정 — N1)
                SetClip(s, "idle", "Marine_Idle", 4f);
                SetClip(s, "run", "Marine_Run", 12f);
                SetClip(s, "jump", "Marine_Run", 0f);
            });

            player.AddComponent<Health>();
            Configure(player.AddComponent<PlayerHurt>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadHurtSettings();
                s.FindProperty("blinkTarget").objectReferenceValue = visual;
                s.FindProperty("hazardLayers").intValue = LayerMask.GetMask("EnemyProjectile");
            });

            return movement;
        }

        static void SetClip(SerializedObject animator, string field, string sheetName, float framesPerSecond)
        {
            string path = $"{MarineFolder}{sheetName}.png";
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (sheet == null)
                Debug.LogWarning($"[TestArenaBuilder] 캐릭터 시트가 없습니다: {path}");

            // 해병 시트는 칸 64x64, 발 중앙 x = 32
            SerializedProperty clip = animator.FindProperty(field);
            clip.FindPropertyRelative("sheet").objectReferenceValue = sheet;
            clip.FindPropertyRelative("cellWidth").intValue = 64;
            clip.FindPropertyRelative("pivotX").intValue = 32;
            clip.FindPropertyRelative("framesPerSecond").floatValue = framesPerSecond;
        }

        // ───────── 임시 적 ─────────

        static void BuildTestEnemies()
        {
            var root = new GameObject("TestEnemies").transform;
            var enemy = new GameObject("EnemyPatrol") { layer = RequireLayer("Enemy") };
            enemy.transform.SetParent(root, false);
            enemy.transform.position = FindMarker('E') + new Vector2(0f, EnemySize.y * 0.5f);

            var body = enemy.AddComponent<Rigidbody2D>();
            // 플레이어가 밀어도 잘 밀리지 않도록 무겁게, 벽에 붙어 멈추지 않도록 마찰 0
            body.mass = 20f;
            body.gravityScale = 3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sharedMaterial = LoadOrCreateNoFriction();
            enemy.AddComponent<BoxCollider2D>().size = EnemySize;

            SpriteRenderer sprite = CreateChildSprite(enemy, "Visual", "Characters");
            sprite.transform.localScale = new Vector3(EnemySize.x, EnemySize.y, 1f);
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
            sprite.color = EnemyColor;

            enemy.AddComponent<Health>();
            var settings = LoadEnemyPatrolSettings();
            Configure(enemy.AddComponent<Enemy>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                s.FindProperty("spriteRenderer").objectReferenceValue = sprite;
                s.FindProperty("contactTargets").intValue = LayerMask.GetMask("Player");
            });
            Configure(enemy.AddComponent<EnemyPatrol>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                s.FindProperty("groundLayers").intValue = LayerMask.GetMask("Ground", "OneWay");
                s.FindProperty("startDirection").intValue = -1;
                s.FindProperty("spriteRenderer").objectReferenceValue = sprite;
            });
        }

        // ───────── 카메라 ─────────

        static void SetupCamera(PlayerMovement player)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            PixelProjectSetup.ApplyPixelPerfectCamera(cam);

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            Configure(follow, s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadCameraSettings();
                s.FindProperty("target").objectReferenceValue = player;
                s.FindProperty("bounds").rectValue = new Rect(0f, 0f, Width, Height);
            });
        }

        // ───────── 조명 ─────────

        // URP 2D에서는 스프라이트가 2D 조명을 받아야 보인다.
        // Global Light 2D는 만들어질 때 있던 소팅 레이어만 비추므로, 나중에 추가한 레이어(Background, Tiles 등)까지 모두 비추게 한다
        static void SetupGlobalLight()
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                .Where(l => l.lightType == Light2D.LightType.Global)
                .ToList();
            if (lights.Count == 0)
            {
                var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                lights.Add(light);
            }

            int[] allLayers = SortingLayer.layers.Select(l => l.id).ToArray();
            foreach (Light2D light in lights)
            {
                Configure(light, s =>
                {
                    SerializedProperty targets = s.FindProperty("m_ApplyToSortingLayers");
                    targets.arraySize = allLayers.Length;
                    for (int i = 0; i < allLayers.Length; i++)
                        targets.GetArrayElementAtIndex(i).intValue = allLayers[i];
                });
            }
        }

        // ───────── 수치 SO (Data/V2, 없을 때만 새 크기 기준 임시값으로 생성) ─────────

        static PlayerMovementSettings LoadMovementSettings() => LoadOrCreate<PlayerMovementSettings>("PlayerMovementSettings", s =>
        {
            s.FindProperty("maxSpeed").floatValue = 4.5f;
            s.FindProperty("groundAcceleration").floatValue = 60f;
            s.FindProperty("groundDeceleration").floatValue = 60f;
            s.FindProperty("airAcceleration").floatValue = 25f;
            s.FindProperty("airDeceleration").floatValue = 15f;
            s.FindProperty("jumpHeight").floatValue = 2.2f;
            s.FindProperty("timeToApex").floatValue = 0.35f;
            s.FindProperty("maxFallSpeed").floatValue = 12f;
        });

        static PlayerCombatSettings LoadCombatSettings() => LoadOrCreate<PlayerCombatSettings>("PlayerCombatSettings", s =>
        {
            s.FindProperty("beamSpeed").floatValue = 14f;
            s.FindProperty("beamRange").floatValue = 9f;
            s.FindProperty("beamRadius").floatValue = 0.06f;
            s.FindProperty("chargeBeamSpeed").floatValue = 16f;
            s.FindProperty("chargeBeamRange").floatValue = 10f;
            s.FindProperty("chargeBeamRadius").floatValue = 0.15f;
            s.FindProperty("shoulderDepth").floatValue = 0.22f;
            s.FindProperty("muzzleDistance").floatValue = 0.3f;
            s.FindProperty("beamLength").floatValue = 0.44f;
            s.FindProperty("beamThickness").floatValue = 0.08f;
            s.FindProperty("chargeBeamLength").floatValue = 0.88f;
            s.FindProperty("chargeBeamThickness").floatValue = 0.16f;
        });

        static PlayerHurtSettings LoadHurtSettings() => LoadOrCreate<PlayerHurtSettings>("PlayerHurtSettings", s =>
        {
            s.FindProperty("maxEnergy").intValue = 100;
            s.FindProperty("knockbackVelocity").vector2Value = new Vector2(3f, 3.5f);
        });

        static CameraSettings LoadCameraSettings() => LoadOrCreate<CameraSettings>("CameraSettings", s =>
        {
            s.FindProperty("lookAheadDistance").floatValue = 1.5f;
            s.FindProperty("verticalDeadZone").floatValue = 1f;
            s.FindProperty("verticalOffset").floatValue = 0.5f;
        });

        static EnemyPatrolSettings LoadEnemyPatrolSettings() => LoadOrCreate<EnemyPatrolSettings>("EnemyPatrolSettings", s =>
        {
            s.FindProperty("moveSpeed").floatValue = 1.5f;
        });

        static T LoadOrCreate<T>(string fileName, Action<SerializedObject> defaults) where T : ScriptableObject
        {
            string path = $"{DataFolder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var serialized = new SerializedObject(asset);
            defaults(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[TestArenaBuilder] 수치 에셋 생성 (임시값): {path}");
            return asset;
        }

        // ───────── 공용 ─────────

        static void Configure(Object target, Action<SerializedObject> apply)
        {
            // private [SerializeField] 필드는 SerializedObject로 설정한다
            var serialized = new SerializedObject(target);
            apply(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SpriteRenderer CreateChildSprite(GameObject parent, string name, string sortingLayer)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = sortingLayer;
            return renderer;
        }

        // 표식 칸의 바닥 중앙 (월드 좌표)
        static Vector2 FindMarker(char marker)
        {
            for (int row = 0; row < Height; row++)
            {
                int x = Layout[row].IndexOf(marker);
                if (x >= 0) return new Vector2(x + 0.5f, TopY - row);
            }
            Debug.LogError($"[TestArenaBuilder] Layout에 '{marker}' 표식이 없습니다");
            return Vector2.zero;
        }

        static int RequireLayer(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer >= 0) return layer;
            Debug.LogError($"[TestArenaBuilder] '{name}' 레이어가 없습니다. Edit > Project Settings > Tags and Layers를 확인하세요.");
            return 0;
        }

        static PhysicsMaterial2D LoadOrCreateNoFriction()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (material != null) return material;

            material = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(material, NoFrictionPath);
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path[..slash]);
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static bool AssetExists(string path) =>
            !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
    }
}
