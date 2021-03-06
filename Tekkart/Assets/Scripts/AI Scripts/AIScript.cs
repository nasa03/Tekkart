﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIScript : MonoBehaviour, Kart, AiKart
{
    //Kart Stuff
    float speed, currentSpeed;
    float currentRotate;
    public float rotate;

    //Kart Stats
    public string Name;
    public float TopSpeed = 60f;
    public float steering = 15f;
    public float acceleration = 5f;
    public float handling = 4f;
    public float driftingability = 0.4f;

    const float gravity = 10f;

    private bool Boostbool = false;
    const float MaxBoostTime = 1f;
    float CurrentBoostTime = 1f;
    private float driftPower = 0f;
    private bool Stunned = false;

    public Rigidbody KartSphere;
    public Transform kartNormal;
    public LayerMask layerMask;
    public Animator animator;
    public Animator animatorf;
    public GameObject BoostParticleParent;
    private ParticleSystem[] BoostParticles;
    private bool currentlyboosting = false;

    //AI Stuff
    private int NumberOfCheckpoints;
    private int TargetCheckpoint = 2;
    private const int DefaultTurnMax = 1;
    public GameObject CheckPointParent;
    private Transform[] CheckpointLocationArray;
    private float AngleToTarget;

    private Vector3 nextposition;

    private int TargetCheckPoint = 0;
    private int CurrentCheckPoint = -1;
    private float CheckPointValue = 1;
    private bool IsNewLap = false;


    private float RandomRange = 5.0f;
    private float AIPathingMinDistance = 10f;

    private bool RaceStarted = false;

    //Audio Stuff
    private AudioSource KartEngineSounds;
    private float PitchKartIdle = 0.75f;
    private float PitchKartAcc = 1.5f;
    private float PitchKartBoost = 2f;
    private float PitchKartRate = 0.9f;



    private void Awake()
    {
        //If checkpointparent isn't set, set it with Tag
        if (CheckPointParent == null)
        {
            CheckPointParent = GameObject.FindGameObjectWithTag("CheckpointParent");
        }


        //Set up boost particles
        BoostParticles = BoostParticleParent.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem p in BoostParticles)
        {
            p.Stop();
        }


        //Set Up Checkpoints
        CheckpointLocationArray = CheckPointParent.GetComponentsInChildren<Transform>();
        NumberOfCheckpoints = CheckpointLocationArray.Length;
        CheckPointReached();

        //Set up Audio
        KartEngineSounds = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = KartSphere.transform.position - new Vector3(0f, 0.8f, 0);



        if (RaceStarted)
        {
            //Go Foward
            if (!Stunned)
            {
                if (!Boostbool)
                {
                    speed = TopSpeed;
                    KartEngineSounds.pitch = Mathf.Lerp(KartEngineSounds.pitch, PitchKartAcc, PitchKartRate * Time.deltaTime);
                }
                else
                {
                    speed = TopSpeed + TopSpeed * 1.5f;
                    KartEngineSounds.pitch = Mathf.Lerp(KartEngineSounds.pitch, PitchKartBoost, PitchKartRate * 2 * Time.deltaTime);
                }
            } else
            {
                KartEngineSounds.pitch = Mathf.Lerp(KartEngineSounds.pitch, PitchKartIdle, PitchKartRate * Time.deltaTime);
            }

            //Steering

            //Find Angle of next checkpoint
            Vector3 TargetDirection = nextposition - transform.position;
            AngleToTarget = Vector3.SignedAngle(TargetDirection, transform.forward, Vector3.up);

            //Decide if that's a left turn or a right turn
            int dir = AngleToTarget > 0 ? -1 : 1;

            //Caluclate how hard to turn relative to angle
            if (AngleToTarget < 0) { AngleToTarget = AngleToTarget * -1; }


            float amount = AngleToTarget / 20; //I love magic numbers don't you?
            if (amount > 1) { amount = 1; };


            Steer(dir, amount);
            try
            {
                animator.SetFloat("Direction", rotate);
                try
                {
                    animatorf.SetFloat("Direction", rotate);
                }
                catch
                {
                    //not all Karts have a front animator
                }
            }
            catch
            {
                //not all Karts have a rear animator
            }

            //AI Driving Logic
            //Debug.DrawLine(transform.position, CheckpointLocationArray[TargetCheckpoint - 1].position, Color.green);
            //Debug.DrawLine(transform.position, nextposition, Color.red);
        }


        if (Vector3.Distance(transform.position, CheckpointLocationArray[TargetCheckpoint - 1].position) < AIPathingMinDistance)
        {
            CheckPointReached();
        }

        if (Boostbool)
        {
            currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * acceleration * 2); speed = 0f;
        }
        else
        {
            currentSpeed = Mathf.SmoothStep(currentSpeed, speed, Time.deltaTime * acceleration); speed = 0f;
        }
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * handling); rotate = 0f;

        //Boost 
        if (Boostbool)
        {
            if (!currentlyboosting)
            {
                foreach (ParticleSystem p in BoostParticles)
                {
                   p.Play();
                }
                currentlyboosting = true;
            }

            CurrentBoostTime = CurrentBoostTime - Time.deltaTime;
            if (CurrentBoostTime < 0)
            {
                Boostbool = false;
                CurrentBoostTime = MaxBoostTime;
                foreach (ParticleSystem p in BoostParticles)
                {
                    p.Stop();
                }
                currentlyboosting = false;
            }
        }
    }

    private void FixedUpdate()
    {
        //Going Forward
        KartSphere.AddForce(transform.forward * currentSpeed, ForceMode.Acceleration);

        //Gravity
        KartSphere.AddForce(Vector3.down * gravity, ForceMode.Acceleration);

        //Steering
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, new Vector3(0, transform.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);


        //Making the kart follow the road properly
        RaycastHit hitOn;
        RaycastHit hitNear;

        Physics.Raycast(transform.position + (transform.up * .1f), Vector3.down, out hitOn, 1.1f, layerMask);
        Physics.Raycast(transform.position + (transform.up * .1f), Vector3.down, out hitNear, 2.0f, layerMask);

        //Normal Rotation
        kartNormal.up = Vector3.Lerp(kartNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
        kartNormal.Rotate(0, transform.eulerAngles.y, 0);
    }

    public void CheckPointReached() {
        TargetCheckpoint++;
        if (TargetCheckpoint == NumberOfCheckpoints-1)
        {
            TargetCheckpoint = 2;
        }

        nextposition = CheckpointLocationArray[TargetCheckpoint].position;
        nextposition = nextposition + new Vector3(Random.Range(-RandomRange, RandomRange), 0, Random.Range(-RandomRange, RandomRange));
    }

    private void Steer(int direction, float amount)
    {
        rotate = (steering * direction) * amount;
    }

    public void Boost()
    {
        Boostbool = true;
        CurrentBoostTime = MaxBoostTime;
    }

    public void EnterDriftZone(float DriftSize)
    {
        if (Random.value < .5)
        {
            driftPower = 3 + Random.Range(0f, DriftSize);
        }
    }

    public void ExitDriftZone()
    {
        if (driftPower > 0)
        {
            CalculateBoost();
        }
    }

      private void CalculateBoost()
    {
        if (driftPower > 7)
        {
            Boostbool = true;
            CurrentBoostTime = 1;
            driftPower = 0;
            return;
        }

        else if (driftPower > 5)
        {
            Boostbool = true;
            CurrentBoostTime = 0.5f;
            driftPower = 0;
            return;
        }

        else if (driftPower > 3)
        {
            Boostbool = true;
            CurrentBoostTime = 0.2f;
            driftPower = 0;
            return;
        }

        driftPower = 0;
    }




    /*
    /   Figuring Out Who's Where
    */


    public int GetTargetCheckPoint()
    {
        return TargetCheckPoint;
    }

    public void SetTargetCheckPoint(int IncVal)
    {
        TargetCheckPoint = IncVal;
    }

    public int GetCurrentCheckpoint()
    {
        return CurrentCheckPoint;
    }

    public void SetCurrentCheckpoint(int IncVal)
    {
        CurrentCheckPoint = IncVal;
    }

    public float GetCheckPointValue()
    {
        return CheckPointValue;
    }

    public void SetCheckPointValue(float IncVal)
    {
        CheckPointValue = CheckPointValue + IncVal;
    }

    public string GetName()
    {
        return Name;
    }

    public bool GetIsNewLap()
    {
        return IsNewLap;
    }

    public void SetIsNewLap(bool YesNo)
    {
        IsNewLap = YesNo;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public void SetUpAIPathing(float IncRandomRange, float IncMinDistance)
    {
        if (IncRandomRange == 0 || IncMinDistance == 0)
        {
            return;
        }
        RandomRange = IncRandomRange;
        AIPathingMinDistance = IncMinDistance;
    }

    public void BecomeStunned()
    {
        if (!Stunned)
        {
            StartCoroutine("StunnedCoroutine");
        }
        Stunned = true;
    }

    IEnumerator StunnedCoroutine()
    {
        yield return new WaitForSeconds(2.5f);
        Stunned = false;
    }

    public void SetReadyGo()
    {
        RaceStarted = true;
    }
}
