// ConvertTextToSound.cs
using BookApp.Models; // <-- use your single Chapter model
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TagLib;

namespace BookApp.Fungtions
{
    public class ConvertTextToSound
    {
        public async Task<bool> CreateSoundFileAsync(Chapter chapter, string storyName, string path)
        {
            if (chapter == null) return false;

            // Ensure the directory exists (use safe folder name)
            var storyDirectory = System.IO.Path.Combine(path ?? "", SafeFileName(storyName ?? "Story"));
            System.IO.Directory.CreateDirectory(storyDirectory);
            var safeTitle = "Error";
            if (storyName.Contains("Outfit"))
            {
                safeTitle = SafeFileName(chapter.Title ?? "Untitled");
            }
            else
            {
                //var safeTitle = SafeFileName(chapter.Title ?? "Untitled");
                var cleanedTitle = CleanFileName(chapter.Title ?? "Untitled");
                safeTitle = SafeFileName(cleanedTitle);
            }


            var wavPath = System.IO.Path.Combine(storyDirectory, safeTitle + ".wav");
            var mp3Path = System.IO.Path.Combine(storyDirectory, safeTitle + ".mp3");

            if (System.IO.File.Exists(mp3Path)) return false;

            await Task.Run(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    using var synth = new SpeechSynthesizer { Volume = 100, Rate = 0 };

                    // Try to pick Zira if installed (optional)
                    try
                    {
                        foreach (var v in synth.GetInstalledVoices())
                        {
                            if (v?.VoiceInfo?.Name?.Equals("Microsoft Zira Desktop", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                synth.SelectVoice(v.VoiceInfo.Name);
                                break;
                            }
                        }
                    }
                    catch { /* ignore */ }

                    // Stream straight to disk in compact speech format
                    var format = new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
                    synth.SetOutputToWaveFile(wavPath, format);

                    var text = NormalizeForTts(chapter.Content ?? "");
                    foreach (var chunk in ChunkForTts(text, 3000))
                    {
                        synth.Speak(chunk);
                        // simple pacing between chunks (avoids PromptBreak ctor issue)
                        Thread.Sleep(200);
                    }

                    synth.SetOutputToNull();
                }
                else
                {
                    // Cross-platform: implement this if you target Android/iOS native TTS
                    using var fs = System.IO.File.Create(wavPath);
                    var service = new TextToSpeechService();
                    service.SpeakToWaveStream(chapter.Content ?? "", fs, chunkSize: 3000);
                }

                // WAV -> MP3 (streaming)
                using (var rdr = new WaveFileReader(wavPath))
                using (var wtr = new LameMP3FileWriter(mp3Path, rdr.WaveFormat, LAMEPreset.VBR_90))
                {
                    rdr.CopyTo(wtr);
                }
            });

            // Clean up temp WAV no matter what
            try { System.IO.File.Delete(wavPath); } catch { /* ignore */ }


            try
            {
                using var tagFile = TagLib.File.Create(mp3Path);
                var tag = tagFile.Tag;

                // Safely set the author (performer)
                if (!string.IsNullOrWhiteSpace(chapter?.Author))
                    tag.Performers = new[] { chapter.Author };  // replaces the array safely

                // Set album name
                tag.Album = SafeFileName(storyName ?? "Story");

                if (int.TryParse(chapter.Number, out int trackNum))
                    tag.Track = (uint)trackNum;

                if (!string.IsNullOrWhiteSpace(chapter?.EpubDescription))
                    tag.Comment = chapter.EpubDescription;

                // Get/create ID3v2 tag explicitly for pictures
                var id3v2 = tagFile.GetTag(TagLib.TagTypes.Id3v2, true) as TagLib.Id3v2.Tag;

                if (id3v2 != null && chapter?.CoverImage is { Length: > 0 })
                {
                    var picture = new TagLib.Picture
                    {
                        Type = TagLib.PictureType.FrontCover,
                        Description = "Cover",
                        MimeType = "image/jpeg",  // correct for .jpg
                        Data = new TagLib.ByteVector(chapter.CoverImage)
                    };

                    id3v2.Pictures = new TagLib.IPicture[] { picture };
                }

                //if (!string.IsNullOrWhiteSpace(chapter?.Title))
                //{
                //    // remove the leading 4 digits and any extra spaces
                //    string cleanTitle = Regex.Replace(chapter.Title, @"^\d{4}\s*", "");
                //    tag.Title = cleanTitle;
                //}

                tagFile.Save();
            }
            catch { /* ignore tagging errors */ }

