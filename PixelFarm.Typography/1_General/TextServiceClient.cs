﻿//MIT, 2020-present, WinterDev
using System;
using System.Collections.Generic;

using Typography.OpenFont;
using Typography.OpenFont.Extensions;

using Typography.FontCollections;
using Typography.TextLayout;
using Typography.TextBreak;

using PixelFarm.Drawing;

namespace Typography.Text
{

    public class TextServiceClient
    {
        readonly OpenFontTextService _openFontTextService; //owner
        readonly VirtualTextSpanPrinter _p;

        internal TextServiceClient(OpenFontTextService openFontTextService)
        {
            _openFontTextService = openFontTextService;
            _p = new VirtualTextSpanPrinter();
            _p.BuiltInAlternativeTypefaceSelector = (int codepoint, AltTypefaceSelectorBase userSelector, out Typeface typeface) => _openFontTextService.TryGetAlternativeTypefaceFromCodepoint(codepoint, userSelector, out typeface);
        }
        public ScriptLang CurrentScriptLang
        {
            get => _p.ScriptLang;
            set => _p.ScriptLang = value;
        }
        public void EnableGsubGpos(bool value)
        {
            _p.EnableGsubGpos(value);
        }


        readonly TextPrinterWordVisitor _wordVisitor = new TextPrinterWordVisitor();

        readonly TextPrinterLineSegmentList<TextPrinterLineSegment> _lineSegs = new TextPrinterLineSegmentList<TextPrinterLineSegment>();

        public void CalculateUserCharGlyphAdvancePos(in Typography.Text.TextBufferSpan textBufferSpan, RequestFont font, ref TextSpanMeasureResult measureResult)
        {

            _lineSegs.Clear();
            _wordVisitor.SetLineSegmentList(_lineSegs);

            char[] str = textBufferSpan.GetRawCharBuffer(); //TODO: review here again!
            _p.BreakToLineSegments(str, textBufferSpan.start, textBufferSpan.len, _wordVisitor);

            _wordVisitor.SetLineSegmentList(null); //clear

            CalculateUserCharGlyphAdvancePos(textBufferSpan,
                _lineSegs,
                font,
                ref measureResult);
        }

        public void PrepareFormattedStringList(char[] textBuffer, int startAt, int len, FormattedGlyphPlanList fmtGlyphs)
        {
            _p.PrepapreFormattedStringList(textBuffer, startAt, len, fmtGlyphs);
            fmtGlyphs.IsRightToLeftDirection = _p.NeedRightToLeftArr;
        }

        public void CalculateUserCharGlyphAdvancePos(in Typography.Text.TextBufferSpan textBufferSpan,
            ILineSegmentList lineSegs,
            RequestFont font,
            ref TextSpanMeasureResult measureResult)
        {

            //layout  
            //from font
            //resolve for typeface
            //  
            Typeface typeface = _openFontTextService.ResolveFont(font).Typeface;

            _p.Typeface = typeface;
            _p.FontSizeInPoints = font.SizeInPoints;


            float scale = typeface.CalculateScaleToPixelFromPointSize(font.SizeInPoints);

            int j = lineSegs.Count;
            int pos = 0; //start at 0


            short minOffsetY = 0;
            short maxOffsetY = 0;
            int outputTotalW = 0;
            bool hasSomeExtraOffsetY = false;

            for (int i = 0; i < j; ++i)
            {
                //get each segment
                ILineSegment lineSeg = lineSegs[i];

                //each line seg may has different script lang

                //_txtServices.CurrentScriptLang = lineSeg.scriptLang;
                //
                //CACHING ...., reduce number of GSUB/GPOS
                //
                //we cache used line segment for a while
                //we ask for caching context for a specific typeface and font size   
#if DEBUG

#endif
                TextBufferSpan span1 = new Typography.Text.TextBufferSpan(
                    textBufferSpan.GetRawCharBuffer(),
                    lineSeg.StartAt,
                    lineSeg.Length);

                GlyphPlanSequence seq = _p.CreateGlyphPlanSeq(span1);

                int seqLen = seq.Count;

                for (int s = 0; s < seqLen; ++s)
                {
                    UnscaledGlyphPlan glyphPlan = seq[s];
                    if (glyphPlan.OffsetY != 0)
                    {
                        hasSomeExtraOffsetY = true;
                        if (minOffsetY > glyphPlan.OffsetY)
                        {
                            minOffsetY = glyphPlan.OffsetY;
                        }
                        if (maxOffsetY < glyphPlan.OffsetY)
                        {
                            maxOffsetY = glyphPlan.OffsetY;
                        }

                    }

                    outputTotalW +=
                          measureResult.outputXAdvances[pos + glyphPlan.input_cp_offset] += (int)Math.Round(glyphPlan.AdvanceX * scale);
                }
                pos += lineSeg.Length;
            }


            measureResult.outputTotalW = outputTotalW;
            measureResult.lineHeight = (ushort)Math.Round(typeface.CalculateMaxLineClipHeight() * scale);

            if (hasSomeExtraOffsetY)
            {
                measureResult.minOffsetY = (short)Math.Round(minOffsetY * scale);
                measureResult.maxOffsetY = (short)Math.Round(maxOffsetY * scale);
                if (measureResult.maxOffsetY != 0 || measureResult.minOffsetY != 0)
                {
                    measureResult.hasSomeExtraOffsetY = true;
                }
            }

        }

