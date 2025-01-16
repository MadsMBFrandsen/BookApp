using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System.Speech.Synthesis;

namespace BookApp.Fungtions
{
    public class ConvertTextToSound
    {
        public async Task<bool> CreateSoundFileAsync(Chapter chapter, string storyName, string path)
        {
            // Ensure the directory exists
            var storyDirectory = Path.Combine(path, storyName);
            Directory.CreateDirectory(storyDirectory);
            bool fileCreated = false;

            await Task.Run(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    // Windows-specific implementation using SpeechSynthesizer
                    using (SpeechSynthesizer reader = new())
                    {
                        reader.Volume = 100;
                        reader.Rate = 0; // Medium speed

                        // Select the desired voice (e.g., "Microsoft Zira Desktop")
                        foreach (var voice in reader.GetInstalledVoices())
                        {
                            if (voice.VoiceInfo.Name == "Microsoft Zira Desktop")
                            {
                                reader.SelectVoice(voice.VoiceInfo.Name);
                                break;
                            }
                        }

                        using (MemoryStream memoryStream = new())
                        {
                            var filePath = Path.Combine(storyDirectory, chapter.Title + ".mp3");
                            if (!File.Exists(filePath))
                            {
                                reader.SetOutputToWaveStream(memoryStream);
                                reader.Speak(chapter.Content);

                                ConvertWavStreamToMp3File(memoryStream, filePath);
                                fileCreated = true;
                            }
                        }
                    }
                }
                else
                {
                    // Cross-platform implementation (use a custom TextToSpeechService or library)
                    var synthesizer = new TextToSpeechService();

                    using (var memoryStream = new MemoryStream())
                    {
                        synthesizer.SpeakToWaveStream(chapter.Content, memoryStream);

                        var filePath = Path.Combine(storyDirectory, chapter.Title + ".mp3");

                        if (!File.Exists(filePath))
                        {
                            ConvertWavStreamToMp3File(memoryStream, filePath);
                            fileCreated = true;
                        }
                    }
                }
            });

            return fileCreated;
        }


        public static void ConvertWavStreamToMp3File(MemoryStream ms, string saveToFileName)
        {
            ms.Seek(0, SeekOrigin.Begin);

            using (var rdr = new NAudio.Wave.WaveFileReader(ms))
            using (var wtr = new NAudio.Lame.LameMP3FileWriter(saveToFileName, rdr.WaveFormat, NAudio.Lame.LAMEPreset.VBR_90))
            {
                rdr.CopyTo(wtr);
            }
        }

        public static void SetID3Tags(string filePath, string author, string title)
        {
            var file = TagLib.File.Create(filePath);
            file.Tag.Performers = new[] { author };
            file.Tag.Title = title;
            file.Save();
        }
    }

    // Platform-specific Text-to-Speech Service
    public class TextToSpeechService
    {
        public void SpeakToWaveStream(string text, MemoryStream memoryStream)
        {
            // Placeholder: Implement platform-specific text-to-speech functionality
            // Use Android/iOS APIs directly or plugins like Plugin.TextToSpeech

            // For Android: Use Android.Speech.Tts.TextToSpeech
            // For iOS: Use AVFoundation.AVSpeechSynthesizer
            throw new NotImplementedException("Platform-specific TTS implementation needed.");
        }
    }

}