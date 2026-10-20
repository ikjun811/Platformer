using UnityEditor;
using UnityEngine;

namespace Platformer.EditorTools
{
    /// <summary>
    /// Assets/_Project/Art 아래에 새로 추가되는 이미지에 픽셀아트 임포트 설정을 자동 적용한다.
    /// 최초 임포트 시에만 적용하므로, 이후 인스펙터에서 바꾼 값은 유지된다.
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        const string ArtFolder = "Assets/_Project/Art/";
        const int PixelsPerUnit = 16;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;

            // 재임포트 시에는 사용자가 직접 바꾼 설정을 덮어쓰지 않는다
            if (!assetImporter.importSettingsMissing) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
        }
    }
}