        public float CalculateScaleToPixelsFromPoint(RequestFont font) => (_openFontTextService.ResolveFont(font) is ResolvedFont resolvedFont) ? resolvedFont.GetScaleToPixelFromPointInSize() : 0;

        public float MeasureWhitespace(RequestFont f)
        {
            ResolvedFont resolvedFont = _openFontTextService.ResolveFont(f);
            if (resolvedFont != null)
            {
                return resolvedFont.WhitespaceWidthF;
            }
            return 0;
        }
        public void SetCurrentFont(Typeface typeface, float sizeInPts, ScriptLang sclang)
        {
            _p.ScriptLang = sclang;
            _p.Typeface = typeface;
            _p.FontSizeInPoints = sizeInPts;

            _p.UpdateGlyphLayoutSettings();
        }
        public void SetCurrentFont(Typeface typeface, float sizeInPts, ScriptLang sclang, PositionTechnique posTech)
        {
            _p.ScriptLang = sclang;
            _p.PositionTechnique = posTech;
            _p.Typeface = typeface;
            _p.FontSizeInPoints = sizeInPts;

            _p.UpdateGlyphLayoutSettings();
        }
        public void CreateGlyphPlanSeq(in Typography.Text.TextBufferSpan textBufferSpan, IUnscaledGlyphPlanList unscaledList)
        {
            _p.CreateGlyphPlanSeq(textBufferSpan, unscaledList);
        }
        public GlyphPlanSequence CreateGlyphPlanSeq(in Typography.Text.TextBufferSpan textBufferSpan, Typeface typeface, float sizeInPts)
        {

            _p.Typeface = typeface;
            _p.FontSizeInPoints = sizeInPts;

            return _p.CreateGlyphPlanSeq(textBufferSpan);
        }

        public GlyphPlanSequence CreateGlyphPlanSeq(in Typography.Text.TextBufferSpan textBufferSpan, RequestFont font)
        {
            return CreateGlyphPlanSeq(textBufferSpan, _openFontTextService.ResolveFont(font).Typeface, font.SizeInPoints);
        }
        public GlyphPlanSequence CreateGlyphPlanSeq(in TextBufferSpan textBufferSpan, ResolvedFont font)
        {
            return CreateGlyphPlanSeq(textBufferSpan, font.Typeface, font.SizeInPoints);
        }


