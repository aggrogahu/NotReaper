using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DG.Tweening;
using NLayer;
using NotReaper.IO;
using NotReaper.UI;
using NotReaper.UserInput;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace NotReaper.Timing {


    public class UITiming : MonoBehaviour {


        [Header("UI Elements")]
        public Button genAudicaButton;
        public Button applyButton;
        public Button selectSongButton;
        public TMP_InputField bpmInput;
        public TMP_InputField offsetInput;
        public TextMeshProUGUI nameText;
        public TMP_InputField songNameInput;
        public TMP_InputField mapperInput;


        [Header("Extras")]
        public Image BG;
        public CanvasGroup window;
        public EditorInput editorInput;


        private AudioClip audioFile;
        public Timeline timeline;


        public int offset = 0;
        public double bpm = 0;
        public string loadedSong;

        public string songName = "";
        public string mapperName = "";


        TrimAudio trimAudio = new TrimAudio();


        Process ffmpeg = new Process();

        private void Start() {
            var t = this.transform;
            var position = t.localPosition;
            t.localPosition = new Vector3(0, position.y, position.z);
            window.alpha = 0;

            string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "FFMPEG/ffmpeg.exe");
			ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
			ffmpeg.StartInfo.FileName = ffmpegPath;
            ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardOutput = true;
            ffmpeg.StartInfo.WorkingDirectory = Path.Combine(Application.streamingAssetsPath, "FFMPEG");
            
        }
        


        public void SelectAudioFile() {
            var compatible = new[] { new ExtensionFilter("Compatible Audio Files", "mp3", "ogg") }; 
            string[] paths = StandaloneFileBrowser.OpenFilePanel("Select music track", Path.Combine(Application.persistentDataPath), compatible, false);
            var filePath = paths[0];

            if (filePath != null) {
                // if user loads mp3 instead of ogg, do the conversion first
                if (paths[0].EndsWith(".mp3")) {
                    UnityEngine.Debug.Log(String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], "converted.ogg"));
                    ffmpeg.StartInfo.Arguments =
                        String.Format("-y -i \"{0}\" -map 0:a \"{1}\"", paths[0], "converted.ogg");
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();
                    filePath = "converted.ogg";
                }

                StartCoroutine(
                    GetAudioClip($"file://" + Path.Combine(Application.streamingAssetsPath, "FFMPEG", filePath)));
                nameText.text = paths[0];
                loadedSong = paths[0];
            }
        }


        public void ApplyValues() {

            if (!timeline.paused) {
                timeline.TogglePlayback();
            }

            Double.TryParse(bpmInput.text, out bpm);
            Int32.TryParse(offsetInput.text, out offset);

            timeline.SetTimingModeStats(bpm, offset);
            timeline.SetOffset(offset);

            mapperName = RemoveSpecialCharacters(mapperInput.text);
            songName = RemoveSpecialCharacters(songNameInput.text);

            CheckAllUIFilled();

        }

        public void GenerateOgg() {
            trimAudio.SetAudioLength(loadedSong, Path.Combine(Application.streamingAssetsPath, "FFMPEG", "output.ogg"), offset, bpm);
            string path = AudicaGenerator.Generate(Path.Combine(Application.streamingAssetsPath, "FFMPEG", "output.ogg"), (songName + "-" + mapperName), (songName + "-" + mapperName), "artist", bpm, "event:/song_end/song_end_C#", mapperName, 0);
            timeline.LoadAudicaFile(false, path);
            editorInput.SelectMode(EditorMode.Compose);

            applyButton.interactable = false;
            genAudicaButton.interactable = false;
            selectSongButton.interactable = false;

        }

        public void CheckAllUIFilled() {
            if (bpm != 0 && loadedSong != "" && mapperName != "" && songName != "") {
                genAudicaButton.interactable = true;
            } else {
                genAudicaButton.interactable = false;
            }
        }

        public IEnumerator FadeIn() {


            float fadeDuration = (float) NRSettings.config.UIFadeDuration;


            BG.DOFade(1.0f, fadeDuration / 2f);

            yield return new WaitForSeconds(fadeDuration / 4f);

            DOTween.To(x => window.alpha = x, 0.0f, 1.0f, fadeDuration / 2f);

            yield break;
        }

        public IEnumerator FadeOut() {

            float fadeDuration = (float) NRSettings.config.UIFadeDuration;

            DOTween.To(x => window.alpha = x, 1.0f, 0.0f, fadeDuration / 4f);

            BG.DOFade(0.0f, fadeDuration / 2f);

            yield return new WaitForSeconds(fadeDuration / 2f);

            this.gameObject.SetActive(false);

            yield break;
        }


        IEnumerator GetAudioClip(string uri) {
            using(UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.OGGVORBIS)) {
                yield return www.SendWebRequest();

                if (www.isNetworkError) {
                    UnityEngine.Debug.Log(www.error);
                } else {
                    audioFile = DownloadHandlerAudioClip.GetContent(www);

                    Double.TryParse(bpmInput.text, out bpm);

                    timeline.LoadTimingMode(audioFile);


                    yield break;


                }
            }
        }

        public AudioClip LoadMp3(string filePath) {
            string filename = System.IO.Path.GetFileNameWithoutExtension(filePath);

            MpegFile mpegFile = new MpegFile(filePath);

            // assign samples into AudioClip
            AudioClip ac = AudioClip.Create(filename,
                (int) (mpegFile.Length / sizeof(float) / mpegFile.Channels),
                mpegFile.Channels,
                mpegFile.SampleRate,
                true,
                data => { int actualReadCount = mpegFile.ReadSamples(data, 0, data.Length); },
                position => { mpegFile = new MpegFile(filePath); });

            //mpegFile.Dispose();

            return ac;
        }

        public string RemoveSpecialCharacters(string str) {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str) {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_' || c == '-') {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }

}