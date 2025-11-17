using UnityEngine;
using System.IO;
using UnityEditor;

public class TextureGrayscaleSaver : MonoBehaviour
{
    public Texture2D sourceTexture; // 인스펙터에서 설정하세요

    [ContextMenu("Convert and Save Grayscale")]
    async void ConvertAndSaveGrayscale()
    {
        // 1. 원본 텍스처를 복사 (Read/Write Enabled 필요)
        Texture2D grayTex = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);

        // 2. 픽셀 전체 가져오기
        Color[] pixels = sourceTexture.GetPixels();

        // 3. 그레이스케일로 변환
        for (int i = 0; i < pixels.Length; i++)
        {
            float gray = pixels[i].grayscale; // 또는 (r + g + b) / 3f
            pixels[i] = new Color(gray, gray, gray, pixels[i].a);
        }

        // 4. 변환된 픽셀을 새 텍스처에 적용
        grayTex.SetPixels(pixels);
        grayTex.Apply();

        // 5. 파일로 저장 (PNG 형식)
        byte[] pngData = grayTex.EncodeToPNG();
        var fileName = sourceTexture.name + "_Grayscale.png";
        string path = Path.Combine(Application.dataPath, "6.Textures/" + fileName);
        
        // File.WriteAllBytes(path, pngData);
        // Debug.Log("흑백 이미지 저장 완료: " + path);

        try
        {
            Debug.Log("흑백 이미지 생성 시작!");
            await File.WriteAllBytesAsync(path, pngData);
            AssetDatabase.Refresh();
            Debug.Log("흑백 이미지 저장 완료: " + path);
        }
        catch (IOException e)
        {
            Debug.LogError("저장 실패: " + e.Message);
        }

    }
}
