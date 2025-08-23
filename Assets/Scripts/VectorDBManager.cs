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
    private const string dbPath = "KnowledgeBase/vector_db.json"; // Resources 폴더에 있다고 가정

    // OpenAI 임베딩 API 호출을 위한 참조
    public OpenAiApiClient apiClient; // 임베딩 기능을 OpenAiApiClient로 옮기거나 별도 클래스로 만들어야 함

    void Awake()
    {
        instance = this;
        LoadVectorDB();
    }

    /// <summary>
    /// Resources 폴더에서 vector_db.json 파일을 로드
    /// </summary>
    private void LoadVectorDB()
    {
        TextAsset dbJsonAsset = Resources.Load<TextAsset>(dbPath.Replace(".json", ""));
        if (dbJsonAsset == null)
        {
            Debug.LogError($"Vector DB 파일을 Resources/{dbPath} 에서 찾을 수 없습니다.");
            return;
        }

        vectorDB = JsonUtility.FromJson<VectorDatabase>(dbJsonAsset.text);
        Debug.Log($"Vector DB 로드 완료. 총 {vectorDB.vectorEntries.Count}개의 항목이 로드되었습니다.");
    }

    /// <summary>
    /// 주어진 질문과 가장 유사한 지식 문장을 DB에서 찾아 반환
    /// </summary>
    /// <param name="query">플레이어의 질문 문자열</param>
    /// <returns>가장 관련성 높은 지식 문장 (string)</returns>
    public async Task<VectorEntry> FindMostSimilarEntry(string query)
    {
        if (vectorDB == null || vectorDB.vectorEntries.Count == 0)
        {
            Debug.LogError("지식 데이터베이스가 로드되지 않았습니다.");
            return null;
        }

        List<float> queryVector = await apiClient.GetEmbedding(query);
        if (queryVector == null)
        {
            Debug.LogError("질문을 이해하지 못했습니다. (임베딩 실패)");
            return null;
        }

        VectorEntry mostSimilarEntry = vectorDB.vectorEntries
            .OrderByDescending(entry => CosineSimilarity(entry.embedding, queryVector))
            .FirstOrDefault();

        return mostSimilarEntry;
    }

    /// <summary>
    /// 두 벡터 간의 코사인 유사도를 계산합니다. (값이 1에 가까울수록 유사함)
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