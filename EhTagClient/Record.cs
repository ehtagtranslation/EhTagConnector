﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EhTagClient
{
    public sealed class AcceptableRecordAttribute : ValidationAttribute
    {
        public bool NoCheck { get; set; }

        public override bool RequiresValidationContext => true;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!(value is Record record))
                return new ValidationResult($"The value '{value}' is not valid.", new[] { validationContext.DisplayName });
            record.Render(null);
            if (NoCheck)
                return ValidationResult.Success;
            if (string.IsNullOrEmpty(record.Name.Text))
                return new ValidationResult($"Field should not be empty.", new[] { validationContext.DisplayName + ".name" });
            return ValidationResult.Success;
        }
    }
    public sealed class AcceptableRawAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!(value is string raw))
                return new ValidationResult($"The value '{value}' is not a valid tag.", new[] { validationContext.DisplayName });
            var traw = raw.Trim();
            if (traw != raw)
                return new ValidationResult($"The tag starts with or end with whitespaces.", new[] { validationContext.DisplayName });
            if (string.IsNullOrEmpty(traw))
                return new ValidationResult($"The tag is too short.", new[] { validationContext.DisplayName });
            foreach (var ch in traw)
            {
                if ((ch >= 'a' && ch <= 'z')
                    || (ch >= 'A' && ch <= 'Z')
                    || (ch >= '0' && ch <= '9')
                    || ch == '.'
                    || ch == ' '
                    || ch == '-')
                    continue;
                return new ValidationResult($"The tag included non-alphanumeric characters which are not permitted. Only hyphens, periods, and spaces are allowed in tags.", new[] { validationContext.DisplayName });
            }
            return ValidationResult.Success;
        }
    }

    public class Record
    {
        private static readonly string _Raw = "_Raw";

        private static readonly Regex _LineRegex = new Regex(
            $@"
            ^\s*(?<!\\)\|?\s*
            (?<{nameof(_Raw)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Name)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Intro)}>.*?)
		    \s*(?<!\\)\|\s*
		    (?<{nameof(Links)}>.*?)
		    \s*(?<!\\)\|?\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

        public static KeyValuePair<string, Record> TryParse(string line)
        {
            var match = _LineRegex.Match(line);
            if (!match.Success)
            {
                return default;
            }

            var raw = match.Groups[nameof(_Raw)].Value.Trim().ToLower();
            var name = match.Groups[nameof(Name)].Value;
            var intro = match.Groups[nameof(Intro)].Value;
            var links = match.Groups[nameof(Links)].Value;
            var record = new Record(_Unescape(name), _Unescape(intro), _Unescape(links));
            return KeyValuePair.Create(raw, record);
        }

        private static readonly Regex _UnescapeRe1 = new Regex(@"<br\s*/?>", RegexOptions.Compiled);
        private static readonly Regex _UnescapeRe2 = new Regex(@"(?<!\\)((?:\\\\)*)\\\|", RegexOptions.Compiled);
        private static string _Unescape(string value)
        {
            value = _UnescapeRe1.Replace(value, "\n");
            return _UnescapeRe2.Replace(value, "$1|");
        }

        private static readonly Regex _EscapeRe1 = new Regex(@"(\r\n|\r|\n)", RegexOptions.Compiled);
        private static readonly Regex _EscapeRe2 = new Regex(@"(?<!\\)(\\\\)*\|", RegexOptions.Compiled);
        private static string _Escape(string value)
        {
            value = _EscapeRe1.Replace(value, "<br>");
            return _EscapeRe2.Replace(value, @"\$&");
        }

        [JsonConstructor]
        public Record(string name, string intro, string links)
        {
            Name = new MarkdownText(name, true);
            Intro = new MarkdownText(intro, false);
            Links = new MarkdownText(links, false);
        }

        public MarkdownText Name { get; }

        public MarkdownText Intro { get; }

        public MarkdownText Links { get; }

        public void Render(string raw)
        {
            Context.Raw = raw;
            Context.Record = this;
            Name.Render();
            Intro.Render();
            Links.Render();
        }

        public string ToString(string raw)
        {
            Render(raw);
            return $"| {raw.Trim().ToLower()} | {_Escape(Name.Raw)} | {_Escape(Intro.Raw)} | {_Escape(Links.Raw)} |";
        }
    }
}
