using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Unity.XR.CoreUtils;
using System;
public class VRHostCameraControl : NetworkBehaviour
{
    
    public GameObject panel;
    private GameObject maincamera;
    public GameObject subcamera;
    public GameObject fovcamera;
    public GameObject Image_Mask;
    public GameObject Image_Mask1;
    public GameObject White_Circle;
    public Material simpleMaskMaterial;
    private Material originalImageMask1Material;
    private Material whiteCircleRuntimeMaterial;
    private Color mainCameraDefaultBgColor;
    private bool mainCameraBgCaptured;
    public GameObject fpsSelect;
    public GameObject maskSelect;
    public GameObject recordStart;
    public GameObject border;
    public GameObject recordTime;
    public GameObject recordSelect;
    public GameObject LeftHand;
    public GameObject RightHand;
    public GameObject Mark;
    public GameObject Box15;
    // Center-screen "playback completed" panel; assigned in scene, child of main Canvas.
    public GameObject replayCompletedText;
    // Captured at the start of each playback so subsequent restores honor the user's pre-playback layout.
    private bool leftHandActiveBeforePlay;
    private bool rightHandActiveBeforePlay;
    private bool replayHudHiddenForPlay;
    
    public List<GameObject> Boxes = new List<GameObject>();
    private TMP_Dropdown dropdownfps;
    public TMP_Text fpsText;
    private TMP_Dropdown mask;
    private TMP_Dropdown record;
    // The start time to count the current fps
    double startTime = 0;
    // Count how many frames have showed on the display 
    int count;
    // The time to play next frame on all clients
    private float nextFrameTime = 0.0f;
    private float nextCenterTime = 0.0f;
    private float nextDotTime = 0.0f;
    private float nextsecond = 0.0f;
    public Transform Centerposition;
    public ServerActionRecording actionrecord;
    // The index of record to play 
    private int recordtype = 0;
    // The index of the action in the record selected
    private int index = 0;
    // Check the condition of the record selected
    private bool play = false;
    private static int sizeOfPoints = 8;
    private List<Vector3> starts = new List<Vector3>(sizeOfPoints);
    private List<Vector3> ends = new List<Vector3>(sizeOfPoints);
    Vector3 start = new Vector3(-110f, -70f, -2f);
    Vector3 end = new Vector3(0, 0, 0);
    private List<Image> lines = new List<Image>();
    public Image lineImagePrefab;
    public Image DotImagePrefab;
    // private bool dotIsMoving;
    public Material lineMaterial;
    public float lineWidth = 20f;
    public float lineDisplayDuration = 0.9f;
    // Send the action of main camera to all clients
    private List<Vector3> SyncBoxPositions = new List<Vector3>(30);
    [SyncVar]
    private List<Vector3> BoxPositions = new List<Vector3>(30);

    private Vector3 Box15position;
    private List<Vector3> TempBoxpositions = new List<Vector3>();
    private Vector3 syncedPosition;
    private List<Quaternion> SyncBoxRotations = new List<Quaternion>(30);
    [SyncVar]
    private List<Quaternion> BoxRotations = new List<Quaternion>(30);
    private Vector3 TempPosition;
    //[SyncVar]
    private Quaternion Box15rotation;
    private List<Quaternion> TempBoxrotations = new List<Quaternion>();

    private List<Vector3> participantspos;
    private List<Quaternion> participantsrot;
    private Quaternion syncedRotation;

    private Quaternion TempRotation;

    private Quaternion PreRotation;

    private Quaternion PreMainRotation;
    
    private Quaternion PreRotationDot;

    private Quaternion PreMainRotationDot;

    private JsonSerializerSettings setting;
    private string filePathpapos;
    private string filePathparot;
    private float rotangle;

    private float intervalForCenterChange;
    private float intervalForDotChange;
    // Send the action of VR origin to all clients
    [SyncVar]
    private Vector3 ServerCenterposition;
    [SyncVar]
    private Quaternion ServerCenterRatation;
    // Send the fps message to all clients
    [SyncVar]
    private float frameRateInterval;
    // Send the masktype to all clients
    [SyncVar]
    private int masktype;

    public bool localReplayMode = false;
    private bool hostUiInitialized = false;
    private bool localReplayInitialized = false;

    // Dropdown index → internal mask code in ApplyMaskLocal switch.
    // Order: NoMask, SimpleMask, FovOnly, PointOnly, FullMask.
    // Keep legacy codes 1..4 reachable: re-add an entry here to surface them again.
    private readonly int[] dropdownToMaskCode = new int[] { 0, 6, 8, 7, 5 };


    void Start(){
        ResolveMainCamera();
        EnsureRuntimeMaskMaterials();

        // actionrecord = GameObject.FindObjectOfType<ServerActionRecording>().GetComponent<ServerActionRecording>();

        frameRateInterval = 1.0f / 3.0f;

        intervalForCenterChange = 1.0f / 3.0f;

        intervalForDotChange = 1.0f / 60.0f;

        // dotIsMoving = true;

        rotangle = 0.0f;
        
        for(int i = 0; i < 30; i++){
            TempBoxpositions.Add(new Vector3(0, 0, 0));
            TempBoxrotations.Add(new Quaternion(0f, 0f, 0f, 0f));
        }

        starts.Add(new Vector3(-120f, -75f, -2f));
        starts.Add(new Vector3(120f, -75f, -2f));
        starts.Add(new Vector3(-120f, 75f, -2f));
        starts.Add(new Vector3(120f, 75f, -2f));
        starts.Add(new Vector3(-120f, 0f, -2f));
        starts.Add(new Vector3(120f, 0f, -2f));
        starts.Add(new Vector3(0f, -75f, -2f));
        starts.Add(new Vector3(0f, 75f, -2f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));
        ends.Add(new Vector3(0f, 0f, 0f));

        //fpsText = GameObject.FindGameObjectWithTag("Fpsdisplay").GetComponent<TMP_Text>();

        if(isServer){
            for(int i = 0; i < 30; i++){
                SyncBoxPositions.Add(Boxes[i].transform.position);
                SyncBoxRotations.Add(Boxes[i].transform.rotation);
                //Debug.Log(BoxPositions[i]);
            }
            InitHostLikeUI();
            // Active the Border trigger (server-only: ActiveBorder is [ClientRpc])
            border.SetActive(true);
            border.GetComponent<Button>().onClick.AddListener(ActiveBorder);
        }
        if (isClient && !isServer) {
            InitClientLikePlayback();
        }
    }

