using UnityEngine;
using TMPro;
using Mirror;
public class FPSDisplay : NetworkBehaviour
{
    public TMP_Text fpsText;
    public TMP_Dropdown dropdownfps;
    //private float deltaTime = 0.0f;
    double startTime;
    int count;
    private float nextFrameTime = 0.0f;
    private float frameRateInterval;
    void Start()
    {
        // 添加事件监听

        //Debug.Log(dropdownfps.name);
        
        frameRateInterval = 1.0f / ((dropdownfps.value + 1) * 5);
        //Debug.Log(dropdownfps.value);

        dropdownfps.onValueChanged.AddListener(delegate {
            DropdownValueChanged(dropdownfps);
            //Debug.Log(frameRateInterval);
        });
        
    }
    void DropdownValueChanged(TMP_Dropdown change)
    {
        // 这里可以添加你想要执行的操作
        frameRateInterval = 1.0f / ((change.value + 1) * 5);

    }
    void Update()
    {
        /*deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime ;
        float msec = deltaTime * 1000.0f;
        fpsText.text = $"{fps:0.} fps";
        */
        if(isClient && !isServer){
            if (Time.time < nextFrameTime)
            {
                return;
            }
            nextFrameTime = Time.time + frameRateInterval;
            Countfps();
        }else{
            Countfps();
        }
        
        
    }
    void Countfps(){
        ++count;
        if (Time.time >= startTime + 1)
        {
            float fps = count;
            startTime = Time.time;
            count = 0;
            fpsText.text = $"{fps:0.} fps";
        }
    }
}
