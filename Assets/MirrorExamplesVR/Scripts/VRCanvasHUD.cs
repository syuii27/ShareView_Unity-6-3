using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using Mirror.Discovery;
using TMPro;
using UnityEngine.SceneManagement;

public class VRCanvasHUD : MonoBehaviour
{
    // this will check for games to join, if non, start host.
    public bool alwaysAutoStart = false;
    public VRNetworkDiscovery networkDiscovery;
    readonly Dictionary<long, ServerResponse> discoveredServers = new Dictionary<long, ServerResponse>();
    private TouchScreenKeyboard keyboard;
    private int keyboardStatus = 0;
    // UI
    public GameObject PanelStart, PanelStop;
    public Button buttonHost, buttonServer, buttonClient, buttonStop, buttonAuto;
    public Button buttonLocalReplay;
    public ServerActionRecording actionRecord;
    public VRHostCameraControl hostCamera;
    public Text infoText;
    // legacy inputfield interaction does not auto bring up a keyboard on headset builds, use tmp.
    public TMP_InputField inputFieldAddress, inputFieldPlayerName;

    private void Start()
    {
        //ButtonClient();
        //Make sure to attach these Buttons in the Inspector
        buttonHost.onClick.AddListener(ButtonHost);
        buttonServer.onClick.AddListener(ButtonServer);
        buttonClient.onClick.AddListener(ButtonClient);
        buttonStop.onClick.AddListener(ButtonStop);
        buttonAuto.onClick.AddListener(ButtonAuto);
        if (buttonLocalReplay != null)
        {
            buttonLocalReplay.onClick.AddListener(ButtonLocalReplay);
        }

        //Update the canvas text if you have manually changed network managers address from the game object before starting the game scene
        inputFieldAddress.text = NetworkManager.singleton.networkAddress;

        //Adds a listener to the input field and invokes a method when the value changes.
        inputFieldAddress.onValueChanged.AddListener(delegate { OnValueChangedAddress(); });
        inputFieldPlayerName.onValueChanged.AddListener(delegate { OnValueChangedName(); });

        if (networkDiscovery == null)
        { networkDiscovery = GameObject.FindObjectOfType<VRNetworkDiscovery>(); }

        if (networkDiscovery == null)
        { networkDiscovery = GameObject.FindObjectOfType<VRNetworkDiscovery>(); }

        // skips waiting for users to press ui button
        if (alwaysAutoStart)
        {
            StartCoroutine(Waiter());
        }
    }

    public IEnumerator Waiter()
    {
        infoText.text = "Discovering servers..";
        discoveredServers.Clear();
        networkDiscovery.StartDiscovery();
        // we have set this as 3.1 seconds, default discovery scan is 3 seconds, allows some time if host and client are started at same time
        yield return new WaitForSeconds(3.1f);
        if (discoveredServers == null || discoveredServers.Count <= 0)
        {
            infoText.text = "No Servers found, starting as Host.";
            yield return new WaitForSeconds(1.0f);
            discoveredServers.Clear();
           // NetworkManager.singleton.onlineScene = SceneManager.GetActiveScene().name;
            NetworkManager.singleton.StartHost();
            networkDiscovery.AdvertiseServer();
        }
    }

    void Connect(ServerResponse info)
    {
        infoText.text = "Connecting to: " + info.serverId;
        networkDiscovery.StopDiscovery();
        NetworkManager.singleton.StartClient(info.uri);
    }

    public void OnDiscoveredServer(ServerResponse info)
    {
        discoveredServers[info.serverId] = info;
        Connect(info);
    }

    public void ButtonHost()
    {
        SetupInfoText("Starting as host");
        discoveredServers.Clear();
        //NetworkManager.singleton.onlineScene = SceneManager.GetActiveScene().name;
        NetworkManager.singleton.StartHost();
        networkDiscovery.AdvertiseServer();

    }

    public void ButtonServer()
    {
        SetupInfoText("Starting as server.");
        discoveredServers.Clear();
       // NetworkManager.singleton.onlineScene = SceneManager.GetActiveScene().name;
        NetworkManager.singleton.StartServer();
        networkDiscovery.AdvertiseServer();

    }

    public void ButtonClient()
    {
        SetupInfoText("Starting as client.");
        discoveredServers.Clear();
        networkDiscovery.StartDiscovery();       
    }

    public void ButtonStop()
    {
        SetupInfoText("Stopping.");
        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
        networkDiscovery.StopDiscovery();
        // we need to call setup canvas a second time in this function for it to update the abovee changes
        SetupCanvas();
    }

    public void ButtonAuto()
    {
        SetupInfoText("Auto Starting.");
        StartCoroutine(Waiter());
    }

