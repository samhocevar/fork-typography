﻿//MIT, 2017-present, WinterDev
using System;
using System.Windows.Forms;
using System.IO;

using Typography.TextLayout;
using Typography.FontCollections;
using Typography.OpenFont;
using System.Collections.Generic;
using Typography.OpenFont.Tables;

namespace TypographyTest.WinForms
{

    public partial class BasicFontOptionsUserControl : UserControl
    {
        readonly BasicFontOptions _options;

        public BasicFontOptionsUserControl()
        {
            InitializeComponent();
            //
            _options = new BasicFontOptions();

        }
        public BasicFontOptions Options => _options;


        private void OpenFontOptions_Load(object sender, EventArgs e)
        {
            if (this.DesignMode) { return; }
            //
            _options.LoadFontList();
            //
            SetupScriptLangBoxes();
            SetupFontList();
            SetupFontsList2();
            SetupFontSizeList();
            SetupRenderOptions();

            this.lstFontSizes.SelectedIndex = 0;// lstFontSizes.Items.Count - 3;
            if (lstFontList.SelectedItem is InstalledTypeface instTypeface)
            {
                _options.InstalledTypeface = instTypeface;
            }
            SetupPositionTechniqueList();
            chkEnableMultiTypefaces.CheckedChanged += ChkEnableMultiTypefaces_CheckedChanged;

        }

        private void ChkEnableMultiTypefaces_CheckedChanged(object sender, EventArgs e)
        {
            _options.EnableMultiTypefaces = chkEnableMultiTypefaces.Checked;
            _options.InvokeAttachEvents();
        }

        void SetupFontList()
        {

            InstalledTypeface selectedInstalledTypeface = null;
            int selected_index = 0;
            int ffcount = 0;
            bool found = false;


            var tempList = new System.Collections.Generic.List<InstalledTypeface>();
            tempList.AddRange(_options.GetInstalledTypefaceIter());
            tempList.Sort((f1, f2) => f1.ToString().CompareTo(f2.ToString()));

            //add to list and find default font
            string defaultFont = "Tahoma";
            //string defaultFont = "Alef"; //test hebrew
            //string defaultFont = "Century";
            foreach (InstalledTypeface installedTypeface in tempList)
            {
                if (!found && installedTypeface.FontName == defaultFont)
                {
                    selectedInstalledTypeface = installedTypeface;
                    selected_index = ffcount;
                    _options.InstalledTypeface = installedTypeface;
                    found = true;
                }
                lstFontList.Items.Add(installedTypeface);
                ffcount++;
            }
            //set default font for current text printer
            // 

            if (selected_index < 0) { selected_index = 0; }

            lstFontList.SelectedIndex = selected_index;
            lstFontList.SelectedIndexChanged += (s, e) => ChangeSelectedTypeface(lstFontList.SelectedItem as InstalledTypeface);
        }
        void SetupFontsList2()
        {
            {
                InstalledTypefaceCollection typefaceCollection = _options.InstallTypefaceCollection;
                lstFontNameList.Items.Clear();

                foreach (InstalledTypefaceCollection.InstalledTypefaceGroup instGroup in typefaceCollection.GetInstalledTypefaceGroupIter())
                {
                    lstFontNameList.Items.Add(instGroup);
                }

            }
            //
            lstFontNameList.Click += delegate
            {
                lstFontStyle.Items.Clear();

                if (lstFontNameList.SelectedItem is InstalledTypefaceCollection.InstalledTypefaceGroup instGroup)
                {
                    int memberCount = instGroup.Count;
                    foreach (InstalledTypeface instTypeface in instGroup.GetMemberIter())
                    {
                        lstFontStyle.Items.Add(instTypeface);
                    }
                }
            };
            //
            lstFontStyle.Click += (s, e) => ChangeSelectedTypeface(lstFontStyle.SelectedItem as InstalledTypeface);

        }
        void ChangeSelectedTypeface(InstalledTypeface installedTypeface)
        {
            if (installedTypeface == null) return;
            //
            _options.InstalledTypeface = installedTypeface;
            _options.InvokeAttachEvents();
            _txtTypefaceInfo.Text = "file: " + installedTypeface.FontPath + "\r\n" + "weight:" + installedTypeface.WeightClass;

            //
            ShowSupportedScripts(installedTypeface);
        }