    // RawImage.material does NOT auto-instance like Renderer.material — it returns the shared
    // .mat asset directly. ChangetheCenterSize calls SetFloat on these two materials every
    // frame in masktype 5/8, which would otherwise serialize residual _RadiusX/_RadiusY drift
    // back to LimFov3.mat / WhiteCircle.mat on disk after every Play session.
    // The cached originalImageMask1Material is also the material ApplyMaskLocal restores when
    // swapping out of SimpleMask, so cloning that one reference covers all swap paths.
    private void EnsureRuntimeMaskMaterials()
    {
        if (Image_Mask1 != null && originalImageMask1Material == null)
        {
            var raw = Image_Mask1.GetComponent<RawImage>();
            if (raw != null && raw.material != null)
            {
                originalImageMask1Material = new Material(raw.material);
                raw.material = originalImageMask1Material;
            }
        }
        if (White_Circle != null && whiteCircleRuntimeMaterial == null)
        {
            var raw = White_Circle.GetComponent<RawImage>();
            if (raw != null && raw.material != null)
            {
                whiteCircleRuntimeMaterial = new Material(raw.material);
                raw.material = whiteCircleRuntimeMaterial;
            }
        }
    }

    void OnDestroy()
    {
        if (originalImageMask1Material != null) { Destroy(originalImageMask1Material); }
        if (whiteCircleRuntimeMaterial != null) { Destroy(whiteCircleRuntimeMaterial); }
    }

    private void InitHostLikeUI()
    {
        if (hostUiInitialized) { return; }
        if (!ResolveMainCamera()) { return; }

        // Capture maincamera's default bg color so SimpleMask can temporarily flip it to black
        // (kills the blue leakage past Image_Mask1's quad) and other modes restore the default.
        if (!mainCameraBgCaptured && maincamera != null)
        {
            var cam = maincamera.GetComponent<Camera>();
            if (cam != null)
            {
                mainCameraDefaultBgColor = cam.backgroundColor;
                mainCameraBgCaptured = true;
            }
        }

        // Active the fps controller
        fpsSelect.SetActive(true);
        // Active the mask controller
        maskSelect.SetActive(true);
        // Active the RecordButton
        recordStart.SetActive(true);
        // Active the RecordText
        recordTime.SetActive(true);
        // Initiate the fpsdropdown
        dropdownfps = fpsSelect.GetComponent<TMP_Dropdown>();
        // Initiate the maskdropdown
        mask = maskSelect.GetComponent<TMP_Dropdown>();
        // Initiate the recorddropdown
        record = recordSelect.GetComponent<TMP_Dropdown>();

        PreRotation = subcamera.transform.rotation;
        PreRotationDot = subcamera.transform.rotation;
        PreMainRotation = maincamera.transform.rotation;
        PreMainRotationDot = maincamera.transform.rotation;

        masktype = DropdownIndexToMaskCode(mask.value);

        dropdownfps.onValueChanged.AddListener(delegate {
            DropdownValueChanged(dropdownfps);
        });

        mask.onValueChanged.AddListener(delegate {
            int code = DropdownIndexToMaskCode(mask.value);
            masktype = code;
            if (localReplayMode)
            {
                ApplyMaskLocal(code);
                // ApplyMaskLocal -> SetCameraToLimitFov(false) disables main TPD for
                // mask 0/1/2/4/5. In idle state we need it back so the user can look around.
                if (!play) { SetMainCameraTrackedPose(true); }
            }
            else
            {
                UpdateClientMask(code);
            }
        });
        hostUiInitialized = true;
    }

    private int DropdownIndexToMaskCode(int idx)
    {
        if (idx < 0 || idx >= dropdownToMaskCode.Length) { return 0; }
        return dropdownToMaskCode[idx];
    }

