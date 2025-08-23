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
    public List<float> embedding; // ���� ���� �ʵ�
}

[System.Serializable]
public class VectorDatabase
{
    public List<VectorEntry> vectorEntries = new List<VectorEntry>();
}

//OpenAI �Ӻ��� API�� ��û/������ ���� ������ ����
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

    //[Tools/Generate Vector DB] �׸��� �߰�
    [MenuItem("Tools/Generate Vector DB")]
    public static async void EmbedKnowledgeFile()
    {
        if (!File.Exists(InputPath))
        {
            Debug.LogError($"�Է� ����({InputPath})�� ã�� �� �����ϴ�. ���� knowkedge JSON ������ �������ּ���.");
            return;
        }

        // �Է� JSON ���� �б�
        string jsonInput = File.ReadAllText(InputPath);
        KnowledgeData knowledgeData = JsonUtility.FromJson<KnowledgeData>(jsonInput);

        VectorDatabase vectorDatabase = new VectorDatabase();
        int entryCount = knowledgeData.knowledgeEntries.Count;

        Debug.Log($"�Ӻ��� �۾��� �����մϴ�. �� {entryCount}���� �׸��� �ֽ��ϴ�.");

        // �� �׸��� ��ȸ�ϸ� �Ӻ��� API ȣ��
        for (int i = 0; i < entryCount; i++)
        {
            KnowledgeEntry entry = knowledgeData.knowledgeEntries[i];

            // API ȣ�� (�񵿱�)
            List<float> vector = await GetEmbedding(entry.sentence);

            if (vector != null)
            {
                // ��� ����
                vectorDatabase.vectorEntries.Add(new VectorEntry
                {
                    itemId = entry.itemId,
                    sentence = entry.sentence,
                    embedding = vector
                });

                // ���� ��Ȳ ǥ��
                EditorUtility.DisplayProgressBar("Embedding In Progress", $"Processing item {i + 1}/{entryCount}...", (float)(i + 1) / entryCount);
            }
            else
            {
                Debug.LogError($"�׸� '{entry.itemId}'�� �Ӻ����� �����߽��ϴ�.");
                EditorUtility.ClearProgressBar();
                return;
            }
        }

        // ���� ����� JSON���� ��ȯ�Ͽ� ���Ͽ� ����
        string jsonOutput = JsonUtility.ToJson(vectorDatabase, true);
        File.WriteAllText(OutputPath, jsonOutput);

        EditorUtility.ClearProgressBar();
        Debug.Log($"�Ӻ��� �۾� �Ϸ�! {OutputPath} ������ �����Ǿ����ϴ�.");
        AssetDatabase.Refresh();
    }

    private static async Task<List<float>> GetEmbedding(string text)
    {
        string apiKey = ApiKeyLoader.GetApiKey("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OpenAI API Key�� ã�� �� �����ϴ�.");
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