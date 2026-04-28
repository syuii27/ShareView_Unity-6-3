using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;
using Newtonsoft.Json;
using System.IO;
using Button = UnityEngine.UI.Button;
using System.Collections;
using UnityEngine.UIElements;
public class ServerActionRecording : NetworkBehaviour
{
    public GameObject recordButton;
    public GameObject stopButton;
    public GameObject syncButton;
    public GameObject recordSelect;
    public GameObject NetworkObject;
    public TMP_Text timeText;
    private TMP_Dropdown record;
    // [SyncVar]
    private bool isRecording = false;
    private float recordingTime = 0.0f;
    private float recordInterval = 1.0f / 15.0f;
    private float timer;
    public Transform serverCameraTransform;
    public List<Transform> Boxtrasforms;
    public Transform Centerposition;
    List<Vector3> CheckRecordbp = new List<Vector3>();
    List<Quaternion> CheckRecordbr = new List<Quaternion>();
    private List<Vector3> camerapositions;
    private List<List<Vector3>> Boxposts;
    private List<List<Quaternion>> Boxrotas;
    private List<Quaternion> camerarotations;
    private List<List<Vector3>> positionrecords;
    private List<List<List<Vector3>>> SynBoxpositions;
    private List<List<Quaternion>> rotationrecords;
    private List<List<List<Quaternion>>> SynBoxrotations;
    private string filePathboxp;
    private string filePathboxr;
    private string filePathp;
    private string filePathr;
    private JsonSerializerSettings setting;
    private int countRecord = 0;
    //[SyncVar]
    private List<List<Vector3>> Synpositionrecords;
    //[SyncVar]
    private List<List<List<Vector3>>> Boxpositions;
    //[SyncVar]
    private List<List<Quaternion>> Synrotationrecords;
    //[SyncVar]
    private List<List<List<Quaternion>>> Boxrotations;
    //[SyncVar]
    //private bool SynisRecording;
    

