// GetContentFromEpub.cs
using BookApp.Fungtions;   // for ConvertTextToSound.NormalizeForTts
using BookApp.Models;      // <-- ensure Chapter resolves to the Models one
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using VersOne.Epub;

namespace BookApp.Functions
{
    public class GetContentFromEpubFile
    {
        public GetContentFromEpubFile() { }
        private string epubname;

        public List<Chapter> GetContentFromEpubFunction(string epubFilename)
        {
            epubname = epubFilename;
            try
            {
                var epubBook = EpubReader.ReadBook(epubFilename);
                return ExtractChaptersFromEpubViaReader(epubBook);
            }
            catch
            {
                return ExtractChaptersFromEpub(epubFilename);
            }
        }

        private List<Chapter> ExtractChaptersFromEpubViaReader(EpubBook epubBook)
        {
            var chapters = new List<Chapter>();

            byte[] coverBytes = null;
            try
            {
                if (epubBook.CoverImage != null)
                {
                    coverBytes = epubBook.CoverImage;
                }
            }
            catch
            {
                // ignore cover errors, just means no image
            }

            foreach (var item in epubBook.ReadingOrder)
            {
                var title = Path.GetFileNameWithoutExtension(item.FilePath).Trim().Replace("_", " ");
                if (IsValidChapter(title.ToLower(), item.FilePath))
                {
                    string chapterNumber = ExtractFourDigitPrefix(title);

                    var chapter = new Chapter
                    {
                        Title = title,
                        Number = chapterNumber,
                        Content = CleanContent(item.Content),
                        Author = epubBook.Author,
                        CoverImage = coverBytes
                    };
                    if (chapter.WordCount > 300)
                    {
                        chapters.Add(chapter);
                    }
                }
            }
            epubname = string.Empty;
            return chapters;
        }

        private string ExtractFourDigitPrefix(string title)
        {
            // Match exactly 4 digits at the start of the string
            var match = Regex.Match(title, @"^\d{4}");
            return match.Success ? match.Value : string.Empty;
        }

        public bool IsValidChapter(string title, string filepath)
        {
            string[] banned =
            {
                "cover.xhtml",
                "information.xhtml",
                "stylesheet.xhtml",
                "title_page.xhtml",
                "nav.xhtml",
                "introduction.xhtml"
            };

            title = title.ToLower();
            filepath = filepath.ToLower();

            return !banned.Any(b => title.Contains(b) || filepath.Contains(b));
        }




