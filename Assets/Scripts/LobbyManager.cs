using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("Игровые элементы")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Transform playersListContainer;
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("Попап приглашения")]
    [SerializeField] private GameObject invitePopup;
    [SerializeField] private TMP_Text inviteText;
    [SerializeField] private Button acceptInviteButton;
    [SerializeField] private Button declineInviteButton;

    [Header("Попап отказа")]
    [SerializeField] private GameObject declinePopup;
    [SerializeField] private TMP_Text declineText;
    [SerializeField] private Button okButton;

    private const byte INVITE_EVENT = 1;
    private const byte DECLINE_EVENT = 2;

    /// <summary>
    /// Инициализация лобби, подключение к Photon и настройка UI.
    /// </summary>
    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            PhotonNetwork.NickName = "Player" + Random.Range(1000, 9999);

        PhotonNetwork.ConnectUsingSettings();
        playerNameInput.onValueChanged.AddListener(delegate { OnNameChanged(); });

        PhotonNetwork.AddCallbackTarget(this);

        invitePopup.SetActive(false);
        declinePopup.SetActive(false);

        okButton.onClick.AddListener(HideDeclinePopup);
    }

    /// <summary>
    /// Обработчик успешного подключения к Photon.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("GlobalRoom", new RoomOptions { MaxPlayers = 20 }, TypedLobby.Default);
    }

    /// <summary>
    /// Обработчик входа в комнату. Устанавливает имя игрока и обновляет список.
    /// </summary>
    public override void OnJoinedRoom()
    {
        SetPlayerName();
        UpdateRoomPlayers();
    }

    /// <summary>
    /// Обновление списка игроков при входе нового игрока в комнату.
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomPlayers();
    }

    /// <summary>
    /// Обновление списка игроков при выходе игрока из комнаты.
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomPlayers();
    }

    /// <summary>
    /// Обновляет имя игрока при его изменении в UI.
    /// </summary>
    private void OnNameChanged()
    {
        if (!PhotonNetwork.IsConnected || string.IsNullOrEmpty(playerNameInput.text))
            return;

        PhotonNetwork.NickName = playerNameInput.text;
        SetPlayerName();
        UpdateRoomPlayers();
    }

    /// <summary>
    /// Устанавливает имя игрока в кастомные свойства Photon.
    /// </summary>
    private void SetPlayerName()
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["PlayerName"] = PhotonNetwork.NickName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// Обновляет список игроков в лобби, создавая UI-элементы для каждого игрока.
    /// </summary>
    private void UpdateRoomPlayers()
    {
        foreach (Transform child in playersListContainer)
            Destroy(child.gameObject);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerName = player.CustomProperties.ContainsKey("PlayerName") ? (string)player.CustomProperties["PlayerName"] : "Unknown";

            GameObject entry = Instantiate(playerEntryPrefab, playersListContainer);
            entry.transform.Find("PlayerNameText").GetComponent<TMP_Text>().text = playerName;
            Button inviteButton = entry.transform.Find("InviteButton").GetComponent<Button>();

            if (player != PhotonNetwork.LocalPlayer)
            {
                inviteButton.onClick.AddListener(() => SendInvite(player));
            }
            else
            {
                inviteButton.interactable = false;
            }
        }
    }

    /// <summary>
    /// Отправляет приглашение другому игроку.
    /// </summary>
    private void SendInvite(Player targetPlayer)
    {
        object[] content = new object[] { PhotonNetwork.NickName, PhotonNetwork.LocalPlayer.ActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetPlayer.ActorNumber } };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(INVITE_EVENT, content, options, sendOptions);
    }

    /// <summary>
    /// Обрабатывает сетевые события Photon (приглашение и отказ).
    /// </summary>
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == INVITE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            string inviterName = (string)data[0];
            int inviterId = (int)data[1];

            ShowInvitePopup(inviterName, inviterId);
        }
        else if (photonEvent.Code == DECLINE_EVENT)
        {
            object[] data = (object[])photonEvent.CustomData;
            string playerName = (string)data[0];

            ShowDeclinePopup(playerName);
        }
    }

    /// <summary>
    /// Показывает попап с приглашением принять матч.
    /// </summary>
    private void ShowInvitePopup(string inviterName, int inviterId)
    {
        inviteText.text = $"{inviterName} приглашает вас в матч!";
        invitePopup.SetActive(true);

        acceptInviteButton.onClick.RemoveAllListeners();
        acceptInviteButton.onClick.AddListener(() => AcceptInvite(inviterId));

        declineInviteButton.onClick.RemoveAllListeners();
        declineInviteButton.onClick.AddListener(() => DeclineInvite(inviterId));
    }

    /// <summary>
    /// Отклоняет приглашение и отправляет отказ пригласившему игроку.
    /// </summary>
    private void DeclineInvite(int inviterId)
    {
        invitePopup.SetActive(false);

        object[] content = new object[] { PhotonNetwork.NickName };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { inviterId } };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(DECLINE_EVENT, content, options, sendOptions);
    }

    /// <summary>
    /// Показывает попап отказа от приглашения.
    /// </summary>
    private void ShowDeclinePopup(string playerName)
    {
        declineText.text = $"{playerName} отклонил ваше приглашение";
        declinePopup.SetActive(true);
    }

    /// <summary>
    /// Закрывает попап отказа.
    /// </summary>
    private void HideDeclinePopup()
    {
        foreach (Transform child in declinePopup.transform)
        {
            child.gameObject.SetActive(false);
        }

        declinePopup.SetActive(false);
    }

    /// <summary>
    /// Принимает приглашение и переводит игроков в новую комнату.
    /// </summary>
    private void AcceptInvite(int inviterId)
    {
        invitePopup.SetActive(false);
        string matchRoom = $"Match_{inviterId}_{PhotonNetwork.LocalPlayer.ActorNumber}";
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.JoinOrCreateRoom(matchRoom, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    /// <summary>
    /// Удаляет объект из списка сетевых обработчиков при выходе из сцены.
    /// </summary>
    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}