using System;
using System.IO;
using System.Linq;
using Platformer.Enemies;
using Platformer.Gameplay;
using Platformer.Player;
using Platformer.UI;
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
    /// marsh-sci-fi 타일로 소탕 방(30x12타일 2층) 하나와 좌우 통로·문을 칠하고, 얇은 발판(OneWay)·계단·플레이어·픽셀 카메라를 배치한다.
    /// 적은 프리팹(Prefabs/Enemies)으로 저장하고, 방(Room)이 웨이브 데이터대로 생성한다 (GDD 5.2).
    /// 씬 안의 생성 오브젝트(Grid, Walkways, Player, Room)는 실행할 때마다 지우고 다시 만든다.
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
        const string EnemyArtFolder = "Assets/_Project/Art/Enemies/";
        const string HealthShardSpritePath = "Assets/_Project/Art/FX/Health_Shard.png";
        const string ExplosionSheetPath = "Assets/_Project/Art/FX/Explosion.png";
        const string UIArtFolder = "Assets/_Project/Art/UI/";
        const string PlaceholderSpritePath = "Assets/_Project/Art/Placeholder/Placeholder_Tile.png";
        const string NoFrictionPath = "Assets/_Project/Data/NoFriction.physicsMaterial2D";
        const string DataFolder = "Assets/_Project/Data/V2";
        const string EnemyPrefabFolder = "Assets/_Project/Prefabs/Enemies";

        static readonly Color BackgroundColor = new Color32(0x0A, 0x08, 0x14, 0xFF);

        // 해병 키 약 40px, 몸통 폭 약 20px (GDD v2 8장)
        static readonly Vector2 PlayerSize = new(0.3125f, 0.625f);
        // 러너 충돌체: 웅크린 몸통 크기 (외계 생물 그림 몸통 약 60 x 45px)
        static readonly Vector2 RunnerSize = new(0.9f, 0.7f);
        // 브루트: 러너 그림을 1.5배로 키운다. 2배는 대시로 통과하기 너무 커서 사용자 요청으로 줄임
        // (정수배가 아니라 픽셀이 고르지 않게 보일 수 있다. GDD 4.6 예외)
        const float BruteScale = 1.5f;
        static readonly Vector2 BruteSize = new(1.35f, 1.05f);

        // 얇은 발판 그림(32x32)에서 위쪽 13px만 판이다
        const float WalkwayThickness = 13f / 64f;

        // 42x12타일: 왼쪽 통로(0~5) + 방(6~35, 벽 포함 30칸) + 오른쪽 통로(36~41).
        // 맨 윗줄이 y = 11, 맨 아랫줄이 y = 0, 왼쪽 끝이 x = 0
        // '#' = 벽·바닥·천장 (위치에 따라 그림을 고른다), '.' = 빈칸 (배경만)
        // '=' = 얇은 발판. 그 칸의 윗면이 발판 윗면이다 (y = 3 줄이면 윗면 y = 4)
        // 'S' = 계단 블록 8칸 한 묶음, 'P' = 플레이어 시작, 'D' = 방의 문 (세로로 이어진 칸이 문 하나)
        // '1'~'9' = 적 출현 지점 (웨이브 데이터의 출현 지점 번호 0~8)
        // 'C' = 파괴 상자 2개 (0.5타일 상자를 칸 바닥에 나란히), 'G' = 가스탱크
        // 바닥 윗면 y = 2, 발판 윗면 y = 4 / 6 / 8 (한 층 2타일, GDD 5.3). 통로 높이 2타일
        static readonly string[] Layout =
        {
            "##########################################",  // y = 11 천장 기둥
            "##########################################",  // y = 10 천장 환기구
            "#######............................#######",  // y = 9
            "#######............................#######",  // y = 8
            "#######..======....................#######",  // y = 7 발판 (윗면 8)
            "#######..............3..G..........#######",  // y = 6 가운데 발판 위 (출현 지점 3, 가스탱크)
            "#######.........==========.........#######",  // y = 5 발판 (윗면 6)
            "#######.........................4..#######",  // y = 4 오른쪽 낮은 발판 위 (출현 지점 4)
            "#.....D=========...........========D.....#",  // y = 3 문 위쪽, 발판 (윗면 4)
            "#.P...D...CC......SSSSSSSS.2.G.1...D.....#",  // y = 2 통로·문·바닥 (시작 위치, 상자, 계단, 출현 지점 2·1, 가스탱크)
            "##########################################",  // y = 1 바닥면
            "##########################################",  // y = 0 기초
        };
        const int TopY = 11;
        // 방 안쪽 끝 칸 (벽 바로 안쪽)
        const int RoomLeft = 7;
        const int RoomRight = 34;
        // 플레이어가 왼쪽 문에서 이만큼 안쪽으로 들어오면 방이 잠긴다 (문이 닫힐 때 끼지 않도록)
        const float ActivationInset = 2.5f;
        static int Width => Layout[0].Length;
        static int Height => Layout.Length;

        static readonly string[] StairSequence = { "1a1a", "1a2a", "1a2b", "1a3a", "1b1a", "1b2a", "1b2b", "1b3a" };

        // 벽 아래쪽(y = 2) 배경 특수 타일: x → 코드. 그 외는 민 벽(4i1a)
        static readonly (int x, string code)[] LowerWallProps =
        {
            (12, "4d1a"), (15, "4j1a"), (28, "4f1a"), (30, "4h1a"), (32, "4g1a"),
        };
        // 벽 위쪽(y = 3) 배경 장식 타일
        static readonly (int x, string code)[] UpperWallProps =
        {
            (14, "3e1a"), (20, "3b1a"), (23, "3c1a"), (26, "3d1a"),
        };

        [MenuItem("Tools/Platformer/Build Test Arena")]
        static void Build()
        {
            if (!ValidateLayout()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = OpenOrCreateScene();
            if (!scene.IsValid()) return;

            foreach (string rootName in new[] { "Grid", "Walkways", "Player", "TestEnemies", "Room", "HUD" })
            {
                GameObject old = GameObject.Find(rootName);
                if (old != null) Object.DestroyImmediate(old);
            }

            EnsureFolder(TileAssetFolder);
            EnsureFolder(DataFolder);
            EnsureFolder(EnemyPrefabFolder);

            var grid = new GameObject("Grid").AddComponent<Grid>();
            PaintBackground(CreateTilemap(grid, "BackWall", "Background", null));
            PaintSolid(CreateTilemap(grid, "Ground", "Tiles", "Ground"));
            BuildWalkways();
            PlayerMovement player = BuildPlayer();
            Room room = BuildRoom();
            SetupCamera(player);
            SetupGlobalLight();
            BuildHud(player.gameObject, room);

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
            bool left = x == RoomLeft, right = x == RoomRight;
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
                    {
                        tilemap.SetTile(cell, GetTile(SolidCode(x, y), grid));
                    }
                    else if (c == 'S')
                    {
                        // 계단 타일은 그림만 그리고, 충돌은 픽셀 단위 상자로 따로 만든다
                        string sprite = $"Stair/Stair_{StairSequence[stairIndex++ % StairSequence.Length]}";
                        tilemap.SetTile(cell, GetTile(sprite, UnityEngine.Tilemaps.Tile.ColliderType.None));
                        AddStairColliders(tilemap.gameObject, cell, sprite);
                    }
                }
            }
            tilemap.CompressBounds();
        }

        // 자동 생성 물리 모양은 꼭짓점을 줄이느라 8px 계단 모서리를 비스듬하게 뭉갤 수 있다 (턱 넘기가 걸리는 원인).
        // 그림의 열마다 불투명한 높이를 재서, 높이가 같은 열끼리 상자 하나로 묶는다.
        // 상자는 지형 타일맵의 자식이라 CompositeCollider2D가 지형과 하나로 합친다
        static void AddStairColliders(GameObject ground, Vector3Int cell, string spritePath)
        {
            string path = $"{TileArtFolder}{spritePath}.png";
            // 임포트 설정(Read/Write 꺼짐)과 상관없이 픽셀을 읽기 위해 PNG 파일을 직접 연다
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Debug.LogError($"[TestArenaBuilder] 계단 그림을 읽을 수 없습니다: {path}");
                Object.DestroyImmediate(texture);
                return;
            }

            int size = texture.width;
            var heights = new int[size];
            for (int x = 0; x < size; x++)
            {
                for (int y = texture.height - 1; y >= 0; y--)
                {
                    if (texture.GetPixel(x, y).a <= 0f) continue;
                    heights[x] = y + 1;
                    break;
                }
            }
            Object.DestroyImmediate(texture);

            int layer = ground.layer;
            int start = 0;
            for (int x = 1; x <= size; x++)
            {
                if (x < size && heights[x] == heights[start]) continue;

                int h = heights[start];
                if (h > 0)
                {
                    var step = new GameObject($"Step_{cell.x}_{start}") { layer = layer };
                    step.transform.SetParent(ground.transform, false);
                    var box = step.AddComponent<BoxCollider2D>();
                    box.compositeOperation = Collider2D.CompositeOperation.Merge;
                    box.size = new Vector2((x - start) / (float)size, h / (float)size);
                    box.offset = new Vector2(cell.x + (start + x) * 0.5f / size, cell.y + h * 0.5f / size);
                }
                start = x;
            }
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
            // 좌우 벽과 통로 위 막힌 곳: 바닥판 끝 타일을 세워 쌓는다 (테두리가 방 안쪽을 향하게)
            return Corridor(x < RoomLeft ? "5a3a" : "5a1a");
        }

        // 바닥면은 10칸씩 a 묶음과 b 묶음을 번갈아 놓아 끝 타일이 이어지게 한다
        static string FloorCode(int x)
        {
            int segment = x / 10;
            int start = segment * 10, end = Mathf.Min(start + 9, Width - 1);
            if (segment % 2 == 1)
                return x == start ? "5b1a" : x == end ? "5b3a" : x % 2 == 0 ? "5b2a" : "5b2b";

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

            var heat = player.AddComponent<PlayerHeat>();
            Configure(heat, s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadCombatSettings();
                s.FindProperty("shockwaveLayers").intValue = LayerMask.GetMask("Enemy");
            });

            var shooter = player.AddComponent<PlayerShooter>();
            Configure(shooter, s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadCombatSettings();
                s.FindProperty("loadout").objectReferenceValue = LoadWeaponLoadout();
                s.FindProperty("hitLayers").intValue = LayerMask.GetMask("Ground", "Enemy");
                s.FindProperty("beamSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
                s.FindProperty("chargeEffect").objectReferenceValue = charge;
            });

            Configure(player.AddComponent<PlayerAnimator>(), s =>
            {
                s.FindProperty("spriteRenderer").objectReferenceValue = visual;
                // 점프 전용 그림이 없어 달리기 시트의 한 프레임(run_3)을 고정해서 쓴다 (GDD 3.3, PlayerAnimator.jumpFrame)
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

            Configure(player.AddComponent<PlayerDash>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadDashSettings();
                s.FindProperty("visual").objectReferenceValue = visual;
                s.FindProperty("passThroughLayers").intValue = LayerMask.GetMask("Enemy");
            });

            Configure(player.AddComponent<PlayerCounter>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = LoadOrCreate<PlayerCounterSettings>("PlayerCounterSettings", _ => { });
                s.FindProperty("enemyLayers").intValue = LayerMask.GetMask("Enemy");
                s.FindProperty("shardSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(HealthShardSpritePath);
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

        // ───────── 적 ─────────

        // 적 프리팹 3종을 만들어 저장한다 (다시 만들어도 같은 파일을 덮어써 GUID가 유지된다).
        // 러너와 브루트는 같은 행동(EnemyRunner)에 설정·그림·크기만 다르다
        static (GameObject runner, GameObject brute, GameObject infected) BuildEnemyPrefabs()
        {
            GameObject runner = SaveEnemyPrefab(CreateRunner(LoadRunnerSettings(), "Runner", RunnerSize, 1f));
            GameObject brute = SaveEnemyPrefab(CreateRunner(LoadBruteSettings(), "Brute", BruteSize, BruteScale));
            var shooterSettings = LoadOrCreate<EnemyShooterSettings>("EnemyInfectedSettings", s =>
            {
                s.FindProperty("maxHealth").intValue = 40;
                s.FindProperty("contactDamage").intValue = 5;
            });
            GameObject infected = SaveEnemyPrefab(CreateShooter(shooterSettings));
            return (runner, brute, infected);
        }

        static GameObject SaveEnemyPrefab(GameObject instance)
        {
            string path = $"{EnemyPrefabFolder}/{instance.name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // ───────── 방 ─────────

        // 소탕 방: 문('D'), 출현 지점('1'~'9'), 시작 영역, 웨이브 데이터를 묶는다
        static Room BuildRoom()
        {
            var prefabs = BuildEnemyPrefabs();
            var roomObject = new GameObject("Room");
            var room = roomObject.AddComponent<Room>();

            RoomDoor[] doors = BuildDoors(roomObject.transform);
            Destructible[] props = BuildProps(roomObject.transform);

            var points = new System.Collections.Generic.List<Transform>();
            for (char marker = '1'; marker <= '9'; marker++)
            {
                foreach (Vector2 feet in FindMarkers(marker))
                {
                    var point = new GameObject($"SpawnPoint_{marker}").transform;
                    point.SetParent(roomObject.transform, false);
                    point.position = feet;
                    points.Add(point);
                }
            }

            // 부활 위치: 왼쪽 출입구 바깥 통로 (발 위치)
            var respawn = new GameObject("RespawnPoint").transform;
            respawn.SetParent(roomObject.transform, false);
            respawn.position = new Vector2(RoomLeft - 2.5f, 2f);

            float roomLeftEdge = RoomLeft;
            var activation = new Rect(roomLeftEdge + ActivationInset, 2f, RoomRight + 1 - roomLeftEdge - ActivationInset, TopY - 3);

            Configure(room, s =>
            {
                s.FindProperty("waveData").objectReferenceValue = LoadWaveData(prefabs.runner, prefabs.brute, prefabs.infected);
                SetArray(s.FindProperty("doors"), doors);
                SetArray(s.FindProperty("spawnPoints"), points.ToArray());
                s.FindProperty("activationArea").rectValue = activation;
                s.FindProperty("respawnPoint").objectReferenceValue = respawn;
                SetArray(s.FindProperty("props"), props);
            });
            return room;
        }

        // ───────── 환경 요소 (GDD 4.7) ─────────

        static readonly string[] CrateSprites = { "1a1a", "2a1a", "1e1a", "1b1a" };
        static readonly Vector2 CrateSize = new(0.5f, 0.5f);
        // 가스탱크 그림 몸통 약 38 x 44px
        static readonly Vector2 GasTankSize = new(0.6f, 0.65f);

        // 'C' 칸마다 상자 2개, 'G' 칸마다 가스탱크 하나
        static Destructible[] BuildProps(Transform parent)
        {
            var props = new System.Collections.Generic.List<Destructible>();
            EnsureFolder($"{DataFolder}/Props");

            var crateSettings = LoadOrCreate<DestructibleSettings>("Props/Crate", s =>
            {
                s.FindProperty("maxHealth").intValue = 30;
                s.FindProperty("explodes").boolValue = false;
            });
            int crateIndex = 0;
            foreach (Vector2 feet in FindMarkers('C'))
            {
                for (int i = 0; i < 2; i++)
                {
                    var crateFeet = new Vector2(feet.x - 0.25f + i * 0.5f, feet.y);
                    string sprite = $"Box_{CrateSprites[crateIndex++ % CrateSprites.Length]}";
                    // 상자는 지형 레이어: 플레이어·적·탄을 막는 엄폐물이고 위에 올라설 수 있다
                    props.Add(CreateProp(parent, "Crate", crateFeet, CrateSize, sprite, "Ground", crateSettings));
                }
            }

            var tankSettings = LoadOrCreate<DestructibleSettings>("Props/GasTank", s =>
            {
                s.FindProperty("maxHealth").intValue = 10;
                s.FindProperty("explodes").boolValue = true;
            });
            foreach (Vector2 feet in FindMarkers('G'))
            {
                // 가스탱크는 적 레이어: 플레이어 탄에만 맞고 적 탄에는 맞지 않는다
                Destructible tank = CreateProp(parent, "GasTank", feet, GasTankSize, "GasTank", "Enemy", tankSettings);
                Configure(tank, s =>
                {
                    SerializedProperty clip = s.FindProperty("explosionEffect");
                    clip.FindPropertyRelative("sheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(ExplosionSheetPath);
                    // 폭발 시트: 칸 57x84 (착탄 이펙트 3배), 4프레임
                    clip.FindPropertyRelative("cellWidth").intValue = 57;
                    clip.FindPropertyRelative("pivotX").intValue = 28;
                    clip.FindPropertyRelative("framesPerSecond").floatValue = 14f;
                });
                props.Add(tank);
            }
            return props.ToArray();
        }

        static Destructible CreateProp(Transform parent, string propName, Vector2 feet, Vector2 size, string spriteName,
            string layer, DestructibleSettings settings)
        {
            var go = new GameObject(propName) { layer = RequireLayer(layer) };
            go.transform.SetParent(parent, false);
            go.transform.position = feet + new Vector2(0f, size.y * 0.5f);
            go.AddComponent<BoxCollider2D>().size = size;

            // 소품 그림은 피벗이 아래 중앙이다
            SpriteRenderer sprite = CreateChildSprite(go, "Visual", "Props");
            sprite.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            sprite.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropsFolder}{spriteName}.png");

            go.AddComponent<Health>();
            var destructible = go.AddComponent<Destructible>();
            Configure(destructible, s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                s.FindProperty("explosionLayers").intValue = LayerMask.GetMask("Player", "Enemy", "Ground");
                s.FindProperty("playerLayers").intValue = LayerMask.GetMask("Player");
            });
            return destructible;
        }

        // 세로로 이어진 'D' 칸마다 차단 블록 하나: 지형 레이어 충돌체 + 닫혔을 때 보이는 벽 블록 그림.
        // 문 그림은 닫혀 있는 것처럼 보이지 않아, 방 벽과 같은 블록으로 출입구를 메운다 (2026-10-27 사용자 요청)
        static RoomDoor[] BuildDoors(Transform parent)
        {
            var doors = new System.Collections.Generic.List<RoomDoor>();
            for (int x = 0; x < Width; x++)
            {
                int row = 0;
                while (row < Height)
                {
                    if (Layout[row][x] != 'D') { row++; continue; }
                    int topRow = row;
                    while (row < Height && Layout[row][x] == 'D') row++;
                    int bottomY = TopY - (row - 1), topY = TopY - topRow;
                    doors.Add(BuildDoor(parent, x, bottomY, topY));
                }
            }
            return doors.ToArray();
        }

        static RoomDoor BuildDoor(Transform parent, int x, int bottomY, int topY)
        {
            int height = topY - bottomY + 1;
            var go = new GameObject($"Door_{x}") { layer = RequireLayer("Ground") };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x + 0.5f, bottomY + height * 0.5f, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, height);

            var sprites = new SpriteRenderer[height];
            for (int i = 0; i < height; i++)
            {
                var cell = new GameObject($"Cell_{i}");
                cell.transform.SetParent(go.transform, false);
                cell.transform.localPosition = new Vector3(0f, -height * 0.5f + i + 0.5f, 0f);
                var renderer = cell.AddComponent<SpriteRenderer>();
                // 바로 위·아래 벽과 같은 타일 (SolidCode의 좌우 벽 규칙)
                string code = x < RoomLeft ? "5a3a" : "5a1a";
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{TileArtFolder}{Corridor(code)}.png");
                renderer.sortingLayerName = "Tiles";
                sprites[i] = renderer;
            }

            var door = go.AddComponent<RoomDoor>();
            Configure(door, s =>
            {
                SetArray(s.FindProperty("closedSprites"), sprites);
                s.FindProperty("startOpen").boolValue = true;
            });
            return door;
        }

        // 테스트 방 웨이브 (없을 때만 만든다. 이후 Inspector에서 고친 내용은 유지)
        // 출현 지점: 0 = 바닥 오른쪽, 1 = 바닥 계단 옆, 2 = 가운데 발판, 3 = 오른쪽 낮은 발판
        static RoomWaveData LoadWaveData(GameObject runner, GameObject brute, GameObject infected)
        {
            EnsureFolder($"{DataFolder}/Rooms");
            return LoadOrCreate<RoomWaveData>("Rooms/TestArena_Waves", s =>
            {
                var waves = new[]
                {
                    (advance: 0, spawns: new[] { (runner, 0, 0f), (runner, 2, 0.5f) }),
                    (advance: 1, spawns: new[] { (infected, 3, 0f), (runner, 1, 0.3f), (runner, 0, 1f) }),
                    (advance: 0, spawns: new[] { (brute, 1, 0f), (runner, 0, 0.8f), (runner, 2, 1.6f) }),
                };

                SerializedProperty waveArray = s.FindProperty("waves");
                waveArray.arraySize = waves.Length;
                for (int i = 0; i < waves.Length; i++)
                {
                    SerializedProperty wave = waveArray.GetArrayElementAtIndex(i);
                    wave.FindPropertyRelative("advanceWhenRemaining").intValue = waves[i].advance;
                    SerializedProperty spawnArray = wave.FindPropertyRelative("spawns");
                    spawnArray.arraySize = waves[i].spawns.Length;
                    for (int j = 0; j < waves[i].spawns.Length; j++)
                    {
                        var (prefab, point, delay) = waves[i].spawns[j];
                        SerializedProperty spawn = spawnArray.GetArrayElementAtIndex(j);
                        spawn.FindPropertyRelative("enemyPrefab").objectReferenceValue = prefab;
                        spawn.FindPropertyRelative("spawnPoint").intValue = point;
                        spawn.FindPropertyRelative("delay").floatValue = delay;
                    }
                }
            });
        }

        static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        // E5 감염 병사: 해병과 같은 크기, 녹색 해병 그림. 저장용 인스턴스를 만들어 돌려준다
        static GameObject CreateShooter(EnemyShooterSettings settings)
        {
            var enemy = new GameObject("Infected") { layer = RequireLayer("Enemy") };

            var body = enemy.AddComponent<Rigidbody2D>();
            body.mass = 5f;
            body.gravityScale = 3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sharedMaterial = LoadOrCreateNoFriction();
            enemy.AddComponent<BoxCollider2D>().size = PlayerSize;

            SpriteRenderer sprite = CreateChildSprite(enemy, "Visual", "Characters");
            sprite.transform.localPosition = new Vector3(0f, -PlayerSize.y * 0.5f, 0f);
            SpriteRenderer overlay = CreateChildSprite(enemy, "Overlay", "Characters");
            overlay.transform.localPosition = sprite.transform.localPosition;
            overlay.sortingOrder = 1;
            overlay.enabled = false;

            enemy.AddComponent<Health>();
            Configure(enemy.AddComponent<Enemy>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                s.FindProperty("spriteRenderer").objectReferenceValue = null;
                s.FindProperty("contactTargets").intValue = LayerMask.GetMask("Player");
            });
            Configure(enemy.AddComponent<EnemyShooter>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                s.FindProperty("groundLayers").intValue = LayerMask.GetMask("Ground", "OneWay");
                s.FindProperty("bulletHitLayers").intValue = LayerMask.GetMask("Ground", "Player");
                s.FindProperty("bulletSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderSpritePath);
                s.FindProperty("spriteRenderer").objectReferenceValue = sprite;
                s.FindProperty("overlayRenderer").objectReferenceValue = overlay;
                // 해병 계열 시트는 칸 64x64, 발 중앙 x = 32
                SetEnemyClip(s, "idle", "Infected", "Idle", 4f, 64, 32);
                SetEnemyClip(s, "run", "Infected", "Run", 10f, 64, 32);
                SetEnemyClip(s, "shoot", "Infected", "Shoot", 0f, 64, 32);
                SetEnemyClip(s, "die", "Infected", "Die", 10f, 64, 32);
            });
            return enemy;
        }

        // 러너·브루트 저장용 인스턴스를 만들어 돌려준다
        static GameObject CreateRunner(EnemyRunnerSettings settings, string enemyName, Vector2 size, float visualScale)
        {
            var enemy = new GameObject(enemyName) { layer = RequireLayer("Enemy") };

            var body = enemy.AddComponent<Rigidbody2D>();
            // 플레이어가 밀어도 잘 밀리지 않도록 무겁게, 벽에 붙어 멈추지 않도록 마찰 0
            body.mass = 20f;
            body.gravityScale = 3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sharedMaterial = LoadOrCreateNoFriction();
            enemy.AddComponent<BoxCollider2D>().size = size;

            // 그림 피벗(발)을 충돌체 바닥에 맞춘다. 실루엣은 몸 그림 바로 위에 겹친다
            SpriteRenderer sprite = CreateChildSprite(enemy, "Visual", "Characters");
            sprite.transform.localPosition = new Vector3(0f, -size.y * 0.5f, 0f);
            sprite.transform.localScale = new Vector3(visualScale, visualScale, 1f);
            SpriteRenderer overlay = CreateChildSprite(enemy, "Overlay", "Characters");
            overlay.transform.localPosition = sprite.transform.localPosition;
            overlay.transform.localScale = sprite.transform.localScale;
            overlay.sortingOrder = 1;
            overlay.enabled = false;

            enemy.AddComponent<Health>();
            Configure(enemy.AddComponent<Enemy>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                // 몸 그림은 실루엣으로 번쩍이므로 색 번쩍임은 쓰지 않는다
                s.FindProperty("spriteRenderer").objectReferenceValue = null;
                s.FindProperty("contactTargets").intValue = LayerMask.GetMask("Player");
            });
            Configure(enemy.AddComponent<EnemyRunner>(), s =>
            {
                s.FindProperty("settings").objectReferenceValue = settings;
                s.FindProperty("groundLayers").intValue = LayerMask.GetMask("Ground", "OneWay");
                s.FindProperty("targetLayers").intValue = LayerMask.GetMask("Player");
                s.FindProperty("spriteRenderer").objectReferenceValue = sprite;
                s.FindProperty("overlayRenderer").objectReferenceValue = overlay;
                SetEnemyClip(s, "idle", enemyName, "Idle", 3f);
                SetEnemyClip(s, "run", enemyName, "Run", 12f);
                SetEnemyClip(s, "bite", enemyName, "Bite", 0f);
                SetEnemyClip(s, "die", enemyName, "Die", 10f);
            });
            return enemy;
        }

        // 외계 생물 계열 시트는 칸 128x64, 발 중앙 x = 64 (기본값)
        static void SetEnemyClip(SerializedObject component, string field, string enemyName, string clipName, float framesPerSecond,
            int cellWidth = 128, int pivotX = 64)
        {
            string basePath = $"{EnemyArtFolder}{enemyName}/{enemyName}_{clipName}";
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>($"{basePath}.png");
            if (sheet == null)
                Debug.LogWarning($"[TestArenaBuilder] 적 시트가 없습니다: {basePath}.png");

            SerializedProperty clip = component.FindProperty(field);
            clip.FindPropertyRelative("sheet").objectReferenceValue = sheet;
            clip.FindPropertyRelative("whiteSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>($"{basePath}_White.png");
            clip.FindPropertyRelative("cellWidth").intValue = cellWidth;
            clip.FindPropertyRelative("pivotX").intValue = pivotX;
            clip.FindPropertyRelative("framesPerSecond").floatValue = framesPerSecond;
        }

        // ───────── HUD (N5) ─────────

        // 화면 공간 캔버스(카메라 기준)에 HUD를 붙인다. 640x360 기준으로 정수배 확대되고, Pixel Perfect Camera의 검은 테두리 안쪽에 그려진다
        static void BuildHud(GameObject player, Room room)
        {
            var hud = new GameObject("HUD", typeof(RectTransform));
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;
            canvas.pixelPerfect = true;
            canvas.sortingLayerName = "UI";

            var scaler = hud.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Configure(hud.AddComponent<GameHud>(), s =>
            {
                s.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<Health>();
                s.FindProperty("heat").objectReferenceValue = player.GetComponent<PlayerHeat>();
                s.FindProperty("dash").objectReferenceValue = player.GetComponent<PlayerDash>();
                s.FindProperty("shooter").objectReferenceValue = player.GetComponent<PlayerShooter>();
                s.FindProperty("room").objectReferenceValue = room;
                s.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>($"{UIArtFolder}Fonts/Righteous-Regular.ttf");
                s.FindProperty("whiteSprite").objectReferenceValue = LoadUISprite("White");
                s.FindProperty("dotSprite").objectReferenceValue = LoadUISprite("Pip");
            });
        }

        static Sprite LoadUISprite(string name)
        {
            string path = $"{UIArtFolder}{name}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[TestArenaBuilder] UI 그림이 없습니다: {path} (python Tools/ArtPipeline/make_ui.py 실행 필요)");
            return sprite;
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
            s.FindProperty("timeToApex").floatValue = 0.45f;
            s.FindProperty("fallGravityMultiplier").floatValue = 1.5f;
            s.FindProperty("maxFallSpeed").floatValue = 12f;
        });

        static PlayerCombatSettings LoadCombatSettings() => LoadOrCreate<PlayerCombatSettings>("PlayerCombatSettings", s =>
        {
            s.FindProperty("chargeBeamSpeed").floatValue = 16f;
            s.FindProperty("chargeBeamRange").floatValue = 10f;
            s.FindProperty("chargeBeamRadius").floatValue = 0.15f;
            s.FindProperty("shoulderDepth").floatValue = 0.22f;
            s.FindProperty("muzzleDistance").floatValue = 0.3f;
            s.FindProperty("chargeBeamLength").floatValue = 0.88f;
            s.FindProperty("chargeBeamThickness").floatValue = 0.16f;
        });

        // 무기 모듈 3종과 장착 목록 (GDD 4.3). W1은 클래스 기본값 그대로다
        static WeaponLoadout LoadWeaponLoadout()
        {
            EnsureFolder($"{DataFolder}/Weapons");
            var rapid = LoadOrCreate<WeaponModule>("Weapons/W1_Rapid", _ => { });
            var pierce = LoadOrCreate<WeaponModule>("Weapons/W2_Pierce", s =>
            {
                s.FindProperty("displayName").stringValue = "PIERCE";
                s.FindProperty("color").colorValue = new Color(1f, 0.4f, 0.9f);
                s.FindProperty("damage").intValue = 25;
                s.FindProperty("fireCooldown").floatValue = 0.35f;
                s.FindProperty("heat").floatValue = 22f;
                s.FindProperty("pierceEnemies").boolValue = true;
                s.FindProperty("speed").floatValue = 18f;
                s.FindProperty("range").floatValue = 10f;
                s.FindProperty("radius").floatValue = 0.08f;
                s.FindProperty("length").floatValue = 0.88f;
                s.FindProperty("thickness").floatValue = 0.1f;
            });
            var spread = LoadOrCreate<WeaponModule>("Weapons/W4_Spread", s =>
            {
                s.FindProperty("displayName").stringValue = "SPREAD";
                s.FindProperty("color").colorValue = new Color(0.5f, 1f, 0.4f);
                s.FindProperty("damage").intValue = 7;
                s.FindProperty("fireCooldown").floatValue = 0.3f;
                s.FindProperty("heat").floatValue = 20f;
                s.FindProperty("projectileCount").intValue = 3;
                s.FindProperty("spreadAngle").floatValue = 24f;
                s.FindProperty("speed").floatValue = 12f;
                s.FindProperty("range").floatValue = 5f;
                s.FindProperty("length").floatValue = 0.3f;
            });

            return LoadOrCreate<WeaponLoadout>("Weapons/WeaponLoadout", s =>
            {
                SerializedProperty modules = s.FindProperty("modules");
                var all = new Object[] { rapid, pierce, spread };
                modules.arraySize = all.Length;
                for (int i = 0; i < all.Length; i++)
                    modules.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
            });
        }

        // 대시 수치는 클래스 기본값이 이미 새 크기 기준이다 (GDD 4.4)
        static PlayerDashSettings LoadDashSettings() => LoadOrCreate<PlayerDashSettings>("PlayerDashSettings", _ => { });

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

        // E2 브루트 (GDD 4.6): 느리고 단단하다. 정면 사격이 잘 안 먹혀 카운터가 주 공략법이다
        static EnemyRunnerSettings LoadBruteSettings() => LoadOrCreate<EnemyRunnerSettings>("EnemyBruteSettings", s =>
        {
            s.FindProperty("maxHealth").intValue = 120;
            s.FindProperty("contactDamage").intValue = 10;
            s.FindProperty("deathDuration").floatValue = 0.6f;
            s.FindProperty("knockbackScale").floatValue = 0.5f;
            s.FindProperty("frontDamageMultiplier").floatValue = 0.3f;
            s.FindProperty("chaseSpeed").floatValue = 1.8f;
            s.FindProperty("attackRange").floatValue = 0.6f;
            s.FindProperty("windupTime").floatValue = 0.8f;
            s.FindProperty("signalTime").floatValue = 0.3f;
            s.FindProperty("lungeSpeed").floatValue = 5f;
            s.FindProperty("lungeTime").floatValue = 0.2f;
            s.FindProperty("biteDamage").intValue = 25;
            s.FindProperty("biteKnockbackScale").floatValue = 2f;
            s.FindProperty("biteSize").vector2Value = new Vector2(0.75f, 0.75f);
            s.FindProperty("recoverTime").floatValue = 1f;
        });

        // 러너는 물기(카운터 대상)가 주 공격이므로 몸 접촉 피해는 낮게 둔다
        static EnemyRunnerSettings LoadRunnerSettings() => LoadOrCreate<EnemyRunnerSettings>("EnemyRunnerSettings", s =>
        {
            s.FindProperty("contactDamage").intValue = 5;
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

        // 표식 칸마다 바닥 중앙 (월드 좌표)
        static System.Collections.Generic.List<Vector2> FindMarkers(char marker)
        {
            var result = new System.Collections.Generic.List<Vector2>();
            for (int row = 0; row < Height; row++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Layout[row][x] == marker)
                        result.Add(new Vector2(x + 0.5f, TopY - row));
                }
            }
            return result;
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
            // 지워진 씬(이전 TestRoom 등)은 목록에서 뺀다
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && AssetExists(s.path)).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static bool AssetExists(string path) =>
            !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
    }
}
