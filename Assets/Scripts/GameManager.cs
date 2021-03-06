using System;
using System.Collections;
using System.Collections.Generic;

using UniRx;
using UniRx.Triggers;

using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    //[SerializeField] Button Play;
    [SerializeField] Button SetChart;
    [SerializeField] Text ScoreText;
    [SerializeField] Text ComboText;
    [SerializeField] Text TitleText;

    [SerializeField] GameObject Red;
    [SerializeField] GameObject Blue;

    [SerializeField] Transform SpawnPoint;
    [SerializeField] Transform BeatPoint;

    [SerializeField] GameObject SelectBackGround;
    [SerializeField] GameObject ScoreArea;
    [SerializeField] Dropdown SelectMusic;

    AudioSource Music;

    float PlayTime;
    float Distance;
    float During;
    bool isPlaying;
    int GoIndex;

    float CheckRange;// 判定範囲
    float GoodRange;
    float PerfectRange;
    List<float> NoteTimings;

    float ComboCount;
    float Score;
    float ScoreFirstTerm;
    float ScoreTorerance;
    float ScoreCeilingPoint;
    int CheckTimingIndex;

    string Title;
    int BPM;
    List<GameObject> Notes;

    Subject<string> SoundEffectSubject = new Subject<string>();

    public IObservable<string> OnSoundEffect {
        get { return SoundEffectSubject; }
    }

    Subject<string> MessageEffectSubject = new Subject<string>();

    public IObservable<string> OnMessageEffect {
        get { return MessageEffectSubject; }
    }



    void OnEnable() {
        Music = this.GetComponent<AudioSource>();
        Distance = Math.Abs(BeatPoint.position.x - SpawnPoint.position.x);
        During = 1750;
        isPlaying = false;
        GoIndex = 0;

        CheckRange = 180;
        GoodRange = 100;
        PerfectRange = 50;

        //ScoreCeilingPoint = 1050000;
        CheckTimingIndex = 0;

        ScoreArea.SetActive(false);

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

        this.UpdateAsObservable()
            .Where(_ => isPlaying)
            .Where(_ => Notes.Count > CheckTimingIndex)
            .Where(_ => NoteTimings[CheckTimingIndex] == -1)
            .Subscribe(_ => CheckTimingIndex++);


        this.UpdateAsObservable()
            .Where(_ => isPlaying)
            .Where(_ => Notes.Count > CheckTimingIndex)
            .Where(_ => NoteTimings[CheckTimingIndex] != -1)
            .Where(_ => NoteTimings[CheckTimingIndex] < ((Time.time * 1000 - PlayTime) - CheckRange/2))
            .Subscribe(_ => {
                updateScore("failure");
                CheckTimingIndex++;
            });

        this.UpdateAsObservable()
            .Where(_ => isPlaying)
            .Where(_ => (Input.GetKeyDown(KeyCode.F) | Input.GetKeyDown(KeyCode.J)))
            .Subscribe(_ => {
                beat("don", Time.time * 1000 - PlayTime);
                SoundEffectSubject.OnNext("don");
            });


        this.UpdateAsObservable()
            .Where(_ => isPlaying)
            .Where(_ => (Input.GetKeyDown(KeyCode.D) | Input.GetKeyDown(KeyCode.K)))
            .Subscribe(_ => {
                beat("ka", Time.time * 1000 - PlayTime);
                SoundEffectSubject.OnNext("ka");
            });

    }

    void loadChart() {
        //Debug.Log(SelectMusic.options[SelectMusic.value].text + "/chart");
        if (SelectMusic.value == 0) {// 選んでいない時
            return;
        }
        Notes = new List<GameObject>();
        NoteTimings = new List<float>();

        string jsonText = Resources.Load<TextAsset>(SelectMusic.options[SelectMusic.value].text + "/chart").ToString();
        Music.clip = (AudioClip)Resources.Load(SelectMusic.options[SelectMusic.value].text + "/music");
        JsonNode json = JsonNode.Parse(jsonText);
        Title = json["title"].Get<string>();
        BPM = int.Parse(json["bpm"].Get<string>());

        foreach(var note in json["notes"]) {
            string type = note["type"].Get<string>();
            float timing = float.Parse(note["timing"].Get<string>());

            GameObject Note;
            if (type == "don") {
                Note = Instantiate(Red, SpawnPoint.position, Quaternion.identity);
            } else if (type == "ka") {
                Note = Instantiate(Blue, SpawnPoint.position, Quaternion.identity);
            } else {
                Note = Instantiate(Red, SpawnPoint.position, Quaternion.identity); // default Red
            }

            // setParameter関数を発火
            Note.GetComponent<NoteController>().setParameter(type, timing);

            Notes.Add(Note);
            NoteTimings.Add(timing);
        }
        TitleText.text = Title;

        play();


    }

    void play() {
        Music.Stop();
        Music.Play();
        PlayTime = Time.time * 1000;
        isPlaying = true;
        SelectBackGround.SetActive(false);
        ScoreArea.SetActive(true);
    }

    void beat(string type, float timing) {
        float minDiff = -1;
        int minDiffIndex = -1;

        for (int i = 0; i < NoteTimings.Count; i++) {
            if(NoteTimings[i] > 0) {
                float diff = Math.Abs(NoteTimings[i] - timing);
                if(minDiff == -1 || minDiff > diff) {
                    minDiff = diff;
                    minDiffIndex = i;
                }
            }
        }

        if (minDiff != -1 & minDiff < CheckRange) {
            if (minDiff < PerfectRange & Notes[minDiffIndex].GetComponent<NoteController>().getType() == type) {
                NoteTimings[minDiffIndex] = -1;
                Notes[minDiffIndex].SetActive(false);
                MessageEffectSubject.OnNext("perfect");
                updateScore("perfect");
                //Debug.Log("beat " + type + " perfect.");
            } else if (minDiff < GoodRange & Notes[minDiffIndex].GetComponent<NoteController>().getType() == type) {
                NoteTimings[minDiffIndex] = -1;
                Notes[minDiffIndex].SetActive(false);
                MessageEffectSubject.OnNext("good");
                updateScore("good");
                //Debug.Log("beat " + type + " good.");
            } else {
                NoteTimings[minDiffIndex] = -1;
                Notes[minDiffIndex].SetActive(false);
                MessageEffectSubject.OnNext("failure");
                updateScore("failure");
                //Debug.Log("beat " + type + " failure.");
            }
        } else {
            //Debug.Log("through");
        }
    }
    void updateScore(string result) {
        if(result == "perfect") {
            ComboCount++;
            Score += 100;
        } else if (result == "good") {
            ComboCount++;
            Score += 50;
        } else {
            ComboCount = 0; // default failure
        }

        ComboText.text = ComboCount.ToString();
        ScoreText.text = Score.ToString();
    }
}