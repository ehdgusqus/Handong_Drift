using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class ArduinoInput : MonoBehaviour
{
    [Header("Arduino Settings")]
    public string portName = "COM3"; // 본인 포트 번호 확인 필수!
    public int baudRate = 9600;

    SerialPort stream;

    [HideInInspector] public float steerValue = 0f;
    [HideInInspector] public float accelValue = 0f;
    [HideInInspector] public bool isBtnPressed = false;

    void Start()
    {
        OpenConnection();
    }

    public void OpenConnection() 
    {
        if (stream != null && stream.IsOpen) 
        {
            stream.Close();
            stream = null;
        }

        try
        {
            stream = new SerialPort(portName, baudRate);
            stream.ReadTimeout = 20; 
            stream.Open();
            Debug.Log("아두이노 연결 성공! (" + portName + ")");
        }
        catch (System.Exception e)
        {
            Debug.LogError("아두이노 연결 오류 (포트 확인 필요): " + e.Message);
        }
    }

    void Update()
    {
        if (stream != null && stream.IsOpen)
        {
            try
            {
                // 데이터 읽기
                string value = stream.ReadLine();
                string[] vec = value.Split(',');

                if (vec.Length == 3)
                {
                    int rawX = int.Parse(vec[0]);
                    int rawY = int.Parse(vec[1]);
                    int rawBtn = int.Parse(vec[2]);

                    // 값 변환
                    steerValue = (rawX - 512) / 512f;
                    accelValue = (rawY - 512) / 512f;

                    if (Mathf.Abs(steerValue) < 0.15f) steerValue = 0;
                    if (Mathf.Abs(accelValue) < 0.15f) accelValue = 0;

                    isBtnPressed = (rawBtn == 0);
                }
                
                stream.BaseStream.Flush(); 
            }
            catch (System.Exception)
            {

            }
        }
    }

    void OnDisable()
    {
        CloseConnection();
    }

    void OnApplicationQuit()
    {
        CloseConnection();
    }

    void CloseConnection()
    {
        if (stream != null && stream.IsOpen)
        {
            try 
            {
                stream.Close(); 
                Debug.Log("아두이노 연결 해제됨");
            }
            catch(System.Exception) { }
        }
    }
}