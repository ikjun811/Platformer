using System.IO;
using UnityEditor;
using UnityEngine;

namespace Platformer.EditorTools
{
    /// <summary>
    /// Assets/_Project/Art 아래 이미지에 임포트 설정을 자동 적용한다.
    /// - Characters/Marine, Enemies/, Tiles/, Props/, FX/: marsh-sci-fi 픽셀아트 (GDD v2 8장: PPU 64, Point, 무압축).
    ///   Tools/ArtPipeline/convert_marsh.py가 만드는 파일이라 임포트할 때마다 강제 적용한다.
    ///   캐릭터 시트의 프레임 분할은 런타임에 애니메이션 컴포넌트가 한다
    /// - Characters/ 바로 아래 파일, Environment/: GDD v1의 고해상도 일러스트 (PPU 128, Bilinear). 새 아트로 교체할 때(N1) 삭제한다
    /// - 그 외 (Placeholder 등 임시 타일): PPU 16, Point. 최초 임포트 시에만 적용하므로 이후 인스펙터에서 바꾼 값은 유지된다
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const string ArtFolder = "Assets/_Project/Art/";
        const string CharacterFolder = "Assets/_Project/Art/Characters";
        const string EnvironmentFolder = "Assets/_Project/Art/Environment/";
        const string PropsFolder = "Assets/_Project/Art/Props/";
        const int PixelsPerUnit = 16;
        const int IllustrationPixelsPerUnit = 128;
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

            if (IsLegacyIllustration(assetPath))
            {
                ApplyIllustrationSettings(importer);
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

        // GDD v1 아트: Characters/ 바로 아래 파일(하위 폴더 제외)과 Environment/ 전체
        static bool IsLegacyIllustration(string path)
        {
            if (path.StartsWith(EnvironmentFolder)) return true;
            return Path.GetDirectoryName(path)?.Replace('\\', '/') == CharacterFolder;
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

        static void ApplyIllustrationSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = IllustrationPixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            // 달리기 시트는 가로 2304px이라 기본 최대 크기(2048)면 축소된다
            importer.maxTextureSize = 4096;

            // 배경을 Tiled 모드로 반복 배치하려면 스프라이트 메시가 사각형 전체여야 한다
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
