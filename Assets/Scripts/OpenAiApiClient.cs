using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

#region OpenAI API ��û, ���� Ŭ����
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

// ���� API ��� Ŭ����
public class OpenAiApiClient : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private string apiKey;

    void Start()
    {
        // ApiKeyLoader�� API Ű ������
        apiKey = ApiKeyLoader.GetApiKey("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("OpenAI API Key is not set!");
        }
    }

    /// <summary>
    /// OpenAI�� ������Ʈ ������ ������ �񵿱�� ����
    /// </summary>
    /// <param name="prompt">������ ���� �޽���</param>
    /// <returns>NPC�� ���� �޽���</returns>
    public async Task<string> SendMessageToOpenAI(string prompt)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "API Key is missing.";
        }

        // ��û ������ ����
        var requestData = new ChatRequest
        {
            model = "gpt-3.5-turbo",
            messages = new List<ChatMessage>
            {
                //enum Ÿ�Կ� ���� content ���� �ʿ� - �̰͵� ���̺�� ���� �ʿ�
                new ChatMessage { role = "system", content = "You are a helpful assistant in a fantasy game." },
                new ChatMessage { role = "user", content = prompt }
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        //UnityWebRequest ���� �� ����
        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            //�񵿱� ��û
            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    //���� ������ ó��
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