﻿using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.Text;

namespace EhTagClient.MarkdigExt.Html
{
    public class HtmlEntityInlineRenderer : HtmlObjectRenderer<HtmlEntityInline>
    {
        protected override void Write(HtmlRenderer renderer, HtmlEntityInline obj)
        {
            if (renderer.EnableHtmlForInline)
                renderer.WriteEscape(obj.Transcoded);
            else
                renderer.Write(obj.Transcoded);
        }
    }
    public class LiteralInlineRenderer : HtmlObjectRenderer<LiteralInline>
    {
        protected override void Write(HtmlRenderer renderer, LiteralInline obj)
        {
            if (renderer.EnableHtmlForInline)
                renderer.WriteEscape(obj.Content);
            else
                renderer.Write(obj.Content);
        }
    }

}