        private List<Chapter> ExtractChaptersFromEpub(string epubFilePath)
        {
            var chapters = new List<Chapter>();
            string tempDirectory = Path.Combine(Path.GetTempPath(), "EpubTemp");

            try
            {
                Directory.CreateDirectory(tempDirectory);
                ZipFile.ExtractToDirectory(epubFilePath, tempDirectory);

                var xhtmlFiles = Directory.GetFiles(tempDirectory, "*.xhtml", SearchOption.AllDirectories);

                foreach (var xhtmlFile in xhtmlFiles)
                {
                    var title = Path.GetFileNameWithoutExtension(xhtmlFile).Trim().Replace("_", " ");
                    if (IsValidChapter(Path.GetFileName(xhtmlFile).ToLower(), xhtmlFile))
                    {
                        var chapter = new Chapter
                        {
                            Title = title,
                            Content = CleanContent(System.IO.File.ReadAllText(xhtmlFile))
                        };
                        chapters.Add(chapter);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting EPUB file: {ex.Message}");
            }
            finally
            {
                Directory.Delete(tempDirectory, true);
            }

            return chapters.OrderBy(chapter => chapter.Title).ToList();
        }

        private string CleanContent(string content)
        {
            content = RemoveStyleTagsAndCssBlocks(content);
            content = RemoveInlineStyles(content);
            content = CleanHtmlTags(content);
            if (!string.IsNullOrEmpty(epubname) && epubname.Contains("Caterpillar"))
            {
                content = RemoveSkillsBlocks(content);
            }
            if (!string.IsNullOrEmpty(epubname) && epubname.Contains("SpiderGwen"))
            {
                content = RemovePatroneStuff(content);
            }
            content = SplitOnPoint(content);
            content = RemoveExtraSpecialChars(content);
            content = LimitRepeatedCharacters(content);
            content = RemoveExtraStuff(content);

            // final light cleanup before TTS:
            content = ConvertTextToSound.NormalizeForTts(content);

            return content;
        }

        private string RemoveStyleTagsAndCssBlocks(string html)
        {
            // Remove <style> blocks
            html = Regex.Replace(html, @"<style.*?>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            // Remove divs with known CSS classes like custom-block or superchat
            html = Regex.Replace(html, @"<div[^>]*class\s*=\s*[""']?(custom-block-wrapper|custom-block|superchat-[^""'\s>]*)[^>]*>.*?</div>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            return html;
        }

        private string RemoveInlineStyles(string html)
        {
            return Regex.Replace(html, @"style\s*=\s*[""'].*?[""']", "", RegexOptions.IgnoreCase);
        }

        private string CleanHtmlTags(string html)
        {
            var cleanedText = Regex.Replace(html, "<.*?>", string.Empty);
            cleanedText = Regex.Replace(cleanedText, "“|”", "");
            cleanedText = Regex.Replace(cleanedText, @"\*", "'");
            return System.Net.WebUtility.HtmlDecode(cleanedText);
        }

        public string SplitOnPoint(string content)
        {
            content = content.Trim();
            content = Regex.Replace(content, @"\s+", " ");
            content = Regex.Replace(content, @"\n{2,}", "\n");
            content = Regex.Replace(content, @"(\n\s+)|(\s+\n)", "\n");

            List<string> tempcontentList = Regex.Split(content, @"(?<=[.])").ToList();

            List<string> tempcontentList2 = new List<string>();

            foreach (string item in tempcontentList)
            {
                string temp = item.Trim();
                if (temp.Length >= 2)
                {
                    tempcontentList2.Add(temp);
                }
            }

            return String.Join("\n", tempcontentList2).Trim();
        }

        private string RemoveExtraSpecialChars(string content)
        {
            return Regex.Replace(content, @"([^\w\s]{2})[^\w\s]+", "$1");
        }

        private string LimitRepeatedCharacters(string input, int maxRepeat = 3)
        {
            return Regex.Replace(input, @"(\w)\1{" + maxRepeat + @",}", m => new string(m.Groups[1].Value[0], maxRepeat));
        }

        private static readonly Regex SkillsBlockRegex = new Regex(
            @"\[SKILLS:\s*(?:\[[^\]]*\]\s*)+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex CollapseExtraBlankLinesRegex = new Regex(
            @"\n{3,}", RegexOptions.Compiled);

        private string RemoveSkillsBlocks(string text)
        {
            var cleaned = SkillsBlockRegex.Replace(text, string.Empty);
            cleaned = CollapseExtraBlankLinesRegex.Replace(cleaned, "\n\n");
            return cleaned;
        }

        private string RemoveExtraStuff(string content)
        {
            string Tempcontent = content;
            string RemoveWord = "0o0o";

            Tempcontent = Tempcontent.Replace(RemoveWord, "");

            return Tempcontent;
        }

        private string RemovePatroneStuff(string content)
        {
            // 1) Remove short Patreon intro before "onto the story!" only if it is at the start and under 100 words
            content = CutIntroBeforeMarkerIfShort(content, "onto the story!", maxWords: 100, maxMarkerPos: 3000);

            // 2) Remove everything after "Gain access" only if it is near the end of the content
            content = CutEverythingAfterIfNearEnd(content, "Gain access", nearEndRatio: 0.70);

            return content;
        }

        private string CutIntroBeforeMarkerIfShort(string text, string marker, int maxWords, int maxMarkerPos)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int markerIndex = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return text;

            // Only treat it as the "start marker" if it appears near the beginning
            if (markerIndex > maxMarkerPos)
                return text;

            string before = text.Substring(0, markerIndex);

            int wordCount = before.Split(
                new[] { ' ', '\r', '\n', '\t' },
                StringSplitOptions.RemoveEmptyEntries
            ).Length;

            if (wordCount >= maxWords)
                return text;

            // Cut intro including the marker
            return text.Substring(markerIndex + marker.Length).TrimStart();
        }

        private string CutEverythingAfterIfNearEnd(string text, string phrase, double nearEndRatio)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            int idx = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return text;

            // Only cut if phrase is near the end, so we dont wipe the chapter if it appears early
            if (idx < (int)(text.Length * nearEndRatio))
                return text;

            return text.Substring(0, idx);
        }





    }
}