    // Enter offline local replay mode: do NOT start any Mirror role.
    // The two NetworkBehaviour components remain unspawned (isServer/isClient false),
    // so we manually trigger their local-only init paths.
    public void ButtonLocalReplay()
    {
        StartCoroutine(LocalReplayCoroutine());
    }

    private IEnumerator LocalReplayCoroutine()
    {
        // Don't go through SetupInfoText -> SetupCanvas; SetupCanvas would re-enable PanelStart
        // because NetworkClient/Server are both inactive in local replay mode.
        infoText.text = "Local Replay Mode";
        discoveredServers.Clear();

        // FindObjectsByType with Include picks up inactive scene objects too.
        // The scene may contain multiple instances (e.g. on player rig prefabs).
        var allActionRecords = FindObjectsByType<ServerActionRecording>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var allHostCameras = FindObjectsByType<VRHostCameraControl>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (actionRecord == null && allActionRecords.Length > 0) { actionRecord = allActionRecords[0]; }
        if (hostCamera == null && allHostCameras.Length > 0) { hostCamera = allHostCameras[0]; }

        // These NetworkBehaviours are inactive at scene start (normally activated by NetworkManager).
        // Activate all instances + ancestors so Start() runs and child UI shows.
        foreach (var ar in allActionRecords) { ActivateAncestors(ar.gameObject); }
        foreach (var hc in allHostCameras) { ActivateAncestors(hc.gameObject); }

        // Wait one frame so Awake/Start fire on the freshly-activated components.
        yield return null;

        // Initialize ALL instances so their internal lists (positionrecords etc) are non-null.
        // Only the primary (actionRecord) loads JSON + populates dropdown to avoid duplicate options.
        foreach (var ar in allActionRecords)
        {
            ar.localReplayMode = true;
            if (ar == actionRecord) { ar.InitLocalReplay(); }
            else { ar.InitClientLikeDataPublic(); }
        }
        // Re-wire every VRHostCameraControl's actionrecord field to the primary loaded instance,
        // so VRHostCameraControl.Update -> actionrecord.NumOfRecords() doesn't NPE on a sibling
        // instance whose positionrecords is null.
        foreach (var hc in allHostCameras)
        {
            hc.localReplayMode = true;
            if (actionRecord != null) { hc.actionrecord = actionRecord; }
        }
        // Only run UI/playback init on the chosen primary host camera (avoid duplicate listeners).
        if (hostCamera != null) { hostCamera.InitLocalReplay(); }

        if (PanelStart != null) { PanelStart.SetActive(false); }
        if (PanelStop != null) { PanelStop.SetActive(true); }
    }

    private static void ActivateAncestors(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); }
            t = t.parent;
        }
    }

    // manually call canvas changes for performance, can lazily be done via Update()
    public void SetupCanvas()
    {
        // Here we will dump majority of the canvas UI

        if (NetworkManager.singleton == null)
        {
            SetupInfoText("NetworkManager null");
            return;
        }

        // check network status, and show required UI
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            if (NetworkClient.active)
            {
                PanelStart.SetActive(false);
                PanelStop.SetActive(true);
            }
            else
            {
                PanelStart.SetActive(true);
                PanelStop.SetActive(false);
            }
        }
        else
        {
            PanelStart.SetActive(false);
            PanelStop.SetActive(true);
        }
    }

    // useful status info to display on screen
    public void SetupInfoText(string _info)
    {
        infoText.text = _info;
        SetupCanvas();
    }

    // Invoked when the value of the text field changes.
    public void OnValueChangedAddress()
    {
        NetworkManager.singleton.networkAddress = inputFieldAddress.text;
    }

    // touchscreen keyboard can be weird, so we have an option to open it manually
    public void ButtonKeyboard(int _status)
    {
        if (TouchScreenKeyboard.isSupported)
        {
            keyboardStatus = _status;
            keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default, false, false, false, false, "", 15);
            // Open(string text, TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default, bool autocorrection = true, bool multiline = false, bool secure = false, bool alert = false, string textPlaceholder = "", int characterLimit = 0);
        }
        else
        {
            Debug.Log("Touchscreen keyboard not supported.");
        }
    }

    private void Update()
    {
        if (TouchScreenKeyboard.isSupported && keyboard != null && keyboard.active && keyboard.text != "")
        {
            if (keyboardStatus == 1)
            {
                inputFieldAddress.text = keyboard.text;
            }
            else if (keyboardStatus == 2)
            {
                inputFieldPlayerName.text = keyboard.text;
                VRStaticVariables.playerName = inputFieldPlayerName.text;
            }
        }
    }

    // Invoked when the value of the text field changes.
    public void OnValueChangedName()
    {
        VRStaticVariables.playerName = inputFieldPlayerName.text;
    }
}

