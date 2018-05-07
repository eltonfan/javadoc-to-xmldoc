using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Elton.CommentConvert
{
    public class CommentFormatter
    {
        readonly string basePath;
        readonly List<string> listFiles = new List<string>();
        int currentFileIndex = -1;
        string currentFileContent = null;
        int currentFilePosition = -1;
        public CommentFormatter(string basePath)
        {
            this.basePath = basePath;
            FindFiles(this.basePath);
        }

        protected void FindFiles(string path)
        {
            listFiles.AddRange(Directory.GetFiles(path, "*.cs"));
            foreach (var folder in Directory.GetDirectories(path))
                FindFiles(folder);
        }

        public void NextMatch()
        {
            if (currentFileIndex < 0 || currentFileContent == null || currentFilePosition >= currentFileContent.Length)
            {//载入下一个文件
                currentFileIndex++;
                if (currentFileIndex >= listFiles.Count)
                    throw new EndOfStreamException("Finished.");

                currentFileContent = File.ReadAllText(listFiles[currentFileIndex], Encoding.UTF8);
                currentFilePosition = 0;
            }

            FindNext();
        }

        /// <summary>
        /// 查找下一个
        /// </summary>
        protected virtual void FindNext()
        {
            //以单行模式匹配筛选
            var regex = new Regex(@"(?<pre>[\t ]*)///\s*<summary>\s*(?<data>.+?)\s*///\s*</summary>", RegexOptions.Singleline);
            var matchSummary = regex.Match(currentFileContent, currentFilePosition);
            if (!matchSummary.Success)
            {
                currentFilePosition = currentFileContent.Length;
                //MessageBox.Show(this, "当前文件结束。");
                return;
            }

            var prefix = matchSummary.Groups["pre"].Value;// 缩进
            var content = matchSummary.Groups["data"].Value;// <summary></summary> 的 innerXml

            var replace = ComputeReplace(prefix, content);

            currentFileContent = currentFileContent.Substring(0, matchSummary.Index)
                + replace
                + currentFileContent.Substring(matchSummary.Index + matchSummary.Length);
            currentFilePosition = matchSummary.Index + replace.Length;

            File.WriteAllText(listFiles[currentFileIndex], currentFileContent, Encoding.UTF8);
        }

        protected string ComputeReplace(string prefix, string content)
        {
            var dicParams = new Dictionary<string, Node>();
            Node lastNode = null;

            Node defaultNode = new Node("summary", "");
            dicParams.Add(defaultNode.FullName, defaultNode);
            lastNode = defaultNode;
            foreach (var line in content.Split(new string[] { "\r\n" }, StringSplitOptions.None))
            {
                Match match = null;
                match = new Regex(@"///\s*@(?<tag>(?:param|aaaaa))\s+(?<name>[^\s]+)\s+(?<value>.+)", RegexOptions.Singleline).Match(line);
                if (match.Success)
                {//参数行
                    string tag = "";
                    switch (match.Groups["tag"].Value)
                    {
                        case "param": tag = "param"; break;
                        default:
                            throw new FormatException($"Unknown tag: {match.Groups["tag"].Value}");
                    }
                    var node = new Node(
                        tag: tag,
                        name: match.Groups["name"].Value);
                    dicParams.Add(node.FullName, node);

                    lastNode = node;
                }
                else
                {
                    //判断是否为return行
                    match = new Regex(@"///\s*@return\s+(?<value>.+)", RegexOptions.Singleline).Match(line);
                    if (match.Success)
                    {
                        var node = new Node(
                            tag: "value",
                            name: "");
                        dicParams.Add(node.FullName, node);

                        lastNode = node;
                    }
                    else
                    {
                        //判断是否为普通行
                        match = new Regex(@"///\s*(?<value>.*)", RegexOptions.Singleline).Match(line);
                        if (!match.Success)
                            throw new FormatException($"Faile to parse line: {line}");
                    }
                }
                //添加行
                var value = match.Groups["value"].Value;
                // {@link List} --> <see cref="List"/>
                value = Regex.Replace(value, @"\{@link\s+([\w\d\._\(\)#]+)\}", "<see cref=\"$1\"/>");
                lastNode.Contents.Add(value);
            }

            var sb = new StringBuilder();
            foreach (var node in dicParams.Values)
            {
                if (node.Contents.Count < 1)
                    continue;

                //去除末尾的空白行
                int lineCount = node.Contents.Count;
                for (int i = lineCount - 1; i >= 0; i--)
                {
                    if (!string.IsNullOrWhiteSpace(node.Contents[i]))
                    {//不是空行，则为最后一行
                        if (i > 0 && node.Contents[i].Trim().Length < 12)
                        {//如果不是唯一一行，且最后一行的字符少于10个，则合并到上一行末
                            node.Contents[i - 1] += " " + node.Contents[i].Trim();
                            lineCount = i;
                        }
                        else
                        {
                            lineCount = i + 1;
                        }
                        break;
                    }
                }

                if (lineCount == 1 && node.Tag != "summary")
                {//单行
                    if (string.IsNullOrEmpty(node.Name))
                        sb.AppendLine(prefix + $"/// <{node.Tag}\">{node.Contents.First()}</{node.Tag}>");
                    else
                        sb.AppendLine(prefix + $"/// <{node.Tag} name=\"{node.Name}\">{node.Contents.First()}</{node.Tag}>");
                }
                else
                {//多行
                    if (string.IsNullOrEmpty(node.Name))
                        sb.AppendLine(prefix + $"/// <{node.Tag}>");
                    else
                        sb.AppendLine(prefix + $"/// <{node.Tag} name=\"{node.Name}\">");

                    for (int i = 0; i < lineCount; i++)
                    {
                        var line = node.Contents[i];
                        sb.AppendLine(prefix + $"/// {line}");
                    }

                    sb.AppendLine(prefix + $"/// </{node.Tag}>");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
