﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using MS = Markdig.Syntax;
using MSI = Markdig.Syntax.Inlines;

namespace EhTagClient
{

    [System.Diagnostics.DebuggerDisplay(@"MD{Raw}")]
    public sealed class MarkdownText
    {
        public MarkdownText(string rawString, bool singleLine)
        {
            rawString = (rawString ?? "").Trim();
            rawString = Regex.Replace(rawString, "(\r\n|\r|\n)", singleLine ? " " : "\n").Trim();
            Raw = rawString;
        }

        public string Raw { get; private set; }
        public string Text { get; private set; }
        public string Html { get; private set; }
        public Newtonsoft.Json.Linq.JRaw Ast { get; private set; }

        public void Render()
        {
            var ast = MarkdigExt.Renderer.Parse(Raw);
            Text = MarkdigExt.Renderer.ToPlainText(ast);
            Raw = MarkdigExt.Renderer.ToNormalizedMarkdown(ast);
            Html = MarkdigExt.Renderer.ToHtml(ast);
            Ast = new Newtonsoft.Json.Linq.JRaw(MarkdigExt.Renderer.ToJson(ast));
        }
    }
}
