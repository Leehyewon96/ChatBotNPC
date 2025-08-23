using UnityEngine;
using System.IO;
using System.Collections.Generic;

public static class ApiKeyLoader
{
    private static Dictionary<string, string> secrets;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadKeys()
    {
        secrets = new Dictionary<string, string>();
        TextAsset secretsFile = Resources.Load<TextAsset>("APIKey/secrets");

        if (secretsFile == null)
        {
            Debug.LogError("API secrets file not found at Assets/Resources/secrets.txt");
            return;
        }

        // 텍스트 파일을 한 줄씩 읽어서 secrets 딕셔너리에 저장
        using (StringReader reader = new StringReader(secretsFile.text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    secrets[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
    }

    // 저장된 키를 가져오는 함수
    public static string GetApiKey(string keyName)
    {
        if (secrets != null && secrets.ContainsKey(keyName))
        {
            return secrets[keyName];
        }

        Debug.LogError($"API Key '{keyName}' not found.");
        return null;
    }
}