using BookApp.Models;
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
using IOFile = System.IO.File;

namespace BookApp.Fungtions
{
    public class ConvertTextToSound
    {
        public async Task<bool> CreateSoundFileAsync(Chapter chapter, string storyName, string path)
        {
            if (chapter == null) return false;

            var storyDirectory =
                Path.Combine(path ?? "", SafeFileName(storyName ?? "Story"));

            Directory.CreateDirectory(storyDirectory);

            var normalizedTitle =
                NormalizeChapterTitle(chapter.Title);

            var safeTitle = SafeFileName(normalizedTitle);

            var wavPath = Path.Combine(storyDirectory, safeTitle + ".wav");
            var mp3Path = Path.Combine(storyDirectory, safeTitle + ".mp3");

            if (IOFile.Exists(mp3Path))
                return false;

            await Task.Run(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    using var synth =
                        new SpeechSynthesizer { Volume = 100, Rate = 0 };

                    try
                    {
                        foreach (var v in synth.GetInstalledVoices())
                        {
                            if (v?.VoiceInfo?.Name?.Equals(
                                "Microsoft Zira Desktop",
                                StringComparison.OrdinalIgnoreCase) == true)
                            {
                                synth.SelectVoice(v.VoiceInfo.Name);
                                break;
                            }
                        }
                    }
                    catch { }

                    var format =
                        new SpeechAudioFormatInfo(
                            16000,
                            AudioBitsPerSample.Sixteen,
                            AudioChannel.Mono);

                    synth.SetOutputToWaveFile(wavPath, format);

                    var text =
                        NormalizeForTts(chapter.Content ?? "");

                    foreach (var chunk in ChunkForTts(text, 3000))
                    {
                        synth.Speak(chunk);
                        Thread.Sleep(200);
                    }

                    synth.SetOutputToNull();
                }
                else
                {
                    using var fs = IOFile.Create(wavPath);
                    var service = new TextToSpeechService();
                    service.SpeakToWaveStream(
                        chapter.Content ?? "",
                        fs,
                        3000);
                }

                using var rdr = new WaveFileReader(wavPath);
                using var wtr =
                    new LameMP3FileWriter(
                        mp3Path,
                        rdr.WaveFormat,
                        LAMEPreset.VBR_90);

                rdr.CopyTo(wtr);
            });

            try { IOFile.Delete(wavPath); } catch { }

            try
            {
                using var tagFile = TagLib.File.Create(mp3Path);
                var tag = tagFile.Tag;

                tag.Title =
                    NormalizeChapterTitle(chapter.Title);

                if (!string.IsNullOrWhiteSpace(chapter?.Author))
                    tag.Performers = new[] { chapter.Author };

                tag.Album =
                    SafeFileName(storyName ?? "Story");

                if (int.TryParse(chapter.Number, out int track))
                    tag.Track = (uint)track;

                if (!string.IsNullOrWhiteSpace(chapter?.EpubDescription))
                    tag.Comment = chapter.EpubDescription;

                if (chapter?.Tags?.Length > 0)
                    tag.Genres = chapter.Tags;

                var id3v2 =
                    tagFile.GetTag(TagLib.TagTypes.Id3v2, true)
                    as TagLib.Id3v2.Tag;

                if (id3v2 != null &&
                    chapter?.CoverImage is { Length: > 0 })
                {
                    id3v2.Pictures =
                    new TagLib.IPicture[]
                    {
                        new TagLib.Picture
                        {
                            Type = TagLib.PictureType.FrontCover,
                            Description = "Cover",
                            MimeType = "image/jpeg",
                            Data = new TagLib.ByteVector(chapter.CoverImage)
                        }
                    };
                }

                tagFile.Save();
            }
            catch { }

            return true;
        }

        // =============================
        // TITLE NORMALIZATION
        // =============================
        public static string NormalizeChapterTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "Untitled";

            title = title.Trim();

            // Only modify if it contains "Chapter"
            if (!Regex.IsMatch(title,
                @"\bChapter\b",
                RegexOptions.IgnoreCase))
                return title;

            // Remove everything before first "Chapter"
            return Regex.Replace(
                title,
                @"^.*?\bChapter\b",
                "Chapter",
                RegexOptions.IgnoreCase
            ).Trim();
        }

        // =============================
        // TTS CLEANUP
        // =============================
        public static string NormalizeForTts(string text)
        {
            text = Regex.Replace(text ?? "",
                @"(\p{L})-\r?\n(\p{L})",
                "$1$2");

            text = text.Replace("\r\n", "\n");

            text = Regex.Replace(text,
                @"[ \t]*\n[ \t]*",
                "\n");

            text = Regex.Replace(text,
                @"\n{3,}",
                "\n\n");

            text = Regex.Replace(text,
                @"(?<=\S)\n(?=\S)",
                " ");

            text = Regex.Replace(text,
                @"[ \t]{2,}",
                " ");

            return text.Trim();
        }

        public static IEnumerable<string> ChunkForTts(
            string text,
            int maxChars = 3000)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            text = NormalizeForTts(text);

            var sentences =
                Regex.Split(text,
                @"(?<=[\.!\?…])\s+");

            var buf =
                new StringBuilder(maxChars + 64);

            foreach (var s in sentences)
            {
                if (s.Length > maxChars)
                {
                    foreach (var p in ChunkByWords(s, maxChars))
                        yield return p;
                    continue;
                }

                if (buf.Length + s.Length + 1 > maxChars)
                {
                    yield return buf.ToString();
                    buf.Clear();
                }

                if (buf.Length > 0)
                    buf.Append(' ');

                buf.Append(s);
            }

            if (buf.Length > 0)
                yield return buf.ToString();
        }

        static IEnumerable<string> ChunkByWords(
            string sentence,
            int limit)
        {
            var words =
                sentence.Split(' ',
                StringSplitOptions.RemoveEmptyEntries);

            var sb =
                new StringBuilder(limit + 32);

            foreach (var w in words)
            {
                if (sb.Length + w.Length + 1 > limit)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }

                if (sb.Length > 0)
                    sb.Append(' ');

                sb.Append(w);
            }

            if (sb.Length > 0)
                yield return sb.ToString();
        }

        // =============================
        // SAFE FILENAME
        // =============================
        public static string SafeFileName(
            string name,
            int maxBaseLength = 120)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "untitled";

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Trim().Trim('.');

            if (name.Length > maxBaseLength)
                name = name[..maxBaseLength];

            return name;
        }
    }

    public class TextToSpeechService
    {
        public void SpeakToWaveStream(
            string text,
            Stream outputWaveStream,
            int chunkSize = 3000)
        {
            throw new NotImplementedException(
                "Platform specific TTS needed.");
        }
    }
}