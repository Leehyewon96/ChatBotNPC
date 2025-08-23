using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class KnowledgeEntry
{
    public int itemId;
    public string sentence;
}

[System.Serializable]
public class KnowledgeData
{
    public List<KnowledgeEntry> knowledgeEntries = new List<KnowledgeEntry>();
}

[System.Serializable]
public class VectorEntry
{
    public int itemId;
    public string sentence;
    public List<float> embedding; // 벡터 저장 필드
}

[System.Serializable]
public class VectorDatabase
{
    public List<VectorEntry> vectorEntries = new List<VectorEntry>();
}

//OpenAI 임베딩 API의 요청/응답을 위한 데이터 구조
[System.Serializable]
public class EmbeddingRequest
{
    public string input;
    public string model;
}

[System.Serializable]
public class EmbeddingObject
{
    public List<float> embedding;
}

[System.Serializable]
public class EmbeddingData
{
    public List<EmbeddingObject> data;
}


public class KnowledgeEmbedder
{
    private const string ApiUrl = "https://api.openai.com/v1/embeddings";
    private const string InputPath = "Assets/Resources/KnowledgeBase/knowledge_base.json";
    private const string OutputPath = "Assets/Resources/KnowledgeBase/vector_db.json";

    //[Tools/Generate Vector DB] 항목을 추가
    [MenuItem("Tools/Generate Vector DB")]
    public static async void EmbedKnowledgeFile()
    {
        if (!File.Exists(InputPath))
        {
            Debug.LogError($"입력 파일({InputPath})을 찾을 수 없습니다. 먼저 knowkedge JSON 파일을 생성해주세요.");
            return;
        }

        // 입력 JSON 파일 읽기
        string jsonInput = File.ReadAllText(InputPath);
        KnowledgeData knowledgeData = JsonUtility.FromJson<KnowledgeData>(jsonInput);

        VectorDatabase vectorDatabase = new VectorDatabase();
        int entryCount = knowledgeData.knowledgeEntries.Count;

        Debug.Log($"임베딩 작업을 시작합니다. 총 {entryCount}개의 항목이 있습니다.");

        // 각 항목을 순회하며 임베딩 API 호출
        for (int i = 0; i < entryCount; i++)
        {
            KnowledgeEntry entry = knowledgeData.knowledgeEntries[i];

            // API 호출 (비동기)
            List<float> vector = await GetEmbedding(entry.sentence);

            if (vector != null)
            {
                // 결과 저장
                vectorDatabase.vectorEntries.Add(new VectorEntry
                {
                    itemId = entry.itemId,
                    sentence = entry.sentence,
                    embedding = vector
                });

                // 진행 상황 표시
                EditorUtility.DisplayProgressBar("Embedding In Progress", $"Processing item {i + 1}/{entryCount}...", (float)(i + 1) / entryCount);
            }
            else
            {
                Debug.LogError($"항목 '{entry.itemId}'의 임베딩에 실패했습니다.");
                EditorUtility.ClearProgressBar();
                return;
            }
        }

        // 최종 결과를 JSON으로 변환하여 파일에 저장
        string jsonOutput = JsonUtility.ToJson(vectorDatabase, true);
        File.WriteAllText(OutputPath, jsonOutput);

        EditorUtility.ClearProgressBar();
        Debug.Log($"임베딩 작업 완료! {OutputPath} 파일이 생성되었습니다.");
        AssetDatabase.Refresh();
    }

    private static async Task<List<float>> GetEmbedding(string text)
    {
        string apiKey = ApiKeyLoader.GetApiKey("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OpenAI API Key를 찾을 수 없습니다.");
            return null;
        }

        var requestData = new EmbeddingRequest
        {
            input = text,
            model = "text-embedding-3-small"
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                EmbeddingData responseData = JsonUtility.FromJson<EmbeddingData>(jsonResponse);
                if (responseData != null && responseData.data.Count > 0)
                {
                    return responseData.data[0].embedding;
                }
            }
            else
            {
                Debug.LogError($"API Error: {request.error}\nResponse: {request.downloadHandler.text}");
            }
        }
        return null;
    }
}