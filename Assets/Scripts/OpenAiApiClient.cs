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

// 메인 API 통신 클래스
public class OpenAiApiClient : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
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
    /// <param name="prompt">유저가 보낼 메시지</param>
    /// <returns>NPC의 응답 메시지</returns>
    public async Task<string> SendMessageToOpenAI(string prompt)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "API Key is missing.";
        }

        // 요청 데이터 생성
        var requestData = new ChatRequest
        {
            model = "gpt-3.5-turbo",
            messages = new List<ChatMessage>
            {
                //enum 타입에 대한 content 지정 필요 - 이것도 테이블로 관리 필요
                new ChatMessage { role = "system", content = "You are a helpful assistant in a fantasy game." },
                new ChatMessage { role = "user", content = prompt }
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
}