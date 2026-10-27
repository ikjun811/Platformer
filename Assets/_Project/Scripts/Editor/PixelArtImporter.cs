using UnityEditor;
using UnityEngine;

namespace Platformer.EditorTools
{
    /// <summary>
    /// Assets/_Project/Art 아래 이미지에 임포트 설정을 자동 적용한다.
    /// - Characters/Marine, Enemies/, Tiles/, Props/, FX/: marsh-sci-fi 픽셀아트 (GDD v2 8장: PPU 64, Point, 무압축).
    ///   Tools/ArtPipeline/convert_marsh.py가 만드는 파일이라 임포트할 때마다 강제 적용한다.
    ///   캐릭터 시트의 프레임 분할은 런타임에 애니메이션 컴포넌트가 한다
    /// - UI/: Tools/ArtPipeline/make_ui.py가 만드는 HUD 그림. Point, 무압축, PPU 100 (그림 1픽셀 = 캔버스 1단위).
    ///   파일 이름이 _9sN 으로 끝나면 9-slice 테두리를 N 픽셀로 설정한다
    /// - 그 외 (Placeholder 등 임시 타일): PPU 16, Point. 최초 임포트 시에만 적용하므로 이후 인스펙터에서 바꾼 값은 유지된다
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const string ArtFolder = "Assets/_Project/Art/";
        const string PropsFolder = "Assets/_Project/Art/Props/";
        const string UIFolder = "Assets/_Project/Art/UI/";
        const int UIPixelsPerUnit = 100;
        const int PixelsPerUnit = 16;
        const int MarshPixelsPerUnit = 64;

        static readonly string[] MarshFolders =
        {
            "Assets/_Project/Art/Characters/Marine/",
            "Assets/_Project/Art/Enemies/",
            "Assets/_Project/Art/Tiles/",
            "Assets/_Project/Art/Props/",
            "Assets/_Project/Art/FX/",
        };

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;

            var importer = (TextureImporter)assetImporter;

            if (IsMarshAsset(assetPath))
            {
                ApplyMarshSettings(importer);
                return;
            }

            if (assetPath.StartsWith(UIFolder))
            {
                ApplyUISettings(importer);
                return;
            }

            // 재임포트 시에는 사용자가 직접 바꾼 설정을 덮어쓰지 않는다
            if (!assetImporter.importSettingsMissing) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
        }

        static bool IsMarshAsset(string path)
        {
            foreach (var folder in MarshFolders)
            {
                if (path.StartsWith(folder)) return true;
            }
            return false;
        }

        void ApplyUISettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = UIPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            // 이름 끝 _9sN → 9-slice 테두리 N 픽셀
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            int marker = fileName.LastIndexOf("_9s", System.StringComparison.Ordinal);
            int border = 0;
            if (marker >= 0)
                int.TryParse(fileName.Substring(marker + 3), out border);
            importer.spriteBorder = new Vector4(border, border, border, border);
        }

        void ApplyMarshSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = MarshPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // 시트를 런타임에 칸 단위로 자르려면 메시가 사각형 전체여야 한다
            settings.spriteMeshType = SpriteMeshType.FullRect;
            // 소품은 바닥에 세워 배치하므로 피벗을 아래 중앙에 둔다. 타일·이펙트는 기본값(중앙)
            settings.spriteAlignment = assetPath.StartsWith(PropsFolder)
                ? (int)SpriteAlignment.BottomCenter
                : (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }
    }
}
