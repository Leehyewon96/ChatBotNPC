using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcController : MonoBehaviour
{
    enum ChatType
    {
        None = 0,
        User=1,
        AI=2,
    }


    [SerializeField] OpenAiApiClient apiClient; // �ν����� â���� OpenAiApiClient�� �ִ� ������Ʈ�� ����
    [SerializeField] TextMeshProUGUI chatText; //ä�� �ؽ�Ʈ ������Ʈ ����
    [SerializeField] Button sendButton;
    [SerializeField] TMP_InputField userInput;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] Transform content;

    private void Start()
    {
        BindUI();
    }

    void BindUI()
    {
        sendButton.onClick.AddListener(() =>
            {
                if (userInput == null || string.IsNullOrEmpty(userInput.text))
                    return;

                SendQuestion(userInput.text);
            });
    }

    void SendQuestion(string question)
    {
        AddChat(question, ChatType.User);

        OnPlayerAskQuestion(question);
        userInput.text = string.Empty;
    }


    public async void OnPlayerAskQuestion(string question)
    {
        sendButton.interactable = false;
        string playerQuestion = question;
        string loadingText = "���� ��..."; // �ε� ǥ��
        var newChat = AddChat(loadingText, ChatType.AI);
        
        // �񵿱�� API ȣ�� �� ��� �޾ƿ���
        string response = await apiClient.SendMessageToOpenAI(playerQuestion);

        // UI�� ���� �亯 ǥ��
        newChat.SetText(response);
        newChat.SetLayoutDirty();
        sendButton.interactable = true;

        //��Ʈ�� ��� �߰� �ʿ�
    }

    TextMeshProUGUI AddChat(string chat, ChatType chatType)
    {
        var newChat = Instantiate(chatText, content);
        newChat.SetText(chat);

        switch (chatType)
        {
            case ChatType.User:
                newChat.alignment = TextAlignmentOptions.MidlineRight;
                break;
            case ChatType.AI:
                newChat.alignment = TextAlignmentOptions.MidlineLeft;
                break;
            default:
                break;
        }


        return newChat;
    }
}