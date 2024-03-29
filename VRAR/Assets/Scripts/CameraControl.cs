﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System.Text;

public class CameraControl : MonoBehaviour {
    public bool DEBUG = false;

    #region GPS and Serial Variables
    private string DEVICE;
    private long TIME_STAMP;
    private bool STATUS;
    private float LAT;
    private float LON;
    private float ALT;
    private int SAT;
    private int PREC;
    private long CHARS;
    private int SENTENCES;
    private int CSUM_ERR;
    private float iniLAT;
    private float iniLON;
    private float iniALT;

    // Set the COM Port (COM4) and the BAUD Rate (9600 for XBee Connection).
    private static string COMPort = "COM4";
    private static int BAUDRate = 9600;
    SerialPort streamCable = new SerialPort(COMPort, BAUDRate);

    private bool readDoneFlg;
    private bool firstGPSFlg;
    StringBuilder jsonBuffer = new StringBuilder();
    #endregion

    #region Player Control Variables
    public float speed = 6.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;
    private Vector3 moveDirection = Vector3.zero;

    public int playerControlSpeed = 10;
    public bool playerControlFlg;
    #endregion

    // Use this for initialization
    void Start() {
        Debug.Log("Start  ");
        firstGPSFlg = true;
        readDoneFlg = false;
        playerControlFlg = true;
        //streamCable.DataReceived += new SerialDataReceivedEventHandler(StreamCable_DataReceived);
        streamCable.ReadTimeout = 100;
        streamCable.Open(); //Open the Serial Stream.
    }

    private void StreamCable_DataReceived(object sender, SerialDataReceivedEventArgs e) {
        if (readDoneFlg == true) {
            // wait for main program done with the current buffer
            return;
        }
        SerialPort sp = (SerialPort)sender;
        char indata = (char)sp.ReadChar();
        if (indata == '\n') {
            readDoneFlg = true;
        } else {
            if (DEBUG) {
                Debug.Log("read: " + indata);
            }

            jsonBuffer.Append(indata);
        }
    }

    // Update is called once per frame
    void Update() {
        if (!playerControlFlg) {
            if (readGPS()) {
                move();
            }
            if (Input.GetKeyDown(KeyCode.C)) {
                playerControlFlg = true;
                LAT = 0;
                LON = 0;
                iniLAT = 0;
                iniLON = 0;
            }
        } else {
            // ccmove();
            playerControl();
            if (Input.GetKeyDown(KeyCode.C)) {
                playerControlFlg = false;
            }
        }
    }

    bool readGPS() {
        if (!streamCable.IsOpen) {
            Debug.Log("Serial port it not open!");
        }
        char indata;
        while (true) {
            try {
                indata = (char)streamCable.ReadChar();
                if (indata == '\n') {
                    readDoneFlg = true;
                    if (DEBUG) {
                        Debug.Log("read: " + jsonBuffer);
                    }
                    break;
                } else {

                    jsonBuffer.Append(indata);
                }
            } catch (System.Exception) {
            }
        }
        if (readDoneFlg) {

            try {
                JsonUtility.FromJsonOverwrite(jsonBuffer.ToString(), this);
                jsonBuffer.Remove(0, jsonBuffer.Length);        // clear the buffer for the next reading
                readDoneFlg = false;
                if (DEBUG) {
                    Debug.Log("device = " + this.DEVICE);
                    Debug.Log("time_stamp = " + this.TIME_STAMP);
                    Debug.Log("status = " + this.STATUS);
                    Debug.Log("lat = " + this.LAT);
                    Debug.Log("lon = " + this.LON);
                    Debug.Log("alt = " + this.ALT);
                    Debug.Log("sat = " + this.SAT);
                    Debug.Log("prec = " + this.PREC);
                }
                return true;
            } catch (System.Exception) {
                Debug.Log("fail to Json.");
                jsonBuffer.Remove(0, jsonBuffer.Length);
                readDoneFlg = false;
                throw;
            }
        } else {
            Debug.Log("No  ");
            return false;
        }
    }

    void ccmove() {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller.isGrounded) {
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= speed;
            if (Input.GetButton("Jump"))
                moveDirection.y = jumpSpeed;

        }
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    void playerControl() {
        if (Input.GetKey(KeyCode.D)) {
            transform.Translate(new Vector3(playerControlSpeed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.A)) {
            transform.Translate(new Vector3(-playerControlSpeed * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.Q)) {
            transform.Translate(new Vector3(0, -playerControlSpeed * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.E)) {
            transform.Translate(new Vector3(0, playerControlSpeed * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.W)) {
            transform.Translate(new Vector3(0, 0, playerControlSpeed * Time.deltaTime));
        }
        if (Input.GetKey(KeyCode.S)) {
            transform.Translate(new Vector3(0, 0, -playerControlSpeed * Time.deltaTime));
        }
    }

    void move() {
        if (firstGPSFlg == true && LON != 0 && LAT != 0 && ALT != 0) {
            iniLON = LON;
            iniLAT = LAT;
            iniALT = ALT;
            firstGPSFlg = false;
        } else if (LON != 0 && LAT != 0) {
            float la = (float)((LAT - 53.178469) / 0.00001 * 0.12179047095976932582726898256213);
            float lo = (float)((LON - 53.178469) / 0.00001 * 0.12179047095976932582726898256213);
            float ila = (float)((iniLAT - 53.178469) / 0.00001 * 0.12179047095976932582726898256213);
            float ilo = (float)((iniLON - 53.178469) / 0.00001 * 0.12179047095976932582726898256213);
            //transform.position = Quaternion.AngleAxis(LON-iniLON, -Vector3.up) * Quaternion.AngleAxis(LAT-iniLAT, -Vector3.right) * new Vector3(0, 0, 1);
            transform.position = new Vector3((lo - ilo), 0.5f, (la - ila));
        }
    }
}