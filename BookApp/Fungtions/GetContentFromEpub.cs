// GetContentFromEpub.cs
using BookApp.Fungtions;   // for ConvertTextToSound.NormalizeForTts
using BookApp.Models;      // <-- ensure Chapter resolves to the Models one
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
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

            string author = (epubBook.Author ?? string.Empty).Trim();
            string description = (epubBook.Description ?? string.Empty).Trim();
            string[] tags = ExtractEpubTags(epubBook);

            foreach (var item in epubBook.ReadingOrder)
            {
                var title = Path.GetFileNameWithoutExtension(item.FilePath).Trim().Replace("_", " ");
                if (IsValidChapter(title.ToLower(), item.FilePath))
                {
                    string chapterNumber = ExtractFourDigitPrefix(title);
                    if (title.Contains("532"))
                    {

                    }

                    var chapter = new Chapter
                    {
                        Title = title.Trim(),
                        Number = chapterNumber,
                        Content = CleanContent(item.Content, title),
                        Author = author,
                        Tags = tags,
                        EpubDescription = description,
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
            if (title.Contains("universe_cover") || title.Contains("universe cover"))
            {
                return true;
            }

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
                    var title = Path.GetFileNameWithoutExtension(xhtmlFile).Replace("_", " ").Trim();
                    if (IsValidChapter(Path.GetFileName(xhtmlFile).ToLower(), xhtmlFile))
                    {
                        var chapter = new Chapter
                        {
                            Title = title,
                            Content = CleanContent(System.IO.File.ReadAllText(xhtmlFile), title)
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

        private string CleanContent(string content, string? chapterTitle)
        {
            content = RemoveStyleTagsAndCssBlocks(content);
            content = RemoveInlineStyles(content);
            content = CleanHtmlTags(content);

            content = RemoveRepeatedChapterTitleFromStart(content, chapterTitle, keepOneInContent: false);
            string ct= chapterTitle;
            var name = epubname?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains("caterpillar"))
                    content = RemoveSkillsBlocks(content);

                if (name.Contains("spidergwen"))
                    content = RemovePatroneStuff(content);

                if (name.Contains("infinite") && name.Contains("comprehension"))
                    content = RemoveCharsfromContent(content);

                if (name.Contains("naruto") &&
                    name.Contains("learning") &&
                    name.Contains("ninjutsu") &&
                    name.Contains("single") &&
                    name.Contains("glance"))
                {
                    content = RemoveViaSplitFromContent(content);
                }

                if (name.Contains("douluo") &&
                    name.Contains("dalu") &&
                    name.Contains("true") &&
                    name.Contains("destiny"))
                {
                    if (ct.Contains("123"))
                    {

                    }
                    content = RemoveAuthorNotes(content);
                    content = RemoveEverythingBeforeChapterIfInFirstTenPercent(content);
                }

                if (name.Contains("the") &&
                    name.Contains("fox") &&
                    name.Contains("hole"))
                {
                    content = CutContentByPercentPosition(content, "XXX", 20, "end");
                }
                if (name.Contains("dream") &&
                    name.Contains("eternity") &&
                    name.Contains("of"))
                {
                    content = RemoveSlashSeparators(content);
                }
                if (name.Contains("gamer") &&
                    name.Contains("became") &&
                    name.Contains("pokemon") &&
                    name.Contains("trainer"))
                {
                    content = CutContentByPercentPosition(content, "-Pokemon-", 35, "start");
                    content = CutContentByPercentPosition(content, "Done!", 40, "end");
                    content = RemoveTrailingWord(content, "And");
                }
                if (name.Contains("the") &&
                    name.Contains("gbpt") &&
                    name.Contains("vol"))
                {
                    content = CutContentByPercentPosition(content, "-Pokemon-", 35, "start");
                    content = CutContentByPercentPosition(content, "Done!", 40, "end");
                    content = RemoveTrailingWord(content, "And");
                }
                if (name.Contains("dd") &&
                   name.Contains("heaven") &&
                   name.Contains("undressed") &&
                   name.Contains("demigod") &&
                   name.Contains("master"))
                {
                    content = CutContentByPercentPosition(content, "(End of Chapter)", 10, "end");
                }
                if (name.Contains("douluo") &&
                   name.Contains("dalu") &&
                   name.Contains("stealing") &&
                   name.Contains("tang") &&
                   name.Contains("destiny"))
                {
                    content = CutContentByPercentPosition(content, "(End of Chapter)", 10, "end");
                }
                if (name.Contains("dd") &&
                   name.Contains("supreme") &&
                   name.Contains("bone") &&
                   name.Contains("awakens") &&
                   name.Contains("goddesses"))
                {
                    content = CutContentByPercentPosition(content, "(End of Chapter)", 10, "end");
                }
                if (name.Contains("Retirement"))
                {
                    content = CutContentByPercentPosition(content, "XXXXX", 15, "start");
                }
                if (name.Contains("btth") &&
                   name.Contains("treasure") &&
                   name.Contains("exchange") &&
                   name.Contains("system"))
                {
                    content = CutContentByPercentPosition(content, "(End of Chapter)", 10, "end");
                }
                if (name.Contains("douluo") &&
                   name.Contains("nature") &&
                   name.Contains("chosen") &&
                   name.Contains("harvesting"))
                {
                    content = CutContentByPercentPosition(content, "(End of Chapter)", 10, "end");
                }
                if (name.Contains("crossover") &&
                   name.Contains("problematic") &&
                   name.Contains("human") &&
                   name.Contains("arcadia"))
                {
                    //if (content.Contains("Read 50 advanced chapters", StringComparison.OrdinalIgnoreCase))
                    //{ }
                    content = CutContentByPercentPosition(content, "Read 50 advanced chapters", 5, "end");
                    content = CutContentByPercentPosition(content, "Every 200 Power Stones = 1 Bonus Chapter!", 5, "end");
                }
                if (name.Contains("ding") &&
                   name.Contains("ultramarine") &&
                   name.Contains("joined") &&
                   name.Contains("group"))
                {
                    content = CutContentByPercentPosition(content, "(Ps", 10, "end");
                    content = CutContentByPercentPosition(content, "END OF CHAPTER", 10, "end");
                }
                if (name.Contains("multiverse") &&
                   name.Contains("conquest") &&
                   name.Contains("starting") &&
                   name.Contains("dragon"))
                {
                    content = CutContentByPercentPosition(content, "darkshadow6395", 10, "start");
                    content = CutContentByPercentPosition(content, "(End of chapter)", 10, "end");
                    content = CutContentByPercentPosition(content, "(Chapter End)", 10, "end");
                    content = CutContentByPercentPosition(content, "(End Of The Chapter)", 10, "end");
                    content = CutContentByPercentPosition(content, "You can read ahead up to", 10, "end");
                }
                if (name.Contains("konoha") &&
                  name.Contains("uchiha") &&
                  name.Contains("living") &&
                  name.Contains("hell"))
                {
                    content = CutContentByPercentPosition(content, "<><><>", 10, "end");
                }
                if (name.Contains("fate") &&
                 name.Contains("just") &&
                 name.Contains("want") &&
                 name.Contains("die")&&
                 name.Contains("throne") &&
                 name.Contains("heroes"))
                {
                    content = CutContentByPercentPosition(content, "<><><>", 15, "end");
                }
                if (name.Contains("food") &&
                 name.Contains("wars") &&
                 name.Contains("inheriting") &&
                 name.Contains("dark") &&
                 name.Contains("culinary") &&
                 name.Contains("world"))
                {
                    content = CutContentByPercentPosition(content, "======", 10, "end");
                }//Food Wars: Inheriting the Dark Culinary World from the Start

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


        static string? GetFirstStringProperty(object? obj, params string[] names)
        {
            if (obj == null) return null;

            var t = obj.GetType();

            foreach (var name in names)
            {
                var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string))
                    return (string?)p.GetValue(obj);
            }

            // fallback: sometimes the string is the only string property
            var anyStringProp = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .FirstOrDefault(p => p.PropertyType == typeof(string));
            return anyStringProp != null ? (string?)anyStringProp.GetValue(obj) : null;
        }

        static string[] ExtractEpubTags(EpubBook? epubBook)
        {
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var subjects = epubBook?.Schema?.Package?.Metadata?.Subjects;
            if (subjects == null) return Array.Empty<string>();

            foreach (var subj in subjects)
            {
                // Try common property names first
                var raw = GetFirstStringProperty(
                    subj,
                    "Subject", "Value", "Text", "Content", "Name"
                );

                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                // Your EPUB uses one comma-separated string in dc:subject
                foreach (var part in raw.Split(new[] { ',', ';', '|', '\n', '\r' },
                                               StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = part.Trim();
                    if (t.Length > 0) tags.Add(t);
                }
            }

            return tags.ToArray();
        }

        static string RemoveCharsfromContent(string content)
        {
            string tempcontent = string.Empty;
            tempcontent = content.Trim();

            tempcontent = tempcontent.Replace("“`", "");
            tempcontent = tempcontent.Replace("Translator: 549690339", "");

            return tempcontent;
        }

        static string RemoveViaSplitFromContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // Find blocks of 8 or more consecutive dashes
            var matches = Regex.Matches(content, "-{8,}");

            if (matches.Count == 0)
                return content;

            var lastMatch = matches[matches.Count - 1];

            // position must be within last 20% of content
            int threshold = (int)(content.Length * 0.8);

            if (lastMatch.Index < threshold)
                return content;

            return content.Substring(0, lastMatch.Index);
        }

        private static string RemoveRepeatedChapterTitleFromStart(string content, string? chapterTitle, bool keepOneInContent)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(chapterTitle))
                return content;

            // Build a tolerant regex pattern from the chapter title.
            // The pattern allows punctuation, whitespace, and other separators between characters
            // so titles like "Xiao Xun'er, Tenfold Feedback!" still match even if the source text
            // contains variants like "Xiao Xuner Tenfold Feedback" or different punctuation.
            string titlePattern = BuildLooseTitlePattern(chapterTitle);

            // Some sources prepend a numeric index before the chapter title.
            // Examples: "1 Chapter 1...", "1: Chapter 1...", "1. Chapter 1..."
            // Allow an optional numeric prefix followed by optional punctuation and whitespace.
            string oneTitle = @"\s*(?:\d+\s*[:.\-]?\s*)?(?:" + titlePattern + @")[\W_]*";

            // Match two or more occurrences of the chapter title at the very beginning of the content.
            // This catches cases where scrapers or OCR pipelines duplicated the title multiple times.
            string repeatedAtStart = @"^(?:" + oneTitle + @"){2,}";

            var rxRepeated = new Regex(repeatedAtStart, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // If there are not at least two consecutive titles at the start,
            // return the content unchanged.
            if (!rxRepeated.IsMatch(content))
            {
                // If you ever want to remove even a single title at the beginning,
                // you could uncomment the following line:
                // content = Regex.Replace(content, @"^" + oneTitle, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                return content;
            }

            if (keepOneInContent)
            {
                // Replace multiple consecutive titles with exactly one normalized title.
                // The first matched title fragment is kept and trimmed to remove extra spacing.
                content = rxRepeated.Replace(content, m =>
                {
                    string first = Regex.Match(m.Value, oneTitle, RegexOptions.IgnoreCase | RegexOptions.Singleline).Value;
                    return first.Trim() + "\n";
                });
            }
            else
            {
                // Remove all duplicated titles from the start of the content.
                content = rxRepeated.Replace(content, "");

                // If one more title remains at the beginning, remove that as well.
                content = Regex.Replace(content, @"^" + oneTitle, "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            // Clean up leading whitespace left behind after removal.
            content = content.TrimStart();

            // Collapse excessive blank lines that might result from stripping titles.
            content = Regex.Replace(content, @"\n{3,}", "\n\n");

            return content;
        }

        private static string BuildLooseTitlePattern(string chapterTitle)
        {
            // Drop leading 4 digits if present: "0001 ..."
            string t = Regex.Replace(chapterTitle.Trim(), @"^\d{4}\s*", "", RegexOptions.IgnoreCase);

            // Some sources include an extra leading chapter index before the real title:
            // "1 Chapter 1 Binding Xiao Xuner Tenfold Feedback"
            // Since RemoveRepeatedChapterTitleFromStart already allows an optional numeric prefix
            // before the title, remove that duplicated leading number here.
            t = Regex.Replace(t, @"^\d+\s+(?=Chapter\b)", "", RegexOptions.IgnoreCase);

            // Normalize apostrophes so different unicode versions behave the same
            t = t.Replace("’", "'")
                 .Replace("‘", "'")
                 .Replace("`", "'")
                 .Replace("´", "'");

            // Tokenize into alphanum chunks: Chapter, 1, 1, Please, Ascend, Little, Martial, Uncle, 1
            // Keep apostrophes inside words so "Yan's" doesn't become "Yan", "s"
            var tokens = Regex.Matches(t, @"[A-Za-z0-9]+(?:'[A-Za-z0-9]+)*")
                              .Cast<Match>()
                              .Select(m => m.Value)
                              .ToList();

            if (tokens.Count == 0)
                return Regex.Escape(t);

            // Allow any punctuation, underscores, or whitespace between tokens
            // This matches: commas, exclamation, dashes, underscores, multiple spaces, etc.
            string between = @"[\W_]*";

            // Allow punctuation inside tokens too by splitting characters
            // This lets "Yan's" match "Yan's", "Yans", "Yan s", etc.
            string TokenToLoose(string token)
            {
                var chars = token.Where(char.IsLetterOrDigit)
                                 .Select(c => Regex.Escape(c.ToString()));

                return string.Join(@"[\W_]*", chars);
            }

            // Build: Chapter[\W_]*1[\W_]*1[\W_]*Please[\W_]*Ascend...
            string pattern = string.Join(between, tokens.Select(TokenToLoose));

            return pattern;
        }

        private static readonly Regex ChapterHeaderRegex =
            new Regex(@"\bchapter\s+\d+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex AuthorNoteRegex =
            new Regex(@"\(\s*a\s*/\s*n\s*:\s*.*?\)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

        private static string RemoveEverythingBeforeChapterIfInFirstTenPercent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var m = ChapterHeaderRegex.Match(content);
            if (!m.Success)
                return content;

            int tenPercent = (int)Math.Floor(content.Length * 0.10);

            // Remove everything before Chapter X only if "Chapter X" starts within first 10%
            if (m.Index <= tenPercent)
                return content.Substring(m.Index);

            return content;
        }

        private static string RemoveAuthorNotes(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            return AuthorNoteRegex.Replace(content, "");
        }

        private static string CutContentByPercentPosition(string content, string marker, int percent, string mode)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(marker))
                return content;

            int idx = mode.Equals("end", StringComparison.OrdinalIgnoreCase)
                ? content.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase)
                : content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (idx < 0)
                return content;

            int firstThreshold = (int)(content.Length * (percent / 100.0));
            int lastThreshold = (int)(content.Length * (1.0 - percent / 100.0));

            if (mode.Equals("start", StringComparison.OrdinalIgnoreCase))
            {
                if (idx <= firstThreshold)
                    return content.Substring(idx + marker.Length).TrimStart();
            }
            else if (mode.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                if (idx >= lastThreshold)
                    return content.Substring(0, idx).TrimEnd();
            }

            return content;
        }

        public static string RemoveSlashSeparators(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            var lines = content.Replace("\r\n", "\n").Split('\n').ToList();

            bool IsSeparator(string line)
            {
                var t = line.Trim();
                return t.Length >= 8 && t.All(c => c == '/');
            }

            int first = lines.FindIndex(IsSeparator);
            int last = lines.FindLastIndex(IsSeparator);

            if (first >= 0)
                lines.RemoveRange(0, first + 1);

            if (last >= 0 && last < lines.Count)
                lines.RemoveRange(last - first - 1, lines.Count - (last - first - 1));

            return string.Join("\n", lines).Trim();
        }

        private static string RemoveTrailingWord(string content, string marker)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            content = content.TrimEnd();

            if (content.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
                content = content.Substring(0, content.Length - marker.Length).TrimEnd();

            return content;
        }
    }
}
