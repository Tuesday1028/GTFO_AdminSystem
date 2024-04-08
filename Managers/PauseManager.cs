using BepInEx.Unity.IL2CPP.Utils;
using Hikaria.AdminSystem.Interfaces;
using SNetwork;
using System.Collections;
using System.Collections.Generic;
using TheArchive.Loader;
using UnityEngine;

namespace Hikaria.AdminSystem.Managers;

public class PauseManager : MonoBehaviour
{
    public static void Setup()
    {
        if (s_Object == null)
        {
            LoaderWrapper.ClassInjector.RegisterTypeInIl2Cpp<PauseManager>();
            s_Object = new(typeof(PauseManager).FullName);
            GameObject.DontDestroyOnLoad(s_Object);
            s_Object.AddComponent<PauseManager>();
        }
    }

    private void Awake()
    {
        Current = this;
    }

    private void SetPaused()
    {
        if (SNet.IsMaster)
        {
            SNet.Capture.CaptureGameState(eBufferType.Migration_A);
        }
        if (_PauseUpdateCoroutine != null)
        {
            StopCoroutine(_PauseUpdateCoroutine);
        }
        _PauseUpdateCoroutine = this.StartCoroutine(UpdateRegistered());
        foreach (IPauseable pauseable in _PausableUpdaters)
        {
            pauseable.OnPaused();
        }
    }

    private void SetUnpaused()
    {
        if (_PauseUpdateCoroutine != null)
        {
            StopCoroutine(_PauseUpdateCoroutine);
            _PauseUpdateCoroutine = null;
        }
        foreach (IPauseable pauseable in _PausableUpdaters)
        {
            pauseable.OnUnpaused();
        }
        if (SNet.IsMaster)
        {
            SNet.Sync.StartRecallWithAllSyncedPlayers(eBufferType.Migration_A, false);
        }
    }

    private IEnumerator UpdateRegistered()
    {
        var yielder = new WaitForSecondsRealtime(PauseUpdateInterval);
        while (true)
        {
            foreach (IPauseable pauseable in _PausableUpdaters)
            {
                pauseable.PausedUpdate();
            }
            yield return yielder;
        }
    }

    public void RegisterForPausedUpdate(IPauseable pu)
    {
        _PausableUpdaters.Add(pu);
    }

    public void UnregisterPausedUpdate(IPauseable pu)
    {
        _PausableUpdaters.Remove(pu);
    }

    public static bool IsPaused
    {
        get
        {
            return s_isPaused;
        }
        set
        {
            if (s_isPaused != value)
            {
                s_isPaused = value;
                if (value)
                {
                    Current.SetPaused();
                    global::PauseManager.IsPaused = true;
                    return;
                }
                Current.SetUnpaused();
                global::PauseManager.IsPaused = false;
            }
        }
    }

    public static float PauseUpdateInterval => Time.fixedDeltaTime;

    private Coroutine _PauseUpdateCoroutine;

    public static PauseManager Current;

    private List<IPauseable> _PausableUpdaters = new();

    private static bool s_isPaused;

    private static GameObject s_Object;
}