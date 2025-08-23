using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class VectorEntry
{
    public int itemId;
    public string sentence;
    public List<float> embedding;
}

[System.Serializable]
public class VectorDatabase
{
    public List<VectorEntry> vectorEntries = new List<VectorEntry>();
}


public class VectorDBManager : MonoBehaviour
{
    private static VectorDBManager instance;
    public static VectorDBManager Instance { 
        get 
        {
            if (instance == null)
            {
                instance = new VectorDBManager();
            }
            return instance; 
        }
    }

    private VectorDatabase vectorDB;
    private const string dbPath = "KnowledgeBase/vector_db.json"; // Resources ������ �ִٰ� ����

    // OpenAI �Ӻ��� API ȣ���� ���� ����
    public OpenAiApiClient apiClient; // �Ӻ��� ����� OpenAiApiClient�� �ű�ų� ���� Ŭ������ ������ ��

    void Awake()
    {
        instance = this;
        LoadVectorDB();
    }

    /// <summary>
    /// Resources �������� vector_db.json ������ �ε�
    /// </summary>
    private void LoadVectorDB()
    {
        TextAsset dbJsonAsset = Resources.Load<TextAsset>(dbPath.Replace(".json", ""));
        if (dbJsonAsset == null)
        {
            Debug.LogError($"Vector DB ������ Resources/{dbPath} ���� ã�� �� �����ϴ�.");
            return;
        }

        vectorDB = JsonUtility.FromJson<VectorDatabase>(dbJsonAsset.text);
        Debug.Log($"Vector DB �ε� �Ϸ�. �� {vectorDB.vectorEntries.Count}���� �׸��� �ε�Ǿ����ϴ�.");
    }

    /// <summary>
    /// �־��� ������ ���� ������ ���� ������ DB���� ã�� ��ȯ
    /// </summary>
    /// <param name="query">�÷��̾��� ���� ���ڿ�</param>
    /// <returns>���� ���ü� ���� ���� ���� (string)</returns>
    public async Task<VectorEntry> FindMostSimilarEntry(string query)
    {
        if (vectorDB == null || vectorDB.vectorEntries.Count == 0)
        {
            Debug.LogError("���� �����ͺ��̽��� �ε���� �ʾҽ��ϴ�.");
            return null;
        }

        List<float> queryVector = await apiClient.GetEmbedding(query);
        if (queryVector == null)
        {
            Debug.LogError("������ �������� ���߽��ϴ�. (�Ӻ��� ����)");
            return null;
        }

        VectorEntry mostSimilarEntry = vectorDB.vectorEntries
            .OrderByDescending(entry => CosineSimilarity(entry.embedding, queryVector))
            .FirstOrDefault();

        return mostSimilarEntry;
    }

    /// <summary>
    /// �� ���� ���� �ڻ��� ���絵�� ����մϴ�. (���� 1�� �������� ������)
    /// </summary>
    private float CosineSimilarity(List<float> vecA, List<float> vecB)
    {
        if (vecA == null || vecB == null || vecA.Count != vecB.Count)
        {
            return 0;
        }

        float dotProduct = 0.0f;
        float magA = 0.0f;
        float magB = 0.0f;

        for (int i = 0; i < vecA.Count; i++)
        {
            dotProduct += vecA[i] * vecB[i];
            magA += vecA[i] * vecA[i];
            magB += vecB[i] * vecB[i];
        }

        magA = Mathf.Sqrt(magA);
        magB = Mathf.Sqrt(magB);

        if (magA == 0 || magB == 0)
        {
            return 0;
        }

        return dotProduct / (magA * magB);
    }

}