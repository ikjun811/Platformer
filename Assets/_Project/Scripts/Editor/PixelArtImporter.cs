using UnityEditor;
using UnityEngine;

namespace Platformer.EditorTools
{
    /// <summary>
    /// Assets/_Project/Art 아래 이미지에 임포트 설정을 자동 적용한다.
    /// - Characters/, Environment/: 고해상도 일러스트 (PPU 128, Bilinear). Tools/ArtPipeline 스크립트가 만드는 파일이라
    ///   임포트할 때마다 강제 적용한다. 캐릭터 프레임 분할은 런타임에 PlayerAnimator가 한다
    /// - 그 외 (Placeholder 등 임시 타일): 픽셀 설정 (PPU 16, Point). 최초 임포트 시에만 적용하므로 이후 인스펙터에서 바꾼 값은 유지된다
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const string ArtFolder = "Assets/_Project/Art/";
        const string CharacterFolder = "Assets/_Project/Art/Characters/";
        const string EnvironmentFolder = "Assets/_Project/Art/Environment/";
        const int PixelsPerUnit = 16;
        const int IllustrationPixelsPerUnit = 128;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;

            var importer = (TextureImporter)assetImporter;

            if (assetPath.StartsWith(CharacterFolder) || assetPath.StartsWith(EnvironmentFolder))
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
    }
}
