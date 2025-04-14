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

        public List<Chapter> GetContentFromEpubFunction(string epubFilename)
        {
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

            foreach (var item in epubBook.ReadingOrder)
            {
                var title = Path.GetFileNameWithoutExtension(item.FilePath).Trim().Replace("_", " ");
                if (IsValidChapter(title.ToLower()))
                {
                    var chapter = new Chapter
                    {
                        Title = title,
                        Content = CleanContent(item.Content)
                    };
                    if (chapter.WordCount > 300)
                    {
                        chapters.Add(chapter);
                    }
                }
            }
            return chapters;
        }

        public bool IsValidChapter(string title) =>
            !(title.Contains("cover.xhtml") ||
              title.Contains("information.xhtml") ||
              title.Contains("stylesheet.xhtml") ||
              title.Contains("title_page.xhtml") ||
              title.Contains("nav.xhtml") ||
              title.Contains("introduction.xhtml"));

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
                    if (IsValidChapter(Path.GetFileName(xhtmlFile).ToLower()))
                    {
                        var chapter = new Chapter
                        {
                            Title = title,
                            Content = CleanContent(File.ReadAllText(xhtmlFile))
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
            content = SplitOnPoint(content);
            content = RemoveExtraSpecialChars(content);
            content = LimitRepeatedCharacters(content);
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
    }

}
