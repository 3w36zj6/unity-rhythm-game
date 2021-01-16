using System;
using System.Collections;
using System.Collections.Generic;

using UniRx;
using UniRx.Triggers;

using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    [SerializeField] string FilePath;

    [SerializeField] Button Play;
    [SerializeField] Button SetChart;

    [SerializeField] GameObject Red;
    [SerializeField] GameObject Blue;

    [SerializeField] Transform SpawnPoint;
    [SerializeField] Transform BeatPoint;

    //　ノーツを動かすために必要になる変数を追加
    float PlayTime;
    float Distance;
    float During;
    bool isPlaying;
    int GoIndex;

    string Title;
    int BPM;
    List<GameObject> Notes;

    void OnEnable() {
        // 追加した変数に値をセット
        Distance = Math.Abs(BeatPoint.position.x - SpawnPoint.position.x);
        During = 2 * 1000;
        isPlaying = false;
        GoIndex = 0;

        Debug.Log(Distance);

        Play.onClick
            .AsObservable()
            .Subscribe(_ => play());

        SetChart.onClick
            .AsObservable()
            .Subscribe(_ => loadChart());

        // ノーツを発射するタイミングかチェックし、go関数を発火
        this.UpdateAsObservable()
            .Where(_ => isPlaying)
            .Where(_ => Notes.Count > GoIndex)
            .Where(_ => Notes[GoIndex].GetComponent<NoteController>().getTiming() <= ((Time.time * 1000 - PlayTime) + During))
            .Subscribe(_ => {
                Notes[GoIndex].GetComponent<NoteController>().go(Distance, During);
                GoIndex++;
            });
    }

    void loadChart() {
        Notes = new List<GameObject>();

        string jsonText = Resources.Load<TextAsset>(FilePath).ToString();

        JsonNode json = JsonNode.Parse(jsonText);
        Title = json["title"].Get<string>();
        BPM = int.Parse(json["bpm"].Get<string>());

        foreach(var note in json["notes"]) {
            string type = note["type"].Get<string>();
            float timing = float.Parse(note["timing"].Get<string>());

            GameObject Note;
            if (type == "Red") {
                Note = Instantiate(Red, SpawnPoint.position, Quaternion.identity);
            } else if (type == "Blue") {
                Note = Instantiate(Blue, SpawnPoint.position, Quaternion.identity);
            } else {
                Note = Instantiate(Red, SpawnPoint.position, Quaternion.identity); // default Red
            }

            // setParameter関数を発火
            Note.GetComponent<NoteController>().setParameter(type, timing);

            Notes.Add(Note);
        }
    }

    // ゲーム開始時に追加した変数に値をセット
    void play() {
        PlayTime = Time.time * 1000;
        isPlaying = true;
        Debug.Log("Game Start!");
    }
}