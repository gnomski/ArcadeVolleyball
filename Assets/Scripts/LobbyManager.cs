using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    [Header("������� ��������")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Transform playersListContainer;
    [SerializeField] private GameObject playerEntryPrefab;

    [Header("����� �����������")]
    [SerializeField] private GameObject invitePopup;
    [SerializeField] private TMP_Text inviteText;
    [SerializeField] private Button acceptInviteButton;
    [SerializeField] private Button declineInviteButton;

    [Header("����� ������")]
    [SerializeField] private GameObject declinePopup;
    [SerializeField] private TMP_Text declineText;
    [SerializeField] private Button okButton;

    private const byte INVITE_EVENT = 1;
    private const byte DECLINE_EVENT = 2;

    /// <summary>
    /// ������������� �����, ����������� � Photon � ��������� UI.
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
    /// ���������� ��������� ����������� � Photon.
    /// </summary>
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("GlobalRoom", new RoomOptions { MaxPlayers = 20 }, TypedLobby.Default);
    }

    /// <summary>
    /// ���������� ����� � �������. ������������� ��� ������ � ��������� ������.
    /// </summary>
    public override void OnJoinedRoom()
    {
        SetPlayerName();
        UpdateRoomPlayers();
    }

    /// <summary>
    /// ���������� ������ ������� ��� ����� ������ ������ � �������.
    /// </summary>
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateRoomPlayers();
    }

    /// <summary>
    /// ���������� ������ ������� ��� ������ ������ �� �������.
    /// </summary>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateRoomPlayers();
    }

    /// <summary>
    /// ��������� ��� ������ ��� ��� ��������� � UI.
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
    /// ������������� ��� ������ � ��������� �������� Photon.
    /// </summary>
    private void SetPlayerName()
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["PlayerName"] = PhotonNetwork.NickName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    /// <summary>
    /// ��������� ������ ������� � �����, �������� UI-�������� ��� ������� ������.
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
    /// ���������� ����������� ������� ������.
    /// </summary>
    private void SendInvite(Player targetPlayer)
    {
        object[] content = new object[] { PhotonNetwork.NickName, PhotonNetwork.LocalPlayer.ActorNumber };
        RaiseEventOptions options = new RaiseEventOptions { TargetActors = new int[] { targetPlayer.ActorNumber } };
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(INVITE_EVENT, content, options, sendOptions);
    }

    /// <summary>
    /// ������������ ������� ������� Photon (����������� � �����).
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
    /// ���������� ����� � ������������ ������� ����.
    /// </summary>
    private void ShowInvitePopup(string inviterName, int inviterId)
    {
        inviteText.text = $"{inviterName} ���������� ��� � ����!";
        invitePopup.SetActive(true);

        acceptInviteButton.onClick.RemoveAllListeners();
        acceptInviteButton.onClick.AddListener(() => AcceptInvite(inviterId));

        declineInviteButton.onClick.RemoveAllListeners();
        declineInviteButton.onClick.AddListener(() => DeclineInvite(inviterId));
    }

    /// <summary>
    /// ��������� ����������� � ���������� ����� ������������� ������.
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
    /// ���������� ����� ������ �� �����������.
    /// </summary>
    private void ShowDeclinePopup(string playerName)
    {
        declineText.text = $"{playerName} �������� ���� �����������";
        declinePopup.SetActive(true);
    }

    /// <summary>
    /// ��������� ����� ������.
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
    /// ��������� ����������� � ��������� ������� � ����� �������.
    /// </summary>
    private void AcceptInvite(int inviterId)
    {
        invitePopup.SetActive(false);
        string matchRoom = $"Match_{inviterId}_{PhotonNetwork.LocalPlayer.ActorNumber}";
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.JoinOrCreateRoom(matchRoom, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    /// <summary>
    /// ������� ������ �� ������ ������� ������������ ��� ������ �� �����.
    /// </summary>
    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}