    void Start()
    {
        isRecording = false;
        
        if(isServer){
            syncButton.SetActive(true);
            recordButton.GetComponent<Button>().onClick.AddListener(Toggle);
            stopButton.GetComponent<Button>().onClick.AddListener(Toggle);
            syncButton.GetComponent<Button>().onClick.AddListener(SyncData);
            record = recordSelect.GetComponent<TMP_Dropdown>();
            
        }else if(isClient && !isServer){
            camerapositions = new List<Vector3>();
            camerarotations = new List<Quaternion>();
            Boxposts = new List<List<Vector3>>();
            Boxrotas = new List<List<Quaternion>>();
            positionrecords = new List<List<Vector3>>();
            rotationrecords = new List<List<Quaternion>>();
            Synpositionrecords = new List<List<Vector3>>();
            Synrotationrecords = new List<List<Quaternion>>();
            SynBoxpositions = new List<List<List<Vector3>>>();
            SynBoxrotations = new List<List<List<Quaternion>>>();
            Boxpositions = new List<List<List<Vector3>>>();
            Boxrotations = new List<List<List<Quaternion>>>();
                   
            
            timeText.text = "0.0s";
            filePathboxp = Path.Combine(Application.dataPath, "RecordData", "BoxPosData.json");
            filePathboxr = Path.Combine(Application.dataPath, "RecordData", "BoxRotData.json");
            filePathp = Path.Combine(Application.dataPath,"RecordData", "MainPosData.json");
            filePathr = Path.Combine(Application.dataPath,"RecordData", "MainRotData.json");

            var directoryPath = Path.GetDirectoryName(filePathboxp);
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
        if (isRecording && isServer)
        {
            
            recordingTime += Time.deltaTime;
            timeText.text = recordingTime.ToString("F1") + "s";
        }else if(isRecording && isClient && !isServer){

            timer += Time.deltaTime;

            // Record the action of the camera as the time interval
            if (timer >= recordInterval)
            {
                StartCoroutine(RecordRoutine());
            }
        }
    }
    private IEnumerator RecordRoutine(){
        List<Vector3> tempbp = new List<Vector3>();
        List<Quaternion> tempbr = new List<Quaternion>();
        bool flag = true;
        for(int i = 0; i < 30; i++){
            tempbp.Add(Boxtrasforms[i].position);
            tempbr.Add(Boxtrasforms[i].rotation);
        }
        // Debug.Log(tempbp[0]);
        if(flag){
            camerapositions.Add(serverCameraTransform.position);
            camerarotations.Add(serverCameraTransform.rotation);
            Boxposts.Add(tempbp);Boxrotas.Add(tempbr);
            CheckRecordbp = tempbp;
            CheckRecordbr = tempbr;
        }
        timer = 0;
        yield return new WaitForSeconds(recordingTime / 2);
    }
    [Command(requiresAuthority = false)]
    public void SendNumberofRecords(int num){
        UpdateRecords(num);
    }
    void UpdateRecords(int num){
        Debug.Log("Receive the Records " + num);
        recordSelect.SetActive(true);
        int temp = num - countRecord;
        for(int i = 0; i < temp; i++){
            countRecord++;
            // Add the option to recordSelect dropdown
            record.options.Add(new TMP_Dropdown.OptionData("Record " + countRecord));
        }
    }
    [ClientRpc]
    void SyncData(){
        // SynrecordData(positionrecords, rotationrecords, SynBoxpositions, SynBoxrotations);
        if(!isServer){
            positionrecords = LoadMPData(filePathp); 
            if(NumOfRecords() > 0){
                SendNumberofRecords(NumOfRecords());
                rotationrecords = LoadMRData(filePathr);
                SynBoxpositions = LoadBPData(filePathboxp);
                SynBoxrotations = LoadBRData(filePathboxr);
                Synpositionrecords = LoadMPData(filePathp);
                Synrotationrecords = LoadMRData(filePathr);
                Boxpositions = LoadBPData(filePathboxp);
                Boxrotations = LoadBRData(filePathboxr);
                Debug.Log("Send the Records "+ NumOfRecords());
                Debug.Log(filePathboxp);
            }
        }
    }
    void Toggle(){
        ToggleRecordingServer();
        ToggleRecordingClient();
    }
    [Server]
    void ToggleRecordingServer(){
        ToggleRecording();
    }
    [ClientRpc]
    void ToggleRecordingClient(){
        if(!isServer){
            ToggleRecording();
        }
    }
    
    void ToggleRecording()
    {
        isRecording = !isRecording;
        recordingTime = 0;  // Reset the recording time
        
        if (isRecording && isServer)
        {
            // Start to record the action
            recordButton.SetActive(false);
            stopButton.SetActive(true);
        }else if(isRecording && isClient){
            Debug.Log("StartRecord");
            ClearCameraposition();
            ClearCamerarotation();
            ClearBoxpositions();
            ClearBoxrotations();
        }
        else if(!isRecording && isServer)
        {
            // Stop to record the action
            recordButton.SetActive(true);
            stopButton.SetActive(false);
            recordSelect.SetActive(true);
            // syncButton.SetActive(true);
            countRecord++;

            // Add the option to recordSelect dropdown
            record.options.Add(new TMP_Dropdown.OptionData("Record " + countRecord));
        }else if(!isRecording && isClient){
            
            Synpositionrecords.Add(camerapositions);
            Synrotationrecords.Add(camerarotations);
            Boxpositions.Add(Boxposts);
            Boxrotations.Add(Boxrotas);
            /*SynBoxpositions = Boxpositions;
            SynBoxrotations = Boxrotations;
            positionrecords = Synpositionrecords;
            rotationrecords = Synrotationrecords;*/
            SaveBPData(Boxpositions, filePathboxp);
            SaveBRData(Boxrotations, filePathboxr);
            SaveMPData(Synpositionrecords, filePathp);
            SaveMRData(Synrotationrecords, filePathr);
            // Import the file to the Unity Editor
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
            positionrecords = LoadMPData(filePathp);
            rotationrecords = LoadMRData(filePathr);
            SynBoxpositions = LoadBPData(filePathboxp);
            SynBoxrotations = LoadBRData(filePathboxr);
            Debug.Log("StopRecord");
        }
    }
    public void SaveBPData(List<List<List<Vector3>>> data, string path)
    {   
        string json = JsonConvert.SerializeObject(data, setting);
        File.WriteAllText(path, json);
    }
    public void SaveBRData(List<List<List<Quaternion>>> data, string path)
    {   
        string json = JsonConvert.SerializeObject(data, setting);
        File.WriteAllText(path, json);
    }
    public void SaveMPData(List<List<Vector3>> data, string path)
    {   
        string json = JsonConvert.SerializeObject(data, setting);
        File.WriteAllText(path, json);
    }
    public void SaveMRData(List<List<Quaternion>> data, string path)
    {   
        string json = JsonConvert.SerializeObject(data, setting);
        File.WriteAllText(path, json);
    }
    public List<List<Vector3>> LoadMPData(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            
            return JsonConvert.DeserializeObject<List<List<Vector3>>>(json);
        }
        else
        {
            Debug.LogError("File not found");
            return new List<List<Vector3>>();
        }
    }
    public List<List<Quaternion>> LoadMRData(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            
            return JsonConvert.DeserializeObject<List<List<Quaternion>>>(json);
        }
        else
        {
            Debug.LogError("File not found");
            return new List<List<Quaternion>>();
        }
    }
    public List<List<List<Vector3>>> LoadBPData(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            
            return JsonConvert.DeserializeObject<List<List<List<Vector3>>>>(json);
        }
        else
        {
            Debug.LogError("File not found");
            return new List<List<List<Vector3>>>();
        }
    }
    public List<List<List<Quaternion>>> LoadBRData(string path)
    {
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            
            return JsonConvert.DeserializeObject<List<List<List<Quaternion>>>>(json);
        }
        else
        {
            Debug.LogError("File not found");
            return new List<List<List<Quaternion>>>();
        }
    }
    public int NumOfRecords(){
        return positionrecords.Count;
    }
    public int Numofactions(int index){
        return positionrecords[index].Count;
    }
    public Vector3 GetCamerapositions(int id, int index){
        if (id < NumOfRecords() && index < Numofactions(id)){
            return positionrecords[id][index];
        }
        return new Vector3(0, 0, 0);
    }
    public Quaternion GetCamerarotations(int id, int index){
        if (id < NumOfRecords() && index < Numofactions(id)){
            return rotationrecords[id][index];
        }
        return new Quaternion(0, 0, 0, 0);
    }
    public List<Vector3> GetBoxpositions(int id, int index){
        if (id < NumOfRecords() && index < Numofactions(id)){
            return SynBoxpositions[id][index];
        }
        return new List<Vector3>();
    }
    public List<Quaternion> GetBoxrotations(int id, int index){
        if (id < NumOfRecords() && index < Numofactions(id)){
            return SynBoxrotations[id][index];
        }
        return new List<Quaternion>();
    }
    public void ClearCameraposition(){
        camerapositions = new List<Vector3>();
    }
    public void ClearCamerarotation(){
        camerarotations = new List<Quaternion>();
    }
    public void ClearBoxpositions(){
        Boxposts = new List<List<Vector3>>();
    }
    public void ClearBoxrotations(){
        Boxrotas = new List<List<Quaternion>>();
    }
    [ClientRpc]
    private void SynrecordData(List<List<Vector3>> posi, List<List<Quaternion>> rota, List<List<List<Vector3>>> boxposis, List<List<List<Quaternion>>> boxratas){
        Synpositionrecords = posi;
        Synrotationrecords = rota;
        Boxpositions = boxposis;
        Boxrotations = boxratas;
    }
}