    private void InitClientLikePlayback()
    {
        if (!ResolveMainCamera()) { return; }

        TrackedPoseDriver trackedPoseDriver = maincamera.GetComponent<TrackedPoseDriver>();
        if (trackedPoseDriver != null)
        {
            trackedPoseDriver.enabled = false;
        }
        TempPosition = maincamera.transform.position;
        TempRotation = maincamera.transform.rotation;

        participantspos = new List<Vector3>();
        participantsrot = new List<Quaternion>();
        filePathpapos = Path.Combine(Application.dataPath, "RecordData", "PaPosData.json");
        filePathparot = Path.Combine(Application.dataPath, "RecordData", "PaRotData.json");
        var directoryPath = Path.GetDirectoryName(filePathpapos);
        setting = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public void InitLocalReplay()
    {
        if (localReplayInitialized) { return; }

        localReplayMode = true;
        EnsureReplayTargetsActive();
        InitClientLikePlayback();
        // Idle state in local replay should let HMD drive the view; InitClientLikePlayback
        // disabled the TrackedPoseDriver for real-client semantics, override that here.
        SetMainCameraTrackedPose(true);
        InitHostLikeUI();
        // Hide record-only UI: recording is disabled in local replay mode (Toggle listener
        // is registered only in the if(isServer) branch of ServerActionRecording).
        if (recordStart != null) { recordStart.SetActive(false); }
        if (recordTime != null) { recordTime.SetActive(false); }
        // Author-time stray "active" state on the completion panel would otherwise show before
        // the user has even played anything.
        if (replayCompletedText != null) { replayCompletedText.SetActive(false); }
        localReplayInitialized = true;
    }

    private void SetMainCameraTrackedPose(bool enable)
    {
        if (!ResolveMainCamera()) { return; }
        var tpd = maincamera.GetComponent<TrackedPoseDriver>();
        if (tpd == null) { return; }
        tpd.enabled = enable;
        // SetCameraToLimitFov leaves trackingType at RotationOnly; restore full pose so
        // idle local-replay users can both turn their head and walk freely.
        if (enable)
        {
            tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        }
    }

    private bool ResolveMainCamera()
    {
        if (maincamera != null) { return true; }

        maincamera = GameObject.FindWithTag("MainCamera");
        if (maincamera == null)
        {
            Debug.LogError("VRHostCameraControl: MainCamera-tagged object was not found.");
            return false;
        }
        return true;
    }

    public void StopLocalReplay()
    {
        localReplayMode = false;
        play = false;
        index = 0;
        SetMainCameraTrackedPose(true);
    }

    private void EnsureReplayTargetsActive()
    {
        ActivateObjectAndAncestors(gameObject);

        if (Boxes != null)
        {
            for (int i = 0; i < Boxes.Count; i++)
            {
                ActivateObjectAndAncestors(Boxes[i]);
            }
        }

        if (actionrecord != null && actionrecord.Boxtrasforms != null)
        {
            for (int i = 0; i < actionrecord.Boxtrasforms.Count; i++)
            {
                Transform boxTransform = actionrecord.Boxtrasforms[i];
                if (boxTransform != null)
                {
                    ActivateObjectAndAncestors(boxTransform.gameObject);
                }
            }
        }
    }

    private static void ActivateObjectAndAncestors(GameObject target)
    {
        if (target == null) { return; }

        Transform current = target.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    void Update()
    {
        if (isServer)
        {

            Countfps();
            
            // Get the action of main camera from the Server
            SyncTransform(maincamera.transform.position, maincamera.transform.rotation);
            for(int i = 0; i < 30; i++){
                SyncBoxPositions[i] = Boxes[i].transform.position;
                SyncBoxRotations[i] = Boxes[i].transform.rotation;
            }
            SyncBoxTransforms(SyncBoxPositions, SyncBoxRotations);
            //syncedPosition = maincamera.transform.position;
            //syncedRotation = maincamera.transform.rotation;
            //PreRotation = maincamera.transform.rotation;

            // Get the action of VR origin from the Server
            ServerCenterposition = Centerposition.position;
            ServerCenterRatation = Centerposition.rotation;

        }
        if ((isClient && !isServer) || localReplayMode) // playback runs on pure client OR local replay mode
        {
            // In local replay mode there is no host syncing camera/box transforms.
            // Stay idle until user clicks PlaytheRecord (sets play=true).
            if (localReplayMode && !play)
            {
                // Sub/Fov cameras have RotationOnly TPDs; without an external position
                // driver they would render from stale world positions (mask 4 fails to
                // stereo-fuse, mask 5 peripheral view drifts outside the room). Mirror
                // the HMD-driven maincamera position so both render the observer's view.
                if (maincamera != null)
                {
                    Vector3 headPos = maincamera.transform.position;
                    if ((masktype == 8 || masktype == 7 || masktype == 5) && subcamera != null && subcamera.activeInHierarchy)
                    {
                        subcamera.transform.position = headPos;
                    }
                    if (masktype == 4 && fovcamera != null && fovcamera.activeInHierarchy)
                    {
                        fovcamera.transform.position = headPos;
                    }
                }
                return;
            }
            if (localReplayMode && (actionrecord == null || !actionrecord.HasReplayData))
            {
                play = false;
                SetMainCameraTrackedPose(true);
                OnLocalReplayEnded(false);
                return;
            }
            //SyncBoxTransforms(Boxes);
            if (Time.time < nextFrameTime)
            {
                // Control the action of the Clients' main camera by the data from the Server, make sure the Client Camera not move between two frames
                if(masktype == 3){
                    subcamera.transform.position = TempPosition;
                    subcamera.transform.rotation = TempRotation;
                    maincamera.transform.position = TempPosition;
                    //maincamera.transform.rotation = syncedRotation;
                }else if(masktype == 8 || masktype == 7 || masktype == 5){

                    maincamera.transform.position = TempPosition;
                    maincamera.transform.rotation = TempRotation;
                    subcamera.transform.position = new Vector3(TempPosition.x + 6.5f, TempPosition.y, TempPosition.z + 42f);

                }else{
                    maincamera.transform.position = TempPosition;
                    maincamera.transform.rotation = TempRotation;
                }
                if(play == true){
                    participantspos.Add(TempPosition);
                    participantsrot.Add(subcamera.transform.rotation);
                }
                for(int i = 0; i < 30; i++){
                    Boxes[i].transform.position = TempBoxpositions[i];
                    Boxes[i].transform.rotation = TempBoxrotations[i];
                }
                
                
                
                //return;
                
            }else{
                if(play == true && actionrecord != null && actionrecord.NumOfRecords() > 0){
                    // Control the action of the Clients' main camera by the data from the Server
                    TempPosition = actionrecord.GetCamerapositions(recordtype, index);
                    TempRotation = actionrecord.GetCamerarotations(recordtype, index);
                    TempBoxpositions = actionrecord.GetBoxpositions(recordtype, index);
                    TempBoxrotations = actionrecord.GetBoxrotations(recordtype, index);
                    if (!HasCompleteBoxFrame(TempBoxpositions, TempBoxrotations))
                    {
                        Debug.LogWarning("LocalReplay: invalid box frame data, stopping playback.");
                        play = false;
                        if (localReplayMode)
                        {
                            SetMainCameraTrackedPose(true);
                            OnLocalReplayEnded(false);
                        }
                        return;
                    }
                    // Debug.Log(TempBoxpositions[14]);
                    if(masktype == 3){
                        subcamera.transform.position = TempPosition;
                        subcamera.transform.rotation = TempRotation;
                        maincamera.transform.position = TempPosition;
                    }else if(masktype == 8 || masktype == 7 || masktype == 5){
                        maincamera.transform.position = TempPosition;
                        maincamera.transform.rotation = TempRotation;
                        subcamera.transform.position = new Vector3(TempPosition.x + 6.5f, TempPosition.y, TempPosition.z + 42f);
                    }
                    participantspos.Add(TempPosition);
                    participantsrot.Add(subcamera.transform.rotation);
                    // Debug.Log(TempBoxpositions[0]);
                    for(int i = 0; i < 30; i++){
                        Boxes[i].transform.position = TempBoxpositions[i];
                        Boxes[i].transform.rotation = TempBoxrotations[i];
                    }     
                    // Control the action of the Clients' VR origin by the data from the Server
                    // Centerposition.position = ServerCenterposition;
                    // Centerposition.rotation = ServerCenterRatation;

                    // Read the record action data with the interval of fps
                    index += ReplayFrameStep();

                    // If read all the action of one record, stop reading the record
                    if(index >= actionrecord.Numofactions(recordtype)){
                        index = 0;
                        if (!localReplayMode)
                        {
                            SavePaPosData(participantspos, filePathpapos);
                            SavePaRotData(participantsrot, filePathparot);
                        }
                        participantspos = new List<Vector3>();
                        participantsrot = new List<Quaternion>();
                        play = false;
                        if (localReplayMode)
                        {
                            SetMainCameraTrackedPose(true);
                            OnLocalReplayEnded(true);
                        }
                    }
                }else{
                    if (localReplayMode)
                    {
                        play = false;
                        SetMainCameraTrackedPose(true);
                        OnLocalReplayEnded(false);
                        return;
                    }
                    // Control the action of the Clients' VR origin by the data from the Server
                    // Centerposition.position = ServerCenterposition;
                    // Centerposition.rotation = ServerCenterRatation;

                    // Control the action of the Clients' main camera by the data from the Server
                    if(masktype == 3){
                        subcamera.transform.position = syncedPosition;
                        subcamera.transform.rotation = syncedRotation;
                        maincamera.transform.position = syncedPosition; 
                        //maincamera.transform.rotation = syncedRotation;
                    }
                    else if(masktype == 8 || masktype == 7 || masktype == 5){
                        //ChangetheCenterAndAttach();
                        // Update the camera data
                        maincamera.transform.position = syncedPosition;
                        maincamera.transform.rotation = syncedRotation;
                        subcamera.transform.position = new Vector3(syncedPosition.x + 6.5f, syncedPosition.y, syncedPosition.z + 42f);
                    }
                    for(int i = 0; i < 30; i++){
                        
                        TempBoxpositions[i] = BoxPositions[i];
                        TempBoxrotations[i] = BoxRotations[i];
                        Boxes[i].transform.position = TempBoxpositions[i];
                        Boxes[i].transform.rotation = TempBoxrotations[i];
                    }
                    
                    TempPosition = syncedPosition;
                    TempRotation = syncedRotation;
                }
                nextFrameTime = Time.time + frameRateInterval;
                
                // Initiate the start point
                if(nextsecond <= nextFrameTime - 1.5f){
                    nextsecond = nextFrameTime;
                    //start = new Vector3(-110f, -70f, -2f);
                    starts[0] = new Vector3(-120f, -75f, -2f);
                    starts[1] = new Vector3(120f, -75f, -2f);
                    starts[2] = new Vector3(-120f, 75f, -2f);
                    starts[3] = new Vector3(120f, 75f, -2f);
                    starts[4] = new Vector3(-120f, 0f, -2f);
                    starts[5] = new Vector3(120f, 0f, -2f);
                    starts[6] = new Vector3(0f, -75f, -2f);
                    starts[7] = new Vector3(0f, 75f, -2f);
                    ends[0] = new Vector3(0f, 0f, 0f);
                    ends[1] = new Vector3(0f, 0f, 0f);
                    ends[2] = new Vector3(0f, 0f, 0f);
                    ends[3] = new Vector3(0f, 0f, 0f);
                    ends[4] = new Vector3(0f, 0f, 0f);
                    ends[5] = new Vector3(0f, 0f, 0f);
                    ends[6] = new Vector3(0f, 0f, 0f);
                    ends[7] = new Vector3(0f, 0f, 0f);
                    lines.ForEach(img => Destroy(img.gameObject));
                    lines.Clear();
                    //end = new Vector3(0, 0, 0);
                    //lines.ForEach(img => Destroy(img.gameObject));
                    //lines.Clear();
                }
                Countfps();       
            }
            if(Time.time >= nextCenterTime){
                nextCenterTime = Time.time + intervalForCenterChange;
                // FovOnly + FullMask drive the central ellipse animation; PointOnly hides the
                // ellipse RawImage so the size update would be wasted work.
                if(masktype == 8 || masktype == 5){
                    ChangetheCenterSize();
                }
            }
            if(Time.time >= nextDotTime){
                nextDotTime = Time.time + intervalForDotChange;
                // PointOnly + FullMask draw the moving dots; FovOnly intentionally omits them.
                if(masktype == 7 || masktype == 5){
                    ChangetheDotPosition();
                }
            }
        }          
    }
    public void SavePaPosData(List<Vector3> data, string path)
    {   
        string json = JsonConvert.SerializeObject(data, setting);
        File.WriteAllText(path, json);
    }
    public void SavePaRotData(List<Quaternion> data, string path)
    {   
        string json = JsonConvert.SerializeObject(data, setting);
        File.WriteAllText(path, json);
    }
    //Change the condition of the Border
    [ClientRpc]
    void ActiveBorder(){
        if(isClient && !isServer){
            if(White_Circle.GetComponent<RawImage>().enabled){
                White_Circle.SetActive(false);
                White_Circle.GetComponent<RawImage>().enabled = false;
            }else{
                White_Circle.SetActive(true);
                White_Circle.GetComponent<RawImage>().enabled = true;
            }
        }
    }
    // Change the position of the points
    private void ChangetheDotPosition(){
        // Get the rotation of main and sub camera
        Quaternion qsubvari = Quaternion.Inverse(PreRotationDot)*subcamera.transform.rotation;
        Quaternion qmainvari = Quaternion.Inverse(PreMainRotationDot)*TempRotation;

        // Get the rotation between the main and sub camera(with direction)
        Quaternion qvarui = Quaternion.Inverse(qsubvari) * qmainvari;
        //Vector3 eulerRotation = qvarui.eulerAngles;
        Vector3 eulerRotation = qvarui * Vector3.forward;
        //float uiRotation = eulerRotation.z + eulerRotation.x * Mathf.Sign(Mathf.Cos(eulerRotation.y * Mathf.Deg2Rad));

        // Get the projection of rotation on xy plane
        eulerRotation.z = 0;
        eulerRotation.Normalize();
        float uiRotation = Mathf.Atan2(eulerRotation.y, eulerRotation.x) * Mathf.Rad2Deg;

        // Get the rotation angle between main and sub camera(no direction)
        rotangle = CalMotionAngle(qsubvari, qmainvari);
        // Debug.Log(rotangle);

        // Update the camera data
        PreRotationDot = subcamera.transform.rotation;
        PreMainRotationDot = TempRotation;

        // Draw the line when the angle between main and sub camera is over 10. 
        if(rotangle >= 2.7 ){
            // Mark.SetActive(true);
            // Mark.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0.0f, 0.0f, uiRotation + 180);

            // Calculate the end point of the line
            // end = CalculateEndPoint(start, uiRotation + 180, rotangle / 2);
            Vector3 plus = CalculateEndPointToPlus(uiRotation, rotangle * 0.8f + 6 / rotangle);
            for(int i = 0; i < sizeOfPoints; i++)
            {
                ends[i] = new Vector3(starts[i].x + plus.x, starts[i].y + plus.y, starts[i].z);
            }
            // Debug.Log(end + " " + starts[0]);
            // Draw the line with the two points 
            // DrawLineInMask(start, end, uiRotation + 180);

            // Draw the dot with the two points 
            DrawDotInMask();
            // Reset the start point
            for(int i = 0; i < sizeOfPoints; i++)
            {
                starts[i] = new Vector3(ends[i].x, ends[i].y, ends[i].z);
            }
            start.Set(end.x, end.y, end.z);

            // Mark.GetComponent<RectTransform>().localRotation = new Quaternion(0.0f, 0.0f, qvarui.x +  qvarui.z, qvarui.w);
            // Mark.GetComponent<RectTransform>().localRotation.eulerAngles = new Vector3(0, 0,qvarui.z);
            // Mark.GetComponent<RectTransform>().sizeDelta = new Vector2(Math.Abs(rotangle) / 2, 10);
        }else{
            // Mark.SetActive(false);
            //lines.ForEach(img => Destroy(img.gameObject));
            //lines.Clear();
        }
    } 
    // Arrange the size of the center elliptic
    private void ChangetheCenterSize(){
        // Get the rotation of main and sub camera
        Quaternion qsubvari = Quaternion.Inverse(PreRotation)*subcamera.transform.rotation;
        Quaternion qmainvari = Quaternion.Inverse(PreMainRotation)*TempRotation;

        // Get the rotation between the main and sub camera(with direction)
        Quaternion qvarui = Quaternion.Inverse(qsubvari) * qmainvari;
        //Vector3 eulerRotation = qvarui.eulerAngles;
        Vector3 eulerRotation = qvarui * Vector3.forward;
        //float uiRotation = eulerRotation.z + eulerRotation.x * Mathf.Sign(Mathf.Cos(eulerRotation.y * Mathf.Deg2Rad));

        // Get the projection of rotation on xy plane
        eulerRotation.z = 0;
        eulerRotation.Normalize();
        float uiRotation = Mathf.Atan2(eulerRotation.y, eulerRotation.x) * Mathf.Rad2Deg;

        // Get the rotation angle between main and sub camera(no direction)
        rotangle = CalMotionAngle(qsubvari, qmainvari);
        // Debug.Log(rotangle);

        // Update the camera data
        PreRotation = subcamera.transform.rotation;
        PreMainRotation = TempRotation;

        
        Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusX", -2.0f / 1500.0f * Math.Abs(rotangle) + 0.24f);
        Image_Mask1.GetComponent<RawImage>().material.SetFloat("_RadiusY", -2.0f / 1500.0f * Math.Abs(rotangle) + 0.19f);
        White_Circle.GetComponent<RawImage>().material.SetFloat("_RadiusX", -2.0f / 1500.0f * Math.Abs(rotangle) + 0.24f);
        White_Circle.GetComponent<RawImage>().material.SetFloat("_RadiusY", -2.0f / 1500.0f * Math.Abs(rotangle) + 0.19f);
    }
    float CalMotionAngle(Quaternion q1, Quaternion q2)
    {
        // Calulate the angle between two Quaternions
        Vector3 from = q1 * Vector3.forward;
        Vector3 to = q2 * Vector3.forward;
        float angleInDegrees = Vector3.Angle(from, to);

        // Use the cross product to calculate the direction of the rotation
        // Vector3 crossProduct = Vector3.Cross(from, to);
        // float sign = Mathf.Sign(crossProduct.x + crossProduct.y + crossProduct.z);

        // Nomalize the angle
        // angleInDegrees /= frameRateInterval * 3;
        // angleInDegrees = Mathf.Ceil(angleInDegrees / 10) * 10;

        return angleInDegrees;
    }
    Vector3 CalculateEndPoint(Vector3 start, float angle, float distance)
    {
        // Change the angle to arc
        float angleInRadians = angle * Mathf.Deg2Rad;

        // Calulate the coordinate of the end point
        float x = start.x + distance * Mathf.Cos(angleInRadians);
        float y = start.y + distance * Mathf.Sin(angleInRadians);

        // If the line is on a panel, the data of z will not change
        return new Vector3(x, y, start.z);
    }

    Vector3 CalculateEndPointToPlus(float angle, float distance)
    {
        // Change the angle to arc
        float angleInRadians = angle * Mathf.Deg2Rad;

        // Calulate the distance between the start and the end point
        float x = distance * Mathf.Cos(angleInRadians);
        float y = distance * Mathf.Sin(angleInRadians);

        // If the line is on a panel, the data of z will not change
        return new Vector3(x, y, 0);
    }

    // Draw the line between the two points
    private void DrawLineInMask(Vector3 start, Vector3 end, float angle)
    {
        // Create a new line object
        Image line = Instantiate(lineImagePrefab);

        lines.Add(line);

        // Set the Background_mask as the parent object
        line.transform.SetParent(Image_Mask1.transform, false);

        // Get the RectTransform to set the line
        RectTransform lineRectTransform = line.GetComponent<RectTransform>();

        // Set the distance of the line
        float distance = Vector3.Distance(start, end);
        lineRectTransform.sizeDelta = new Vector2(distance, 5f); // 5f is the width of the line 

        // Set the transform of the middle of the two point as the position of the line
        Vector3 midpoint = (start + end) / 2f;
        lineRectTransform.localPosition = midpoint;

        // Set the rotation of the line
        lineRectTransform.localRotation = Quaternion.Euler(0, 0, angle);

        // Set the duration time of the line
        // StartCoroutine(RemoveLineAfterDelay(line, lineDisplayDuration));
    }
    private void DrawDotInMask(){

        
        // StopAllCoroutines();
        lines.ForEach(img => Destroy(img.gameObject));
        lines.Clear();
        // dotIsMoving = true;
        
        for(int i = 0; i < sizeOfPoints; i++){
            // Create a new line object
            Image dot = Instantiate(DotImagePrefab);

            lines.Add(dot);

            // Set the Background_mask as the parent object
            dot.transform.SetParent(Image_Mask1.transform, false);

            // Get the RectTransform to set the dot
            RectTransform dotRectTransform = dot.GetComponent<RectTransform>();
            dotRectTransform.sizeDelta = new Vector2(10f, 10f);
        }
        for(int i = 0; i < sizeOfPoints; i++){
            // Move the dots with the line start-end
            lines[i].rectTransform.localPosition = ends[i];
        }
        //StartCoroutine(MoveDotCoroutine());
    }
    private IEnumerator MoveDotCoroutine(){
        float elapsedTime = 0f;

        if (lines.Count < sizeOfPoints || starts.Count != sizeOfPoints || ends.Count != sizeOfPoints)
        {
            Debug.LogError("Invalid data: Ensure lines, starts, and ends have the same count as sizeOfPoints.");
            yield break;
        }
        while(lines.Count >= sizeOfPoints && elapsedTime < intervalForDotChange){
            float t = elapsedTime / intervalForDotChange;
            for(int i = 0; i < sizeOfPoints; i++){
                // Move the dots with the line start-end
                lines[i].rectTransform.localPosition = Vector3.Lerp(starts[i], ends[i], t);
            }

            elapsedTime += Time.deltaTime;

            yield return new WaitForSeconds(intervalForDotChange / 2);
        }
        lines.ForEach(img => Destroy(img.gameObject));
        lines.Clear();
    }
    // Remove the line after specify time
    private IEnumerator RemoveLineAfterDelay(Image lineRenderer, float duration)
    {
        // Wait for specify time
        yield return new WaitForSeconds(duration);

        // Make the line invisible
        lineRenderer.enabled = false;

        // Remove the line from the list and destroy the object
        lines.Remove(lineRenderer);
        Destroy(lineRenderer.gameObject);
    }
    void DropdownValueChanged(TMP_Dropdown change)
    {
        // Change the interval between two frames
        switch(change.value){
            case 0:
                frameRateInterval = 1.0f / 3.0f;
                break;
            case 1:
                frameRateInterval = 1.0f / 5.0f;
                break;
            case 2:
                frameRateInterval = 1.0f / 17.0f;
                break;
            case 3:
                frameRateInterval = 1.0f / 25.0f;
                break;
            case 4:
                frameRateInterval = 1.0f / 35.0f;
                break;
            default:
                break;
        }

    }
    void Countfps(){
        ++count;
        if (Time.time >= startTime + 1)
        {
            float fps = count;
            startTime = Time.time;
            count = 0;
            //fpsText.text = $"{fps:0.} fps";
        }
    }
    // Hide / restore the floating UI (FPS / Mask / Record dropdowns) during local replay playback.
    // Idempotent: re-entry is safe.
    private void SetReplayHudVisible(bool visible)
    {
        if (fpsSelect    != null) { fpsSelect.SetActive(visible); }
        if (maskSelect   != null) { maskSelect.SetActive(visible); }
        if (recordSelect != null) { recordSelect.SetActive(visible); }
    }

    // Toggle observer's XR controllers. SetActive(false) takes down ray + line visual + interactor
    // in one shot without reaching into the nested XR Origin prefab.
    private void SetObserverControllersVisible(bool visible)
    {
        if (LeftHand  != null) { LeftHand .SetActive(visible ? leftHandActiveBeforePlay  : false); }
        if (RightHand != null) { RightHand.SetActive(visible ? rightHandActiveBeforePlay : false); }
    }

    // Single end-of-playback handler. Called from the natural completion path AND every
    // early-bail-out branch so the HUD always recovers when `play` transitions true -> false.
    private void OnLocalReplayEnded(bool showCompletedMessage)
    {
        if (!localReplayMode) { return; }
        if (replayHudHiddenForPlay)
        {
            SetReplayHudVisible(true);
            SetObserverControllersVisible(true);
            replayHudHiddenForPlay = false;
        }
        if (showCompletedMessage && replayCompletedText != null)
        {
            replayCompletedText.SetActive(true);
        }
    }

    // Play the record
    public void PlaytheRecord(){
        if (localReplayMode)
        {
            if (record == null || actionrecord == null || !actionrecord.HasReplayData)
            {
                Debug.LogWarning("LocalReplay: no replay data loaded.");
                return;
            }

            if (record.value < 0 || record.value >= actionrecord.NumOfRecords())
            {
                Debug.LogWarning("LocalReplay: selected record index is invalid.");
                return;
            }

            recordtype = record.value;
            play = true;
            index = 0;

            // Snapshot controller state once so SetObserverControllersVisible(true) later honors
            // whatever the user (or mask code) had configured before playback started.
            leftHandActiveBeforePlay  = LeftHand  != null && LeftHand.activeSelf;
            rightHandActiveBeforePlay = RightHand != null && RightHand.activeSelf;
            SetReplayHudVisible(false);
            SetObserverControllersVisible(false);
            replayHudHiddenForPlay = true;
            if (replayCompletedText != null) { replayCompletedText.SetActive(false); }

            EnsureReplayTargetsActive();
            // Playback owns the maincamera transform; let JSON drive it without HMD fighting it.
            SetMainCameraTrackedPose(false);
        }
        else
        {
            Rectherecordtype(record.value, true, 0);
        }
    }

    private int ReplayFrameStep()
    {
        return Mathf.Max(1, Mathf.RoundToInt(15.0f * frameRateInterval));
    }

    private bool HasCompleteBoxFrame(List<Vector3> positions, List<Quaternion> rotations)
    {
        int required = Boxes != null ? Boxes.Count : 0;
        return required > 0 && positions != null && rotations != null &&
            positions.Count >= required && rotations.Count >= required;
    }
    [ClientRpc]
    void UpdateClientMask(int type)
    {
        if (isClient && !isServer){
            ApplyMaskLocal(type);
        }
    }

    private void ApplyMaskLocal(int type)
    {
        // Reset maincamera bg to the captured default; case 6 overrides to black so SimpleMask's
        // periphery beyond Image_Mask1's quad shows black instead of the default blue clear color.
        if (mainCameraBgCaptured) { SetMainCameraBgColor(mainCameraDefaultBgColor); }

        // ----- Cases below are ordered to match the MaskSelect dropdown (see dropdownToMaskCode).
        // ----- Dropdown order: NoMask(0) → SimpleMask(6) → FovOnly(8) → PointOnly(7) → FullMask(5).
        // ----- Legacy cases 1..4 are kept at the bottom — unreachable via the current dropdown
        // ----- but preserved for future reuse / experiments.
        switch(type){
            // Dropdown index 0 — NoMask: no overlay, no FOV change.
            case 0:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(false);
                break;
            // Dropdown index 1 — SimpleMask: central clear ellipse + pure-black periphery on the
            // Image_Mask1 quad. Reuses Image_Mask1 with simpleMaskMaterial whose shader mirrors
            // TransparentCircle (Opaque + discard inside ellipse, opaque black outside) — same
            // render path as FovOnly so the ellipse is geometrically identical and stereo-symmetric.
            // No FOV change, no subcamera, no Vignette.
            case 6:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(false);
                // SetCameraToLimitFov2(false) deactivates Image_Mask1; reactivate the GameObject.
                Image_Mask1.SetActive(true);
                SetImageMask1State(true, simpleMaskMaterial);
                // Flip maincamera clear color to black so any area beyond Image_Mask1's quad
                // (visible when the eye is close to the HMD lens) shows black, not the default blue.
                SetMainCameraBgColor(Color.black);
                break;
            // Dropdown index 2 — FovOnly: same as FullMask but the dot drawing is gated off in Update().
            case 8:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(true);
                SetImageMask1State(true, originalImageMask1Material);
                break;
            // Dropdown index 3 — PointOnly: same dot system as FullMask, but no center ellipse and no FOV shrink.
            case 7:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(true);
                // Undo SetCameraToLimitFov2's FOV change so the main view is unaffected.
                maincamera.GetComponent<Camera>().fieldOfView = 60f;
                // Hide the central ellipse but keep the GameObject active so dot Images parented
                // under it still render. Restore material so next swap to SimpleMask is clean.
                SetImageMask1State(false, originalImageMask1Material);
                break;
            // Dropdown index 4 — FullMask: original mask 5 (LimitFov3) — central ellipse + dots + FOV shrink.
            case 5:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(true);
                // Restore RawImage (PointOnly disables it) and material (SimpleMask swaps it).
                SetImageMask1State(true, originalImageMask1Material);
                break;

            // ----- Legacy: not surfaced by the current dropdown. -----
            // Old MaskFull — full-screen URP Volume post-processing.
            case 1:
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov2(false);
                panel.GetComponent<Volume>().enabled = true;
                break;
            // Old MaskTrans — flat translucent overlay panel image.
            case 2:
                panel.GetComponent<Volume>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(false);
                panel.GetComponent<UnityEngine.UI.Image>().enabled = true;
                break;
            // Old LimitFov — Image_Mask + main TPD set to RotationOnly.
            case 3:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov1(false);
                SetCameraToLimitFov2(false);
                SetCameraToLimitFov(true);
                break;
            // Old LimitFov2 — fovcamera + camera rect crop (mono only).
            case 4:
                panel.GetComponent<Volume>().enabled = false;
                panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                SetCameraToLimitFov(false);
                SetCameraToLimitFov2(false);
                SetCameraToLimitFov1(true);
                break;
            default:
                break;
        }
    }

    private void SetImageMask1State(bool rawEnabled, Material material)
    {
        if (Image_Mask1 == null) { return; }
        var raw = Image_Mask1.GetComponent<RawImage>();
        if (raw == null) { return; }
        raw.enabled = rawEnabled;
        if (material != null) { raw.material = material; }
    }

    private void SetMainCameraBgColor(Color color)
    {
        if (maincamera == null) { return; }
        var cam = maincamera.GetComponent<Camera>();
        if (cam != null) { cam.backgroundColor = color; }
    }
    void SetCameraToLimitFov(bool flag){
        //subcamera.SetActive(flag);
        Image_Mask.SetActive(flag);
        maincamera.GetComponent<TrackedPoseDriver>().enabled = flag;
        maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        LeftHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;
        RightHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;        
    }
    void SetCameraToLimitFov1(bool flag){
        fovcamera.SetActive(flag);
        var cam = maincamera.GetComponent<Camera>();
        // Camera.rect under XR single-pass stereo applies viewport cropping per eye without
        // adjusting the per-eye projection matrix, so left/right diverge and fail to fuse.
        // Combined with mismatched FOV (38.3 vs fovcamera's 60), the central/peripheral
        // stereo cues conflict. Skip the rect/FOV change when stereo is active and keep the
        // legacy mono behavior for non-stereo (e.g. ParrelSync client without HMD).
        bool stereoActive = UnityEngine.XR.XRSettings.enabled
                            && UnityEngine.XR.XRSettings.isDeviceActive;
        if(flag){
            if(!stereoActive){
                cam.rect = new Rect(0.2f, 0.2f, 0.6f, 0.6f);
                cam.fieldOfView = 38.3f;
            }
        }else{
            cam.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            cam.fieldOfView = 60f;
        }

    }
    void SetCameraToLimitFov2(bool flag){
        Image_Mask1.SetActive(flag);
        if(flag){
            subcamera.SetActive(flag);
            subcamera.GetComponent<TrackedPoseDriver>().enabled = flag;
            subcamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            maincamera.GetComponent<Camera>().fieldOfView = 50f;
            subcamera.GetComponent<Camera>().fieldOfView = 80f;
        }else{
            //subcamera.GetComponent<TrackedPoseDriver>().enabled = flag;
            subcamera.GetComponent<Camera>().fieldOfView = 80f;
            maincamera.GetComponent<Camera>().fieldOfView = 60f;
            //subcamera.SetActive(flag);

            //subcamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        }
        
        LeftHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;
        RightHand.GetComponent<ActionBasedControllerManager>().enabled = !flag;        
    }
     [ClientRpc]
    private void SyncBoxTransforms(List<Vector3> boxps, List<Quaternion> boxrs){
        BoxPositions = boxps;
        BoxRotations = boxrs;

    }
    [ClientRpc]
    private void SyncTransform(Vector3 pos, Quaternion rot){
        syncedPosition = pos;
        syncedRotation = rot;
    }
    
    // Initiate the record to play
    [ClientRpc]
    private void Rectherecordtype(int type, bool flag, int start){
        recordtype = type;
        play = flag;
        index = start;
    }
    
}