        readonly MeasureStringArgs _measureResult = new MeasureStringArgs();
        public Size MeasureString(in PixelFarm.Drawing.TextBufferSpan textBufferSpan, RequestFont font)
        {
            //TODO: review here
            var bufferSpan = new TextBufferSpan(textBufferSpan.GetRawCharBuffer(), textBufferSpan.start, textBufferSpan.len);
            return MeasureString(bufferSpan, font);
        }
        public Size MeasureString(in Typography.Text.TextBufferSpan bufferSpan, RequestFont font)
        {
            //TODO: review here
            Typeface typeface = _openFontTextService.ResolveFont(font).Typeface;
            _p.Typeface = typeface;
            _p.FontSizeInPoints = font.SizeInPoints;

            _measureResult.Reset();
            _p.MeasureString(bufferSpan, _measureResult);

            return new Size(_measureResult.Width, _measureResult.Height);
        }
        public Size MeasureString(in PixelFarm.Drawing.TextBufferSpan textBufferSpan, ResolvedFont font)
        {
            //TODO: review here
            var bufferSpan = new TextBufferSpan(textBufferSpan.GetRawCharBuffer(), textBufferSpan.start, textBufferSpan.len);
            return MeasureString(bufferSpan, font);
        }
        public Size MeasureString(in Typography.Text.TextBufferSpan textBufferSpan, ResolvedFont font)
        {
            //TODO: review here
            Typeface typeface = font.Typeface;
            _p.Typeface = typeface;
            _p.FontSizeInPoints = font.SizeInPoints;
            var bufferSpan = new Typography.Text.TextBufferSpan(textBufferSpan.GetRawCharBuffer(), textBufferSpan.start, textBufferSpan.len);

            _measureResult.Reset();
            _p.MeasureString(bufferSpan, _measureResult);

            return new Size(_measureResult.Width, _measureResult.Height);
        }
        public void MeasureString(in Typography.Text.TextBufferSpan bufferSpan, RequestFont font, int limitWidth, out int charFit, out int charFitWidth)
        {
            //TODO: review here ***
            Typeface typeface = _openFontTextService.ResolveFont(font).Typeface;
            _p.Typeface = typeface;
            _p.FontSizeInPoints = font.SizeInPoints;

            _measureResult.Reset();
            _measureResult.LimitWidth = limitWidth;
            _p.MeasureString(bufferSpan, _measureResult);

            charFit = _measureResult.CharFit;
            charFitWidth = _measureResult.CharFitWidth;
        }
        public void MeasureString(in PixelFarm.Drawing.TextBufferSpan textBufferSpan, RequestFont font, int limitWidth, out int charFit, out int charFitWidth)
        {
            var bufferSpan = new TextBufferSpan(textBufferSpan.GetRawCharBuffer(), textBufferSpan.start, textBufferSpan.len);
            MeasureString(bufferSpan, font, limitWidth, out charFit, out charFitWidth);
        }

        public float MeasureBlankLineHeight(RequestFont font)
        {
            ResolvedFont resolvedFont = ResolveFont(font);
            return resolvedFont.LineSpacingInPixels;
        }


        public void BreakToLineSegments(in TextBufferSpan textBufferSpan, WordVisitor wordVisitor)
        {
            //a text buffer span is separated into multiple line segment list  
            _p.BreakToLineSegments(
                textBufferSpan.GetRawCharBuffer(),
                textBufferSpan.start,
                textBufferSpan.len,
                wordVisitor);
        }
        public ResolvedFont ResolveFont(RequestFont reqFont) => _openFontTextService.ResolveFont(reqFont);
        public bool TryGetAlternativeTypefaceFromCodepoint(int codepoint, AltTypefaceSelectorBase selector, out Typeface found) => _openFontTextService.TryGetAlternativeTypefaceFromCodepoint(codepoint, selector, out found);

        public AlternativeTypefaceSelector AlternativeTypefaceSelector
        {
            get => (AlternativeTypefaceSelector)_p.AlternativeTypefaceSelector;
            set => _p.AlternativeTypefaceSelector = value;
        }

        public bool Eq(ReqFontSpec spec1, ReqFontSpec spec2)
        {
            return spec1.GetReqKey() == spec2.GetReqKey();
        }
        public bool Eq(ResolvedFont resolved1, ReqFontSpec spec2)
        {
            ResolvedFont resolved2 = ReqFontSpec.GetResolvedFont1<ResolvedFont>(spec2);
            if (resolved1 == resolved2)
            {
                return true;
            }

            if (resolved2 == null)
            {
                //no cache resolved data,
                if (spec2.Name == resolved1.Typeface.Name)
                {

                }
                return false;
            }
            else
            {
                return resolved1.RuntimeResolvedKey == resolved2.RuntimeResolvedKey;
            }

        }
        public bool Eq(ResolvedFont resolvedFont1, ResolvedFont resolvedFont2)
        {
            return resolvedFont1.RuntimeResolvedKey == resolvedFont2.RuntimeResolvedKey;
        }
    }
}