        void ShowSupportedScripts(InstalledTypeface installedTypeface)
        {
            //show supported lang

            lstScriptLangs.Items.Clear();

            Dictionary<string, ScriptLang> dic = new Dictionary<string, ScriptLang>();
            installedTypeface.CollectScriptLang(dic);
            foreach (var kv in dic)
            {
                ScriptLang s = kv.Value;
                if (s.sysLangTag != 0 && LanguageSystemNames.TryGetLangSystem(s.GetScriptTagString(), out LangSys found))
                {

                }
                else
                {

                }
                lstScriptLangs.Items.Add(s);
            }


            lstSupportedUnicodeLangs.Items.Clear();

            foreach (BitposAndAssciatedUnicodeRanges bitposAndUnicodeRanges in installedTypeface.GetSupportedUnicodeLangIter())
            {
                lstSupportedUnicodeLangs.Items.Add(bitposAndUnicodeRanges.ToString());
            }
        }

        void SetupFontSizeList()
        {
            lstFontSizes.Items.AddRange(
               new object[]{
                        8, 9,
                        10,11,
                        12,
                        14,
                        16,
                        18,20,22,24,26,28,36,48,72,
                        240,280,300,360,400,420,460,
                        620,720,860,920,1024
               });
            lstFontSizes.SelectedIndexChanged += (s, e) =>
            {
                //new font size
                _options.FontSizeInPoints = (int)lstFontSizes.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }


        //
        int _defaultScriptLangComboBoxIndex = 0;
        EventHandler _scriptLangIndexChanged;
        void SetupScriptLangBoxes()
        {

            //for debug, set default script lang here
            _options.ScriptLang = new ScriptLang(ScriptTagDefs.Latin.Tag);

            //list only scripts that are support by current typeface 
            //show all scripts
            ////
            //int index = 0;
            //foreach (Typography.OpenFont.ScriptLang scriptLang in Typography.OpenFont.ScriptLangs.GetRegiteredScriptLangIter())
            //{

            //    this.lstScriptLangs.Items.Add(scriptLang);

            //    if (scriptLang == _options.ScriptLang)
            //    {
            //        //found default script lang
            //        _defaultScriptLangComboBoxIndex = index;
            //    }
            //    index++;
            //} 
            //lstScriptLangs.SelectedIndex = 0; //default

            _scriptLangIndexChanged = (s, e) =>
            {
                if (this.lstScriptLangs.SelectedItem is ScriptLang sclang)
                {
                    _options.ScriptLang = sclang;
                    _options.InvokeAttachEvents();
                }
            };

            this.lstScriptLangs.SelectedIndexChanged += _scriptLangIndexChanged;
        }


        void SetupPositionTechniqueList()
        {
            cmbPositionTech.Items.Add(PositionTechnique.OpenFont);
            cmbPositionTech.Items.Add(PositionTechnique.Kerning);
            cmbPositionTech.Items.Add(PositionTechnique.None);
            cmbPositionTech.SelectedIndex = 0;
            cmbPositionTech.SelectedIndexChanged += (s, e) =>
            {
                _options.PositionTech = (PositionTechnique)cmbPositionTech.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }

        void SetupRenderOptions()
        {
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithTextPrinterAndMiniAgg);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMiniAgg_SingleGlyph);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithGdiPlusPath);
            cmbRenderChoices.Items.Add(RenderChoice.RenderWithMsdfGen);
            cmbRenderChoices.SelectedIndex = 0;
            cmbRenderChoices.SelectedIndexChanged += (s, e) =>
            {
                _options.RenderChoice = (RenderChoice)cmbRenderChoices.SelectedItem;
                _options.InvokeAttachEvents();
            };
        }
    }
}
