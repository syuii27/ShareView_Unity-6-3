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
    
    
    void Start(){
        maincamera = GameObject.FindWithTag("MainCamera");

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
            // Active the fps controller 
            fpsSelect.SetActive(true);
            // Active the mask controller 
            maskSelect.SetActive(true);
            // Active the RecordButton
            recordStart.SetActive(true);
            // Active the RecordText
            recordTime.SetActive(true);
            // Active the Border trigger
            border.SetActive(true);
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
            
            masktype = mask.value;

            // Update the Client fps for all clients by the choice of the server 
            dropdownfps.onValueChanged.AddListener(delegate {
                DropdownValueChanged(dropdownfps);
            });

            // Update the Client mask for all clients by the choice of the server 
            mask.onValueChanged.AddListener(delegate {
                masktype = mask.value;
                UpdateClientMask(mask.value);
            });
            border.GetComponent<Button>().onClick.AddListener(ActiveBorder);
        }
        if (isClient && !isServer) {
            // Close the action control of the Client HMD
            // maincamera.GetComponent<TrackedPoseDriver>().trackingType = TrackedPoseDriver.TrackingType.PositionOnly;
            maincamera.GetComponent<TrackedPoseDriver>().enabled = false;
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
        if (isClient && !isServer) // Make sure the change is on clients
        {
            //SyncBoxTransforms(Boxes);
            if (Time.time < nextFrameTime)
            {
                // Control the action of the Clients' main camera by the data from the Server, make sure the Client Camera not move between two frames
                if(masktype == 3){
                    subcamera.transform.position = TempPosition;
                    subcamera.transform.rotation = TempRotation;
                    maincamera.transform.position = TempPosition; 
                    //maincamera.transform.rotation = syncedRotation;
                }else if(masktype == 5){
                    
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
                if(play == true && actionrecord.NumOfRecords() > 0){
                    // Control the action of the Clients' main camera by the data from the Server
                    TempPosition = actionrecord.GetCamerapositions(recordtype, index);
                    TempRotation = actionrecord.GetCamerarotations(recordtype, index);
                    TempBoxpositions = actionrecord.GetBoxpositions(recordtype, index);
                    TempBoxrotations = actionrecord.GetBoxrotations(recordtype, index);
                    // Debug.Log(TempBoxpositions[14]);
                    if(masktype == 3){
                        subcamera.transform.position = TempPosition;
                        subcamera.transform.rotation = TempRotation;
                        maincamera.transform.position = TempPosition;
                    }else if(masktype == 5){
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
                    index += (15 * frameRateInterval).ConvertTo<int>(); 

                    // If read all the action of one record, stop reading the record
                    if(index >= actionrecord.Numofactions(recordtype)){
                        index = 0;
                        SavePaPosData(participantspos, filePathpapos);
                        SavePaRotData(participantsrot, filePathparot);
                        participantspos = new List<Vector3>();
                        participantsrot = new List<Quaternion>();
                        play = false;
                    }
                }else{
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
                    else if(masktype == 5){
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
                if(masktype == 5){
                    ChangetheCenterSize();
                }
            }
            if(Time.time >= nextDotTime){
                nextDotTime = Time.time + intervalForDotChange;
                if(masktype == 5){
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
    // Play the record
    public void PlaytheRecord(){
    
        Rectherecordtype(record.value, true, 0);
    }
    [ClientRpc]
    void UpdateClientMask(int type)
    {
        if (isClient && !isServer){
            // Change the Mask of the Clients by the Choice Selected by the Server 
            switch(type){
                case 0:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(false);
                    break;
                case 1:
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov2(false);
                    panel.GetComponent<Volume>().enabled = true;              
                    break;
                case 2:
                    panel.GetComponent<Volume>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(false);
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = true;
                    break;
                case 3:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(false);
                    SetCameraToLimitFov(true);
                    break;
                case 4:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov2(false);
                    SetCameraToLimitFov1(true);
                    break;
                case 5:
                    panel.GetComponent<Volume>().enabled = false;
                    panel.GetComponent<UnityEngine.UI.Image>().enabled = false;
                    SetCameraToLimitFov(false);
                    SetCameraToLimitFov1(false);
                    SetCameraToLimitFov2(true);
                    break;
            default:
                    break;
            }
        }
        
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
        if(flag){
            maincamera.GetComponent<Camera>().rect = new Rect(0.2f, 0.2f, 0.6f, 0.6f);
            maincamera.GetComponent<Camera>().fieldOfView = 38.3f;
        }else{
            maincamera.GetComponent<Camera>().rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            maincamera.GetComponent<Camera>().fieldOfView = 60f;
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