            return true;
        }

        // ---- helpers ----

        // Light, speech-friendly cleanup
        public static string NormalizeForTts(string text)
        {
            text = Regex.Replace(text ?? "", @"(\p{L})-\r?\n(\p{L})", "$1$2"); // de-hyphenate soft-wraps
            text = (text ?? "").Replace("\r\n", "\n");
            text = Regex.Replace(text, @"[ \t]*\n[ \t]*", "\n");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            text = Regex.Replace(text, @"(?<=\S)\n(?=\S)", " "); // join single line breaks inside paragraphs
            text = Regex.Replace(text, @"[ \t]{2,}", " ");
            return text.Trim();
        }

        public static IEnumerable<string> ChunkForTts(string text, int maxChars = 3000)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;

            text = NormalizeForTts(text);

            // sentence-first split
            var sentences = Regex.Split(text, @"(?<=[\.!\?…])\s+");
            var buf = new StringBuilder(maxChars + 64);

            foreach (var s in sentences)
            {
                if (s.Length > maxChars)
                {
                    foreach (var part in ChunkByWords(s, maxChars)) yield return part;
                    continue;
                }
                if (buf.Length + s.Length + 1 > maxChars)
                {
                    if (buf.Length > 0) { yield return buf.ToString(); buf.Clear(); }
                }
                if (buf.Length > 0) buf.Append(' ');
                buf.Append(s);
            }
            if (buf.Length > 0) yield return buf.ToString();

            static IEnumerable<string> ChunkByWords(string sentence, int limit)
            {
                var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder(limit + 32);
                foreach (var w in words)
                {
                    if (w.Length > limit)
                    {
                        if (sb.Length > 0) { yield return sb.ToString(); sb.Clear(); }
                        for (int i = 0; i < w.Length; i += limit)
                            yield return w.Substring(i, Math.Min(limit, w.Length - i));
                        continue;
                    }
                    if (sb.Length + w.Length + 1 > limit)
                    {
                        yield return sb.ToString();
                        sb.Clear();
                    }
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(w);
                }
                if (sb.Length > 0) yield return sb.ToString();
            }
        }

        public static string SafeFileName(string name, int maxBaseLength = 120)
        {
            if (string.IsNullOrWhiteSpace(name)) name = "untitled";
            name = name.Normalize(NormalizationForm.FormC);
            name = new string(name.Where(ch => !char.IsControl(ch)).ToArray());
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim().Trim('.');
            var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
                "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
            };
            if (reserved.Contains(name)) name = $"_{name}_";
            if (name.Contains("..")) name = name.Replace("..", "_");
            if (System.IO.Path.IsPathRooted(name)) name = System.IO.Path.GetFileName(name);
            if (name.Length > maxBaseLength) name = name.Substring(0, maxBaseLength);
            return string.IsNullOrWhiteSpace(name) ? "untitled" : name;
        }

        public static string CleanFileName(string fileName)
        {
            // fileName may contain extension or not
            string name = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);

            // If it contains Chapter X, strip everything before it
            var m = Regex.Match(name, @"Chapter\s*\d+", RegexOptions.IgnoreCase);
            if (!m.Success)
                return fileName;

            string cleaned = name.Substring(m.Index);

            return cleaned + ext;
        }


    }

    // Cross-platform stub; implement with native TTS if you target Android/iOS
    public class TextToSpeechService
    {
        public void SpeakToWaveStream(string text, Stream outputWaveStream, int chunkSize = 3000)
        {
            throw new NotImplementedException("Platform-specific TTS implementation needed.");
        }
    }
}
