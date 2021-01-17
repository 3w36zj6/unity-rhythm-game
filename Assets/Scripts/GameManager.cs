using System;
using System.Collections;
using System.Collections.Generic;

using UniRx;
using UniRx.Triggers;

using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    [SerializeField] string FilePath;
    [SerializeField] string ClipPath;

    [SerializeField] Button Play;
    [SerializeField] Button SetChart;
    [SerializeField] Text ScoreText;
    [SerializeField] Text ComboText;
    [SerializeField] Text TitleText;

    [SerializeField] GameObject Red;
    [SerializeField] GameObject Blue;

    [SerializeField] Transform SpawnPoint;
    [SerializeField] Transform BeatPoint;

    AudioSource Music;
    // ノーツを動かすために必要になる変数を追加
    float PlayTime;
    float Distance;
    float During;
    bool isPlaying;
    int GoIndex;

    float CheckRange;// 判定範囲
    float BeatRange;
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

    // イベントを検知するオブザーバーを追加
    public IObservable<string> OnMessageEffect {
        get { return MessageEffectSubject; }
    }

    void OnEnable() {
        Music = this.GetComponent<AudioSource>();
        Distance = Math.Abs(BeatPoint.position.x - SpawnPoint.position.x);
        During = 2 * 1000;
        isPlaying = false;
        GoIndex = 0;

        CheckRange = 100;
        BeatRange = 50;

        //ScoreCeilingPoint = 1050000;
        CheckTimingIndex = 0;

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
        Notes = new List<GameObject>();
        NoteTimings = new List<float>();

        string jsonText = Resources.Load<TextAsset>(FilePath).ToString();
        Music.clip = (AudioClip)Resources.Load(ClipPath);
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


    }

    // ゲーム開始時に追加した変数に値をセット
    void play() {
        Music.Stop();
        Music.Play();
        PlayTime = Time.time * 1000;
        isPlaying = true;
        Debug.Log("Game Start!");
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

        if(minDiff != -1 & minDiff < CheckRange) {
            if(minDiff < BeatRange & Notes[minDiffIndex].GetComponent<NoteController>().getType() == type) {
                NoteTimings[minDiffIndex] = -1;
                Notes[minDiffIndex].SetActive(false);
                MessageEffectSubject.OnNext("perfect");
                updateScore("perfect");
                //Debug.Log("beat " + type + " success.");
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
        } else if (result == "failure") {
            ComboCount = 0;
        } else {
            ComboCount = 0; // default failure
        }

        ComboText.text = ComboCount.ToString();
        ScoreText.text = Score.ToString();
    }
}