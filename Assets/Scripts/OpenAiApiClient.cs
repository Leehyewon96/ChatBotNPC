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

#region OpenAI Embedding API�� ���� ����ȭ Ŭ����
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


// ���� API ��� Ŭ����
public class OpenAiApiClient : MonoBehaviour
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string EmbeddingApiUrl = "https://api.openai.com/v1/embeddings";
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
    /// <param name="userMessage">������ ���� �޽���</param>
    /// <returns>NPC�� ���� �޽���</returns>
    public async Task<string> SendMessageToOpenAI(string userMessage)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return "API Key is missing.";
        }

        if (VectorDBManager.Instance == null) return "VectorDBManager�� ������� �ʾҽ��ϴ�.";

        //VectorDBManager�� ����� DB���� ���� ������ ����(Context)�� �˻�
        VectorEntry contextEntry = await VectorDBManager.Instance.FindMostSimilarEntry(userMessage);
        string contextSentence = "���� ������ ã�� ���߽��ϴ�."; // �⺻��
        if (contextEntry != null)
        {
            contextSentence = contextEntry.sentence;
        }

        //�˻��� ����(Context)�� �����Ͽ� ���� ������Ʈ�� ����
        string systemPrompt = "����� '���׸��� �����' ������ ģ���ϰ� �ڽ��� NPC�Դϴ�.";
        string finalPrompt = $"���� �ڷ�: \"{contextSentence}\"\n\n�� ���� �ڷḦ �������� ���� ������ ���� ������ּ���:\n����: \"{userMessage}\"";

        Debug.Log($"[SendMessageToOpenAI] Final Prompt:\n{finalPrompt}");

        // ��û ������ ����
        var requestData = new ChatRequest
        {
            model = "gpt-3.5-turbo",
            messages = new List<ChatMessage>
            {
                //enum Ÿ�Կ� ���� content ���� �ʿ� - �̰͵� ���̺�� ���� �ʿ�
                new ChatMessage { role = "system", content = systemPrompt },
                new ChatMessage { role = "user", content = finalPrompt }
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

    /// <summary>
    /// �־��� �ؽ�Ʈ�� OpenAI �Ӻ��� API�� ���� ���ͷ� ��ȯ
    /// </summary>
    /// <param name="textToEmbed">���ͷ� ��ȯ�� ���ڿ�</param>
    /// <returns>��ȯ�� ����(List<float>) �Ǵ� ���� �� null</returns>
    public async Task<List<float>> GetEmbedding(string textToEmbed)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("API Key is missing.");
            return null;
        }

        // API ��û ������ ����
        var requestData = new EmbeddingRequest
        {
            input = textToEmbed,
            model = "text-embedding-3-small"
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        // UnityWebRequest ���� �� ����
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
                    //���� ������ ó��
                    string jsonResponse = request.downloadHandler.text;
                    EmbeddingData responseData = JsonUtility.FromJson<EmbeddingData>(jsonResponse);

                    if (responseData != null && responseData.data != null && responseData.data.Count > 0)
                    {
                        // ���� �� ���� ����Ʈ ��ȯ
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