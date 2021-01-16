using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class MessageEffectManager : MonoBehaviour {

    [SerializeField] GameManager GameManager;
    [SerializeField] GameObject Perfect;
    [SerializeField] GameObject Failure;

    void OnEnable() {
        GameManager
            .OnMessageEffect
            .Where(result => result == "perfect")
            .Subscribe(result => perfectShow());

        GameManager
            .OnMessageEffect
            .Where(result => result == "failure")
            .Subscribe(result => failureShow());
    }

    void perfectShow() {
        Perfect.SetActive(false);
        Perfect.SetActive(true);

        Observable.Timer(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => Perfect.SetActive(false));
    }

    void failureShow() {
        Failure.SetActive(false);
        Failure.SetActive(true);

        Observable.Timer(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => Failure.SetActive(false));
    }
}