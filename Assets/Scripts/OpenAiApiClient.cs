using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

#region OpenAI API 요청, 응답 클래스
[Serializable]
public class ChatMessage
{
    public string role;
    public string content;
}

[Serializable]
public class ChatRequest
{
    public string model;
    public List<ChatMessage> messages;
}

[Serializable]
public class ChatResponse
{
    public List<Choice> choices;
}

[Serializable]
public class Choice
{
    public ChatMessage message;
}
#endregion

#region OpenAI Embedding API를 위한 직렬화 클래스
[Serializable]
public class EmbeddingRequest
{
    public string input;
    public string model;
}

[Serializable]
public class EmbeddingObject
{
    public List<float> embedding;
    public int index;
    public string @object;
}

[Serializable]
public class EmbeddingData
{
    public List<EmbeddingObject> data;
    public string model;
    public string @object;
}
#endregion


// 메인 API 통신 클래스
public class OpenAiApiClient : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string EmbeddingApiUrl = "https://api.openai.com/v1/embeddings";
    private string apiKey;

    void Start()
    {
        // ApiKeyLoader로 API 키 가져옴
        apiKey = ApiKeyLoader.GetApiKey("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OpenAI API Key is not set!");
        }
    }

    /// <summary>
    /// OpenAI에 프롬프트 보내고 응답을 비동기로 받음
    /// </summary>
    /// <param name="userMessage">유저가 보낼 메시지</param>
    /// <returns>NPC의 응답 메시지</returns>
    public async Task<string> SendMessageToOpenAI(string userMessage)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "API Key is missing.";
        }

        if (VectorDBManager.Instance == null) return "VectorDBManager가 연결되지 않았습니다.";

        //VectorDBManager를 사용해 DB에서 가장 유사한 정보(Context)를 검색
        VectorEntry contextEntry = await VectorDBManager.Instance.FindMostSimilarEntry(userMessage);
        string contextSentence = "관련 정보를 찾지 못했습니다."; // 기본값
        if (contextEntry != null)
        {
            contextSentence = contextEntry.sentence;
        }

        //검색된 정보(Context)를 포함하여 최종 프롬프트를 구성
        string systemPrompt = "당신은 '에테리아 연대기' 게임의 친절하고 박식한 NPC입니다.";
        string finalPrompt = $"참고 자료: \"{contextSentence}\"\n\n위 참고 자료를 바탕으로 다음 질문에 대해 대답해주세요:\n질문: \"{userMessage}\"";

        Debug.Log($"[SendMessageToOpenAI] Final Prompt:\n{finalPrompt}");

        // 요청 데이터 생성
        var requestData = new ChatRequest
        {
            model = "gpt-3.5-turbo",
            messages = new List<ChatMessage>
            {
                //enum 타입에 대한 content 지정 필요 - 이것도 테이블로 관리 필요
                new ChatMessage { role = "system", content = systemPrompt },
                new ChatMessage { role = "user", content = finalPrompt }
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        //UnityWebRequest 생성 및 설정
        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            //비동기 요청
            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    //응답 데이터 처리
                    string jsonResponse = request.downloadHandler.text;
                    ChatResponse responseData = JsonUtility.FromJson<ChatResponse>(jsonResponse);

                    if (responseData != null && responseData.choices.Count > 0)
                    {
                        return responseData.choices[0].message.content;
                    }
                    else
                    {
                        return "Failed to parse response or no choices returned.";
                    }
                }
                else
                {
                    Debug.LogError($"Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    return $"Error: {request.error}";
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return "An exception occurred during the request.";
            }
        }
    }

    /// <summary>
    /// 주어진 텍스트를 OpenAI 임베딩 API를 통해 벡터로 변환
    /// </summary>
    /// <param name="textToEmbed">벡터로 변환할 문자열</param>
    /// <returns>변환된 벡터(List<float>) 또는 실패 시 null</returns>
    public async Task<List<float>> GetEmbedding(string textToEmbed)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key is missing.");
            return null;
        }

        // API 요청 데이터 생성
        var requestData = new EmbeddingRequest
        {
            input = textToEmbed,
            model = "text-embedding-3-small"
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        // UnityWebRequest 생성 및 설정
        using (UnityWebRequest request = new UnityWebRequest(EmbeddingApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    //응답 데이터 처리
                    string jsonResponse = request.downloadHandler.text;
                    EmbeddingData responseData = JsonUtility.FromJson<EmbeddingData>(jsonResponse);

                    if (responseData != null && responseData.data != null && responseData.data.Count > 0)
                    {
                        // 성공 시 벡터 리스트 반환
                        return responseData.data[0].embedding;
                    }
                    else
                    {
                        Debug.LogError("Failed to parse embedding response or no data returned.");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }
    }
}